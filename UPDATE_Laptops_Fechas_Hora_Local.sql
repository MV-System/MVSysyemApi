-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 9 (fechas consistentes)
-- Proposito: todas las fechas del modulo se guardan y se comparan en HORA LOCAL, igual que el resto del sistema:
--            DATEADD(HOUR, -4, GETDATE()). Antes se mezclaban GETDATE() (hora del servidor) y la hora local
--            (ej. Fecha_Recepcion usaba GETDATE() y Equipos_Historico la hora local).
--   1. Se redefinen con la hora local los SP del modulo que usaban GETDATE() suelto: registro, recepcion, entrega al
--      cliente, pendientes (las horas de custodia comparan fechas del mismo tipo), anulacion, recalcular configuracion,
--      corregir modulo y guardar configuracion. Cada SP parte de su version vigente; el resto de su logica no cambia.
--   2. Los valores por defecto de las columnas de fecha del modulo pasan a la hora local
--      (Equipo_Configuracion.Fecha_Registro, Equipo_Configuracion_Modulo.Fecha_Registro,
--       Equipo_Configuracion_Correccion.Fecha, Equipo_Cambio_Pieza.Fecha_Registro).
--   Los datos que ya existen NO se modifican: los registros hechos antes de este script quedaron con la hora del
--   servidor (4 horas por delante de la hora local si el servidor esta en UTC). Solo afecta a registros de prueba;
--   para una pieza en custodia anterior, las horas transcurridas se veran unas horas menores.
-- Requiere: todos los scripts de las Tareas 1 a 8 ejecutados ANTES (este es el ultimo).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER; los defaults solo se cambian si aun usan GETDATE()).
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================

-- ------------------------------------------------------------
-- dbo.Equipo_Cambio_Pieza_Registrar
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Registrar
    @Imei                   VARCHAR(30),
    @Tipo_Pieza             VARCHAR(20),          -- 'RAM' | 'ALMACENAMIENTO'
    @ID_Accesorio_Instalado BIGINT,
    @Nueva_Capacidad_GB     INT = NULL,           -- opcional: se calcula de la pieza instalada
    @Nuevo_Tipo             VARCHAR(30) = NULL,   -- opcional: se toma del modelo de la pieza instalada
    @ID_Accesorio_Retirado  BIGINT = NULL,        -- opcional: lo resuelve el SP desde el modulo retirado
    @Destino_Retirada       VARCHAR(10) = NULL,   -- 'CUSTODIA' | 'CLIENTE' (se ignora si no hay retirada)
    @Tecnico                VARCHAR(60),
    @ID_Almacen             INT,
    @ID_PreFactura          INT = NULL,
    @ID_Reparacion          INT = NULL,
    @Nuevo_Precio_Detalle   DECIMAL(18,2) = NULL,
    @Nuevo_Precio_Por_Mayor DECIMAL(18,2) = NULL,
    @Usuario                VARCHAR(60),
    @Horas_Max_Custodia     INT = 24,             -- Tarea 16: viene de appsettings Laptops:HorasMaxCustodia
    @Bloquear_Vencidos      BIT = 0,              -- Tarea 16: Laptops:BloquearTecnicoConPendientesVencidos
    @ID_Cambio              INT = NULL,           -- solo lo envia el API al replicar en la espejo (mismo ID)
    @ID_Modulo_Retirado     INT = NULL,           -- NUEVO: modulo de la laptop que sale (NULL = slot libre)
    @ID_Modulo_Instalado    INT = NULL,           -- NUEVO: solo al replicar (mismo ID del modulo nuevo)
    @Accesorios_Ids         NVARCHAR(MAX) = NULL, -- solo al replicar (JSON de accesorios creados)
    @Porcentaje_Costo_Usado DECIMAL(5,2) = 50     -- NUEVO: Laptops:PorcentajeCostoUsado (costo de la pieza usada)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- --------------------------------------------------------
        -- Replicacion en la espejo: si el cambio ya existe con ese ID, no se repite
        -- --------------------------------------------------------
        IF @ID_Cambio IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza WHERE ID_Cambio = @ID_Cambio)
        BEGIN
            SELECT C.*,
                   Accesorios_Creados = CAST(NULL AS NVARCHAR(MAX)),
                   Mensaje = CASE WHEN C.Imei = @Imei
                                  THEN 'Este cambio de pieza ya existe'
                                  ELSE CONCAT('Error: el ID de cambio ', @ID_Cambio, ' ya lo usa otro equipo (', C.Imei, ')') END
            FROM dbo.Equipo_Cambio_Pieza C
            WHERE C.ID_Cambio = @ID_Cambio;
            RETURN;
        END

        -- --------------------------------------------------------
        -- Validaciones de parametros
        -- --------------------------------------------------------
        IF @Tipo_Pieza NOT IN ('RAM', 'ALMACENAMIENTO')
            THROW 50001, 'Tipo de pieza invalido. Use RAM o ALMACENAMIENTO.', 1;

        IF @Tecnico IS NULL OR LTRIM(RTRIM(@Tecnico)) = ''
            THROW 50004, 'El tecnico es requerido.', 1;

        IF @ID_Accesorio_Instalado IS NULL
            THROW 50005, 'Debe indicar el accesorio instalado.', 1;

        IF (@ID_Modulo_Retirado IS NOT NULL OR @ID_Accesorio_Retirado IS NOT NULL)
           AND ISNULL(@Destino_Retirada, '') NOT IN ('CUSTODIA', 'CLIENTE')
            THROW 50006, 'Destino de la pieza retirada invalido. Use CUSTODIA o CLIENTE.', 1;

        -- Tarea 16: un tecnico con piezas en custodia vencidas no puede registrar mas cambios
        -- (en la replicacion no se aplica: la base de trabajo ya lo valido)
        IF @Bloquear_Vencidos = 1 AND @ID_Cambio IS NULL
           AND EXISTS (SELECT 1
                         FROM dbo.Equipo_Cambio_Pieza P
                        WHERE P.Tecnico = @Tecnico
                          AND P.Estado_Custodia = 'PENDIENTE'
                          AND DATEDIFF(MINUTE, P.Fecha_Registro, DATEADD(HOUR, -4, GETDATE())) > @Horas_Max_Custodia * 60)
        BEGIN
            DECLARE @MsgVencidos NVARCHAR(300) = CONCAT('El tecnico ', @Tecnico, ' tiene piezas retiradas pendientes de recibir por mas de ', @Horas_Max_Custodia, ' horas. Deben recibirse antes de registrar nuevos cambios.');
            THROW 50008, @MsgVencidos, 1;
        END

        -- R2 / R3: los IDs de grupo se obtienen por descripcion una vez y se comparan por ID
        DECLARE @ID_Grupo_Laptops   INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');
        DECLARE @ID_Grupo_Componentes INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes');

        IF @ID_Grupo_Laptops IS NULL OR @ID_Grupo_Componentes IS NULL
            THROW 50007, 'Faltan los grupos Laptops/Componentes. Ejecute CREATE_Laptops_Fase1_Grupos.sql.', 1;

        BEGIN TRAN;

        -- --------------------------------------------------------
        -- 1. Validar el equipo: existe, es laptop (R2), no vendido (R7), en el almacen
        --    (UPDLOCK evita que dos cambios simultaneos usen el mismo costo base)
        -- --------------------------------------------------------
        DECLARE @ID_Modelo INT, @Disponible CHAR(1), @ID_Almacen_Equipo INT, @Costo_Anterior DECIMAL(18,2),
                @Precio_Detalle_Ant DECIMAL(18,2), @Precio_Mayor_Ant DECIMAL(18,2);

        SELECT TOP 1
               @ID_Modelo = E.ID_Modelo,
               @Disponible = E.Disponible,
               @ID_Almacen_Equipo = E.ID_Almacen,
               @Costo_Anterior = CAST(E.Costo_Equipo AS DECIMAL(18,2)),
               @Precio_Detalle_Ant = CAST(E.Precio_Detalle AS DECIMAL(18,2)),
               @Precio_Mayor_Ant = CAST(E.Precio_Por_Mayor AS DECIMAL(18,2))
        FROM dbo.Equipos E WITH (UPDLOCK, ROWLOCK)
        WHERE E.Imei = @Imei;

        IF @ID_Modelo IS NULL
            THROW 50010, 'El equipo no existe.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Modelos WHERE ID_Modelo = @ID_Modelo AND ID_Grupo = @ID_Grupo_Laptops)
            THROW 50011, 'El equipo no es una laptop (su modelo no pertenece al grupo Laptops).', 1;

        IF @Disponible <> 'S'
            THROW 50012, 'La laptop no esta disponible (vendida o no disponible). Solo se permiten cambios en laptops disponibles.', 1;

        IF @ID_Almacen_Equipo <> @ID_Almacen
            THROW 50013, 'La laptop no se encuentra en el almacen indicado.', 1;

        -- Una prefactura pendiente guarda el costo de la laptop al crearse; si el costo cambia
        -- antes de procesarla, la utilidad de la venta saldria con el costo viejo.
        DECLARE @PreFactura_Pendiente INT = (
            SELECT TOP 1 pf.Numero_PreFactura
              FROM dbo.PreDetalle_Facturas pf
              INNER JOIN dbo.PreFacturas p ON p.Numero_PreFactura = pf.Numero_PreFactura
             WHERE pf.ID_Equipo = @Imei AND pf.ID_Tipo = 1 AND pf.Estado = 'P' AND p.Estado = 'P'
             ORDER BY pf.Numero_PreFactura DESC);

        -- (en la replicacion no se aplica: la base de trabajo ya lo valido)
        IF @PreFactura_Pendiente IS NOT NULL AND @ID_Cambio IS NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La laptop esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de cambiar piezas.');
            THROW 50014, @MsgPre, 1;
        END

        -- --------------------------------------------------------
        -- 2. Validar y descontar el accesorio instalado (R3: debe ser un Componente del tipo de pieza elegido)
        -- --------------------------------------------------------
        DECLARE @Costo_Instalado DECIMAL(18,2), @ID_Modelo_Inst INT, @Desc_Inst VARCHAR(100);

        SELECT @Costo_Instalado = CAST(A.Costo_Equipo AS DECIMAL(18,2)),
               @ID_Modelo_Inst  = A.ID_Modelo,
               @Desc_Inst       = A.Descripcion_Accesorio
        FROM dbo.Accesorios A
        INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
        WHERE A.ID_Accesorio = @ID_Accesorio_Instalado
          AND A.ID_Almacen = @ID_Almacen
          AND A.Estado = 'A'
          AND M.ID_Grupo = @ID_Grupo_Componentes;

        IF @Costo_Instalado IS NULL
            THROW 50020, 'El accesorio instalado no existe en este almacen, esta inactivo o no es un Componente.', 1;

        DECLARE @Pieza_Inst VARCHAR(20), @Tipo_Inst VARCHAR(30);

        SELECT @Pieza_Inst = T.Tipo_Pieza, @Tipo_Inst = T.Tipo
          FROM dbo.Componente_Modelo_Tipo T
         WHERE T.ID_Modelo = @ID_Modelo_Inst;

        IF @Pieza_Inst IS NULL
            THROW 50060, 'El modelo del accesorio instalado no es un modelo de componente reconocido (RAM / almacenamiento).', 1;

        IF @Pieza_Inst <> @Tipo_Pieza
            THROW 50061, 'La pieza instalada no corresponde al tipo de pieza elegido (RAM / almacenamiento).', 1;

        DECLARE @Cap_Inst INT = dbo.fn_Componente_Capacidad_GB(@Desc_Inst);
        IF @Cap_Inst IS NULL SET @Cap_Inst = NULLIF(@Nueva_Capacidad_GB, 0);

        IF @Cap_Inst IS NULL
            THROW 50062, 'No se pudo determinar la capacidad de la pieza; revise la descripcion del accesorio.', 1;

        UPDATE dbo.Accesorios
           SET Cantidad = Cantidad - 1
         WHERE ID_Accesorio = @ID_Accesorio_Instalado
           AND ID_Almacen = @ID_Almacen
           AND ISNULL(Cantidad, 0) > 0;

        IF @@ROWCOUNT = 0
            THROW 50021, 'No hay existencia del accesorio instalado en este almacen.', 1;

        -- --------------------------------------------------------
        -- 3. Modulo retirado y su accesorio (para custodia, inventario y costo)
        -- --------------------------------------------------------
        DECLARE @Mod_Ret_Modelo INT, @Mod_Ret_Cap INT, @Mod_Ret_Acc BIGINT, @Creados NVARCHAR(MAX) = NULL;

        -- Compatibilidad: si solo llega el accesorio retirado, se busca el modulo activo que le corresponde
        IF @ID_Modulo_Retirado IS NULL AND @ID_Accesorio_Retirado IS NOT NULL
            SELECT TOP 1 @ID_Modulo_Retirado = Mo.ID_Modulo
              FROM dbo.Equipo_Configuracion_Modulo Mo
             INNER JOIN dbo.Accesorios AR ON AR.ID_Accesorio = @ID_Accesorio_Retirado
             WHERE Mo.Imei = @Imei AND Mo.Estado = 'A' AND Mo.Tipo_Pieza = @Tipo_Pieza
               AND Mo.ID_Modelo_Componente = AR.ID_Modelo
               AND Mo.Capacidad_GB = dbo.fn_Componente_Capacidad_GB(AR.Descripcion_Accesorio)
             ORDER BY Mo.ID_Modulo;

        IF @ID_Modulo_Retirado IS NOT NULL
        BEGIN
            SELECT @Mod_Ret_Modelo = Mo.ID_Modelo_Componente, @Mod_Ret_Cap = Mo.Capacidad_GB, @Mod_Ret_Acc = Mo.ID_Accesorio
              FROM dbo.Equipo_Configuracion_Modulo Mo WITH (UPDLOCK, ROWLOCK)
             WHERE Mo.ID_Modulo = @ID_Modulo_Retirado AND Mo.Imei = @Imei AND Mo.Estado = 'A' AND Mo.Tipo_Pieza = @Tipo_Pieza;

            IF @Mod_Ret_Modelo IS NULL
                THROW 50063, 'El modulo retirado no pertenece a esta laptop, no esta instalado o no es del tipo de pieza elegido.', 1;

            IF @ID_Cambio IS NULL
            BEGIN
                -- (principal) 1) si el accesorio del modulo ya es una pieza USADA, se usa tal cual (con su propio costo)
                SET @ID_Accesorio_Retirado = NULL;

                IF @Mod_Ret_Acc IS NOT NULL
                    SELECT TOP 1 @ID_Accesorio_Retirado = X.ID_Accesorio
                      FROM dbo.Accesorios R
                     INNER JOIN dbo.Accesorios X ON X.ID_Accesorio = R.ID_Accesorio
                            OR (X.ID_Modelo = R.ID_Modelo AND X.Descripcion_Accesorio = R.Descripcion_Accesorio)
                     WHERE R.ID_Accesorio = @Mod_Ret_Acc AND R.Descripcion_Accesorio LIKE '%(Usado)'
                       AND X.ID_Almacen = @ID_Almacen
                     ORDER BY CASE WHEN X.ID_Accesorio = R.ID_Accesorio THEN 0 ELSE 1 END, X.ID_Accesorio;

                -- 2) y 3) la variante USADA del componente (mismo modelo y capacidad) del almacen; si no existe, se crea
                IF @ID_Accesorio_Retirado IS NULL
                    EXEC dbo.Accesorio_Componente_Asegurar
                         @ID_Modelo = @Mod_Ret_Modelo, @Capacidad_GB = @Mod_Ret_Cap, @ID_Almacen = @ID_Almacen,
                         @Usuario = @Usuario, @Ids_Explicitos = NULL,
                         @ID_Accesorio = @ID_Accesorio_Retirado OUTPUT, @Creados = @Creados OUTPUT,
                         @Usado = 1, @Porcentaje_Costo_Usado = @Porcentaje_Costo_Usado;
            END
            ELSE IF @Accesorios_Ids IS NOT NULL
            BEGIN
                -- (replica) se crean con los mismos ID que en la base de trabajo; @ID_Accesorio_Retirado llega ya resuelto
                DECLARE @IdDescartado BIGINT;
                EXEC dbo.Accesorio_Componente_Asegurar
                     @ID_Modelo = @Mod_Ret_Modelo, @Capacidad_GB = @Mod_Ret_Cap, @ID_Almacen = @ID_Almacen,
                     @Usuario = @Usuario, @Ids_Explicitos = @Accesorios_Ids,
                     @ID_Accesorio = @IdDescartado OUTPUT, @Creados = @Creados OUTPUT,
                     @Usado = 1, @Porcentaje_Costo_Usado = @Porcentaje_Costo_Usado;
                IF @ID_Accesorio_Retirado IS NULL SET @ID_Accesorio_Retirado = @IdDescartado;
            END
        END

        -- RAM: no se mezclan tipos (DDR3 / DDR4 / DDR5) entre los modulos que quedan y el que se instala
        IF @Tipo_Pieza = 'RAM'
           AND EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo Mo
                        WHERE Mo.Imei = @Imei AND Mo.Estado = 'A' AND Mo.Tipo_Pieza = 'RAM'
                          AND Mo.ID_Modulo <> ISNULL(@ID_Modulo_Retirado, 0) AND Mo.Tipo <> @Tipo_Inst)
            THROW 50064, 'No se pueden mezclar tipos de RAM (DDR3 / DDR4 / DDR5) en una misma laptop.', 1;

        -- Una laptop con configuracion antigua y sin modulos debe migrarse primero (Tarea 2)
        IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion C
                    WHERE C.Imei = @Imei AND (C.Ram_GB IS NOT NULL OR C.Almacenamiento_GB IS NOT NULL))
           AND NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo Mo WHERE Mo.Imei = @Imei AND Mo.Estado = 'A')
            THROW 50065, 'La laptop tiene configuracion pero no tiene modulos registrados. Ejecute la migracion de modulos (CREATE_Equipo_Configuracion_Modulo.sql).', 1;

        -- --------------------------------------------------------
        -- 4. Pieza retirada: costo de catalogo, R5
        -- --------------------------------------------------------
        DECLARE @Costo_Retirado DECIMAL(18,2) = 0;
        DECLARE @Estado_Custodia VARCHAR(20) = 'NO_APLICA';

        IF @ID_Accesorio_Retirado IS NOT NULL
        BEGIN
            SELECT @Costo_Retirado = CAST(A.Costo_Equipo AS DECIMAL(18,2))
            FROM dbo.Accesorios A
            INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
            WHERE A.ID_Accesorio = @ID_Accesorio_Retirado
              AND M.ID_Grupo = @ID_Grupo_Componentes;

            IF @@ROWCOUNT = 0
                THROW 50030, 'El accesorio retirado no existe o no es un Componente.', 1;

            SET @Estado_Custodia = CASE WHEN @Destino_Retirada = 'CLIENTE' THEN 'ENTREGADA_CLIENTE' ELSE 'PENDIENTE' END;
        END

        -- --------------------------------------------------------
        -- 5. Costo nuevo (R4 / R10): si la pieza se va con el cliente no se resta
        -- --------------------------------------------------------
        DECLARE @Costo_Nuevo DECIMAL(18,2) =
            @Costo_Anterior
            - CASE WHEN @Estado_Custodia = 'PENDIENTE' THEN @Costo_Retirado ELSE 0 END
            + @Costo_Instalado;

        IF @Costo_Nuevo < 0
            THROW 50040, 'El costo resultante de la laptop seria negativo. Verifique los costos de las piezas.', 1;

        -- Precios: el costo nuevo no puede superar los precios de venta
        DECLARE @Precio_Detalle_Nuevo DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Detalle, @Precio_Detalle_Ant);
        DECLARE @Precio_Mayor_Nuevo   DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Por_Mayor, @Precio_Mayor_Ant);
        DECLARE @MsgPrecio NVARCHAR(400);

        IF @Precio_Detalle_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio al detalle (', @Precio_Detalle_Nuevo, '). Indique un precio al detalle nuevo.');
            THROW 50041, @MsgPrecio, 1;
        END

        IF @Precio_Mayor_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio por mayor (', @Precio_Mayor_Nuevo, '). Indique un precio por mayor nuevo.');
            THROW 50042, @MsgPrecio, 1;
        END

        IF (@Nuevo_Precio_Detalle IS NOT NULL OR @Nuevo_Precio_Por_Mayor IS NOT NULL)
           AND @Precio_Mayor_Nuevo > @Precio_Detalle_Nuevo
            THROW 50043, 'El precio por mayor no puede ser mayor que el precio al detalle.', 1;

        -- Foto de la fila actual en el historico de equipos (antes de modificar costo/precios)
        INSERT INTO dbo.Equipos_Historico
            (Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
             Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
             Nota_Historico, Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
             Usuario_Historico, Fecha_registro_Historico, Comision_Detalle, Comision_Por_Mayor)
        SELECT Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
               Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
               'Cambio de pieza de laptop: ajuste de costo y/o precios', Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
               @Usuario, DATEADD(HOUR, -4, GETDATE()), Comision_Detalle, Comision_Por_Mayor
          FROM dbo.Equipos
         WHERE Imei = @Imei;

        UPDATE dbo.Equipos
           SET Costo_Equipo = @Costo_Nuevo,
               Precio_Detalle = @Precio_Detalle_Nuevo,
               Precio_Por_Mayor = @Precio_Mayor_Nuevo
         WHERE Imei = @Imei;

        -- --------------------------------------------------------
        -- 6. Modulos y configuracion de la laptop
        -- --------------------------------------------------------
        DECLARE @Config_Anterior VARCHAR(100) = dbo.fn_Equipo_Configuracion_Texto(@Imei);

        IF NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion WHERE Imei = @Imei)
        BEGIN
            INSERT INTO dbo.Equipo_Configuracion (Imei, Usuario, Fecha_Registro)
            VALUES (@Imei, @Usuario, DATEADD(HOUR, -4, GETDATE()));
        END

        IF @ID_Modulo_Retirado IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET Estado = 'R' WHERE ID_Modulo = @ID_Modulo_Retirado;

        DECLARE @ID_Modulo_Nuevo INT;

        IF @ID_Modulo_Instalado IS NULL
        BEGIN
            INSERT INTO dbo.Equipo_Configuracion_Modulo
                (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, ID_Accesorio, Estado, Usuario, Fecha_Registro)
            VALUES
                (@Imei, @Tipo_Pieza, @Cap_Inst, @ID_Modelo_Inst, @Tipo_Inst, @ID_Accesorio_Instalado, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));
            SET @ID_Modulo_Nuevo = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            -- Replicacion: mismo ID de modulo que en la base de trabajo
            IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo WHERE ID_Modulo = @ID_Modulo_Instalado)
                THROW 50066, 'Error: el ID de modulo indicado ya existe en esta base.', 1;

            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo ON;
            INSERT INTO dbo.Equipo_Configuracion_Modulo
                (ID_Modulo, Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, ID_Accesorio, Estado, Usuario, Fecha_Registro)
            VALUES
                (@ID_Modulo_Instalado, @Imei, @Tipo_Pieza, @Cap_Inst, @ID_Modelo_Inst, @Tipo_Inst, @ID_Accesorio_Instalado, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));
            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo OFF;
            SET @ID_Modulo_Nuevo = @ID_Modulo_Instalado;
        END

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;

        DECLARE @Config_Nueva VARCHAR(100) = dbo.fn_Equipo_Configuracion_Texto(@Imei);

        -- --------------------------------------------------------
        -- 7. Historial + custodia
        -- --------------------------------------------------------
        DECLARE @ID_Nuevo INT;

        IF @ID_Cambio IS NULL
        BEGIN
            INSERT INTO dbo.Equipo_Cambio_Pieza
            (Imei, Tipo_Pieza, ID_Accesorio_Instalado, Costo_Instalado, ID_Accesorio_Retirado, Costo_Retirado,
             Config_Anterior, Config_Nueva, Costo_Equipo_Anterior, Costo_Equipo_Nuevo, ID_Almacen,
             ID_PreFactura, ID_Reparacion, Tecnico, Estado_Custodia, Usuario_Registro, Fecha_Registro,
             ID_Modulo_Retirado, ID_Modulo_Instalado)
            VALUES
            (@Imei, @Tipo_Pieza, @ID_Accesorio_Instalado, @Costo_Instalado, @ID_Accesorio_Retirado, @Costo_Retirado,
             @Config_Anterior, @Config_Nueva, @Costo_Anterior, @Costo_Nuevo, @ID_Almacen,
             @ID_PreFactura, @ID_Reparacion, @Tecnico, @Estado_Custodia, @Usuario, DATEADD(HOUR, -4, GETDATE()),
             @ID_Modulo_Retirado, @ID_Modulo_Nuevo);

            SET @ID_Nuevo = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            -- Replicacion: mismo ID que en la base de trabajo
            SET IDENTITY_INSERT dbo.Equipo_Cambio_Pieza ON;

            INSERT INTO dbo.Equipo_Cambio_Pieza
                (ID_Cambio, Imei, Tipo_Pieza, ID_Accesorio_Instalado, Costo_Instalado, ID_Accesorio_Retirado, Costo_Retirado,
             Config_Anterior, Config_Nueva, Costo_Equipo_Anterior, Costo_Equipo_Nuevo, ID_Almacen,
             ID_PreFactura, ID_Reparacion, Tecnico, Estado_Custodia, Usuario_Registro, Fecha_Registro,
             ID_Modulo_Retirado, ID_Modulo_Instalado)
            VALUES
                (@ID_Cambio, @Imei, @Tipo_Pieza, @ID_Accesorio_Instalado, @Costo_Instalado, @ID_Accesorio_Retirado, @Costo_Retirado,
             @Config_Anterior, @Config_Nueva, @Costo_Anterior, @Costo_Nuevo, @ID_Almacen,
             @ID_PreFactura, @ID_Reparacion, @Tecnico, @Estado_Custodia, @Usuario, DATEADD(HOUR, -4, GETDATE()),
             @ID_Modulo_Retirado, @ID_Modulo_Nuevo);

            SET IDENTITY_INSERT dbo.Equipo_Cambio_Pieza OFF;
            SET @ID_Nuevo = @ID_Cambio;
        END

        -- El cambio que dio de baja / alta a cada modulo
        IF @ID_Modulo_Retirado IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET ID_Cambio_Baja = @ID_Nuevo WHERE ID_Modulo = @ID_Modulo_Retirado;

        UPDATE dbo.Equipo_Configuracion_Modulo SET ID_Cambio_Alta = @ID_Nuevo WHERE ID_Modulo = @ID_Modulo_Nuevo;

        -- Precios de venta anteriores (para poder restablecerlos si el cambio se anula)
        UPDATE dbo.Equipo_Cambio_Pieza
           SET Precio_Detalle_Anterior = @Precio_Detalle_Ant,
               Precio_Por_Mayor_Anterior = @Precio_Mayor_Ant
         WHERE ID_Cambio = @ID_Nuevo;

        COMMIT TRAN;

        -- --------------------------------------------------------
        -- 8. Devolver el registro insertado (Accesorios_Creados = JSON para replicar los mismos ID)
        -- --------------------------------------------------------
        SELECT C.*,
               Accesorios_Creados = @Creados,
               Mensaje = 'Cambio de pieza registrado satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Nuevo;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        SET IDENTITY_INSERT dbo.Equipo_Cambio_Pieza OFF;
        SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo OFF;
        SET IDENTITY_INSERT dbo.Accesorios OFF;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- dbo.Equipo_Cambio_Pieza_Recibir
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Recibir
    @ID_Cambio         INT,
    @Usuario           VARCHAR(60),
    @Nota              VARCHAR(250) = NULL,
    @Costo_Equipo      DECIMAL(18,2) = NULL,     -- solo si el accesorio no tiene costo / precios
    @Precio_Por_Mayor  DECIMAL(18,2) = NULL,
    @Precio_Detalle    DECIMAL(18,2) = NULL,
    @ID_Accesorio_Nuevo BIGINT = NULL,           -- solo al replicar (mismo ID del accesorio creado, Tarea 4)
    @Accesorios_Ids     NVARCHAR(MAX) = NULL,    -- NUEVO: solo al replicar (JSON de las piezas usadas creadas)
    @Porcentaje_Costo_Usado DECIMAL(5,2) = 50    -- NUEVO: Laptops:PorcentajeCostoUsado
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50100, 'El usuario que recibe es requerido.', 1;

        BEGIN TRAN;

        DECLARE @Estado VARCHAR(20), @Tecnico VARCHAR(60), @ID_Almacen INT, @ID_Accesorio_Retirado BIGINT;

        SELECT @Estado = C.Estado_Custodia,
               @Tecnico = C.Tecnico,
               @ID_Almacen = C.ID_Almacen,
               @ID_Accesorio_Retirado = C.ID_Accesorio_Retirado
        FROM dbo.Equipo_Cambio_Pieza C WITH (UPDLOCK, ROWLOCK)
        WHERE C.ID_Cambio = @ID_Cambio;

        IF @Estado IS NULL
            THROW 50101, 'El cambio de pieza no existe.', 1;

        IF @Estado <> 'PENDIENTE'
            THROW 50102, 'Solo se puede recibir una pieza en estado PENDIENTE.', 1;

        -- R9: la recepcion la confirma un usuario distinto al tecnico
        IF LTRIM(RTRIM(@Usuario)) = LTRIM(RTRIM(@Tecnico))
            THROW 50103, 'La recepcion debe confirmarla un usuario distinto al tecnico que retiro la pieza.', 1;

        IF @ID_Accesorio_Retirado IS NULL
            THROW 50104, 'El cambio no tiene pieza retirada.', 1;

        -- --------------------------------------------------------
        -- Fila del accesorio retirado en el almacen del cambio
        -- --------------------------------------------------------
        DECLARE @ID_Accesorio_Destino BIGINT, @ID_Accesorio_Creado BIGINT = NULL, @Creados NVARCHAR(MAX) = NULL;

        -- Cambios anteriores a la Tarea 5: el accesorio retirado es la pieza NUEVA; la pieza entra a su variante USADA
        DECLARE @R_Modelo INT, @R_Desc VARCHAR(100), @R_Cap INT;
        SELECT @R_Modelo = A.ID_Modelo, @R_Desc = A.Descripcion_Accesorio
          FROM dbo.Accesorios A
         WHERE A.ID_Accesorio = @ID_Accesorio_Retirado;
        SET @R_Cap = dbo.fn_Componente_Capacidad_GB(@R_Desc);

        IF @R_Modelo IS NOT NULL AND @R_Cap IS NOT NULL AND @R_Desc NOT LIKE '%(Usado)'
           AND EXISTS (SELECT 1 FROM dbo.Componente_Modelo_Tipo WHERE ID_Modelo = @R_Modelo)
            EXEC dbo.Accesorio_Componente_Asegurar
                 @ID_Modelo = @R_Modelo, @Capacidad_GB = @R_Cap, @ID_Almacen = @ID_Almacen,
                 @Usuario = @Usuario, @Ids_Explicitos = @Accesorios_Ids,
                 @ID_Accesorio = @ID_Accesorio_Destino OUTPUT, @Creados = @Creados OUTPUT,
                 @Usado = 1, @Porcentaje_Costo_Usado = @Porcentaje_Costo_Usado;

        IF @ID_Accesorio_Destino IS NULL
            SELECT @ID_Accesorio_Destino = A.ID_Accesorio
            FROM dbo.Accesorios A
            WHERE A.ID_Accesorio = @ID_Accesorio_Retirado
              AND A.ID_Almacen = @ID_Almacen;

        IF @ID_Accesorio_Destino IS NULL
        BEGIN
            -- El accesorio retirado es de otro almacen: buscar el equivalente en el almacen del cambio
            SELECT TOP 1 @ID_Accesorio_Destino = D.ID_Accesorio
            FROM dbo.Accesorios O
            INNER JOIN dbo.Accesorios D
                ON D.ID_Almacen = @ID_Almacen
               AND ((O.Codigo_Barra IS NOT NULL AND O.Codigo_Barra <> 0 AND D.Codigo_Barra = O.Codigo_Barra)
                    OR (D.ID_Modelo = O.ID_Modelo AND D.Descripcion_Accesorio = O.Descripcion_Accesorio))
            WHERE O.ID_Accesorio = @ID_Accesorio_Retirado
            ORDER BY D.ID_Accesorio;
        END

        IF @ID_Accesorio_Destino IS NULL
        BEGIN
            -- No existe en el almacen del cambio: se crea copiando el accesorio retirado (Cantidad = 0)
            IF NOT EXISTS (SELECT 1 FROM dbo.Accesorios WHERE ID_Accesorio = @ID_Accesorio_Retirado)
                THROW 50105, 'El accesorio retirado ya no existe en el catalogo; no se puede recibir la pieza.', 1;

            IF @ID_Accesorio_Nuevo IS NOT NULL
               AND EXISTS (SELECT 1 FROM dbo.Accesorios WHERE ID_Accesorio = @ID_Accesorio_Nuevo)
                THROW 50110, 'Error: el ID de accesorio indicado ya existe en esta base.', 1;

            IF @ID_Accesorio_Nuevo IS NULL
            BEGIN
                INSERT INTO dbo.Accesorios
                    (Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura, Descripcion_Accesorio, Cantidad, Can_Max, Can_Min,
                     Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado, Usuario)
                SELECT O.Imei_Equipo, O.ID_Modelo, @ID_Almacen, O.Numero_de_Factura, O.Descripcion_Accesorio, 0, O.Can_Max, O.Can_Min,
                       O.Precio_Por_Mayor, O.Precio_Detalle, O.Costo_Equipo, O.Disponible, O.Disponible_Detalle, O.Estado, 'Sistema'
                  FROM dbo.Accesorios O
                 WHERE O.ID_Accesorio = @ID_Accesorio_Retirado;
                SET @ID_Accesorio_Creado = SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                -- Replicacion: mismo ID que en la base de trabajo
                SET IDENTITY_INSERT dbo.Accesorios ON;
                INSERT INTO dbo.Accesorios
                    (ID_Accesorio, Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura, Descripcion_Accesorio, Cantidad, Can_Max, Can_Min,
                     Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado, Usuario)
                SELECT @ID_Accesorio_Nuevo, O.Imei_Equipo, O.ID_Modelo, @ID_Almacen, O.Numero_de_Factura, O.Descripcion_Accesorio, 0, O.Can_Max, O.Can_Min,
                       O.Precio_Por_Mayor, O.Precio_Detalle, O.Costo_Equipo, O.Disponible, O.Disponible_Detalle, O.Estado, 'Sistema'
                  FROM dbo.Accesorios O
                 WHERE O.ID_Accesorio = @ID_Accesorio_Retirado;
                SET IDENTITY_INSERT dbo.Accesorios OFF;
                SET @ID_Accesorio_Creado = @ID_Accesorio_Nuevo;
            END

            UPDATE dbo.Accesorios SET Codigo_Barra = 1000000000 + @ID_Accesorio_Creado WHERE ID_Accesorio = @ID_Accesorio_Creado;
            SET @ID_Accesorio_Destino = @ID_Accesorio_Creado;
        END

        -- --------------------------------------------------------
        -- Costo y precios del accesorio (obligatorios si no los tiene)
        -- --------------------------------------------------------
        DECLARE @ID_Modelo_Acc INT, @Desc_Acc VARCHAR(100), @Costo_Acc MONEY, @Mayor_Acc MONEY, @Detalle_Acc MONEY;

        SELECT @ID_Modelo_Acc = A.ID_Modelo, @Desc_Acc = A.Descripcion_Accesorio,
               @Costo_Acc = A.Costo_Equipo, @Mayor_Acc = A.Precio_Por_Mayor, @Detalle_Acc = A.Precio_Detalle
          FROM dbo.Accesorios A
         WHERE A.ID_Accesorio = @ID_Accesorio_Destino;

        IF ISNULL(@Costo_Acc, 0) <= 0 OR ISNULL(@Mayor_Acc, 0) <= 0 OR ISNULL(@Detalle_Acc, 0) <= 0
        BEGIN
            IF ISNULL(@Costo_Equipo, 0) <= 0 OR ISNULL(@Precio_Por_Mayor, 0) <= 0 OR ISNULL(@Precio_Detalle, 0) <= 0
                THROW 50107, 'El accesorio no tiene costo y precios registrados. Indique el costo, el precio por mayor y el precio al detalle.', 1;

            IF @Costo_Equipo > @Precio_Por_Mayor
                THROW 50108, 'El costo no puede ser mayor que el precio por mayor.', 1;

            IF @Precio_Por_Mayor > @Precio_Detalle
                THROW 50109, 'El precio por mayor no puede ser mayor que el precio al detalle.', 1;

            -- Todos los almacenes con el mismo modelo y descripcion; el accesorio queda activo
            UPDATE dbo.Accesorios
               SET Costo_Equipo     = @Costo_Equipo,
                   Precio_Por_Mayor = @Precio_Por_Mayor,
                   Precio_Detalle   = @Precio_Detalle,
                   Estado           = 'A',
                   Usuario          = LEFT(@Usuario, 50)
             WHERE ID_Modelo = @ID_Modelo_Acc
               AND Descripcion_Accesorio = @Desc_Acc;
        END

        -- --------------------------------------------------------
        -- Sumar 1 al inventario y marcar como recibida
        -- --------------------------------------------------------
        UPDATE dbo.Accesorios
           SET Cantidad = ISNULL(Cantidad, 0) + 1
         WHERE ID_Accesorio = @ID_Accesorio_Destino;

        UPDATE dbo.Equipo_Cambio_Pieza
           SET Estado_Custodia = 'RECIBIDA',
               Usuario_Recepcion = @Usuario,
               Fecha_Recepcion = DATEADD(HOUR, -4, GETDATE()),
               Nota_Recepcion = @Nota
         WHERE ID_Cambio = @ID_Cambio
           AND Estado_Custodia = 'PENDIENTE';

        IF @@ROWCOUNT = 0
            THROW 50106, 'La pieza ya fue procesada por otro usuario.', 1;

        COMMIT TRAN;

        SELECT C.*, Accesorio_Creado = @ID_Accesorio_Creado, Accesorios_Creados = @Creados, Mensaje = 'Pieza recibida satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        SET IDENTITY_INSERT dbo.Accesorios OFF;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- dbo.Equipo_Cambio_Pieza_EntregarCliente
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_EntregarCliente
    @ID_Cambio              INT,
    @Usuario                VARCHAR(60),
    @Nota                   VARCHAR(250) = NULL,
    @Nuevo_Precio_Detalle   DECIMAL(18,2) = NULL,
    @Nuevo_Precio_Por_Mayor DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50200, 'El usuario es requerido.', 1;

        BEGIN TRAN;

        DECLARE @Estado VARCHAR(20), @Imei VARCHAR(30), @Costo_Retirado DECIMAL(18,2);

        SELECT @Estado = C.Estado_Custodia,
               @Imei = C.Imei,
               @Costo_Retirado = C.Costo_Retirado
        FROM dbo.Equipo_Cambio_Pieza C WITH (UPDLOCK, ROWLOCK)
        WHERE C.ID_Cambio = @ID_Cambio;

        IF @Estado IS NULL
            THROW 50201, 'El cambio de pieza no existe.', 1;

        IF @Estado <> 'PENDIENTE'
            THROW 50202, 'Solo se puede entregar al cliente una pieza en estado PENDIENTE.', 1;

        -- Laptop: existe y no esta vendida
        DECLARE @Disponible CHAR(1), @Costo_Actual DECIMAL(18,2),
                @Precio_Detalle DECIMAL(18,2), @Precio_Mayor DECIMAL(18,2);

        SELECT TOP 1
               @Disponible = E.Disponible,
               @Costo_Actual = CAST(E.Costo_Equipo AS DECIMAL(18,2)),
               @Precio_Detalle = CAST(E.Precio_Detalle AS DECIMAL(18,2)),
               @Precio_Mayor = CAST(E.Precio_Por_Mayor AS DECIMAL(18,2))
        FROM dbo.Equipos E WITH (UPDLOCK, ROWLOCK)
        WHERE E.Imei = @Imei;

        IF @Disponible IS NULL
            THROW 50203, 'La laptop del cambio no existe.', 1;

        IF @Disponible <> 'S'
            THROW 50204, 'La laptop ya no esta disponible (vendida o no disponible). No se puede entregar la pieza al cliente.', 1;

        -- Una prefactura pendiente guarda el costo de la laptop al crearse; no se puede cambiar el costo
        -- antes de procesarla (la utilidad saldria con el costo viejo).
        DECLARE @PreFactura_Pendiente INT = (
            SELECT TOP 1 pf.Numero_PreFactura
              FROM dbo.PreDetalle_Facturas pf
              INNER JOIN dbo.PreFacturas p ON p.Numero_PreFactura = pf.Numero_PreFactura
             WHERE pf.ID_Equipo = @Imei AND pf.ID_Tipo = 1 AND pf.Estado = 'P' AND p.Estado = 'P'
             ORDER BY pf.Numero_PreFactura DESC);

        IF @PreFactura_Pendiente IS NOT NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La laptop esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de entregar la pieza al cliente.');
            THROW 50206, @MsgPre, 1;
        END

        -- R10: se vuelve a sumar el costo de la pieza que se va con la laptop
        DECLARE @Costo_Nuevo DECIMAL(18,2) = @Costo_Actual + @Costo_Retirado;

        -- Precios: el costo nuevo no puede superar los precios de venta
        DECLARE @Precio_Detalle_Nuevo DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Detalle, @Precio_Detalle);
        DECLARE @Precio_Mayor_Nuevo   DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Por_Mayor, @Precio_Mayor);
        DECLARE @MsgPrecio NVARCHAR(400);

        IF @Precio_Detalle_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio al detalle (', @Precio_Detalle_Nuevo, '). Indique un precio al detalle nuevo.');
            THROW 50241, @MsgPrecio, 1;
        END

        IF @Precio_Mayor_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio por mayor (', @Precio_Mayor_Nuevo, '). Indique un precio por mayor nuevo.');
            THROW 50242, @MsgPrecio, 1;
        END

        IF (@Nuevo_Precio_Detalle IS NOT NULL OR @Nuevo_Precio_Por_Mayor IS NOT NULL)
           AND @Precio_Mayor_Nuevo > @Precio_Detalle_Nuevo
            THROW 50243, 'El precio por mayor no puede ser mayor que el precio al detalle.', 1;

        -- Foto de la fila actual en el historico de equipos (antes de modificar costo/precios)
        INSERT INTO dbo.Equipos_Historico
            (Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
             Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
             Nota_Historico, Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
             Usuario_Historico, Fecha_registro_Historico, Comision_Detalle, Comision_Por_Mayor)
        SELECT Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
               Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
               'Pieza retirada entregada al cliente: se suma su costo a la laptop', Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
               @Usuario, DATEADD(HOUR, -4, GETDATE()), Comision_Detalle, Comision_Por_Mayor
          FROM dbo.Equipos
         WHERE Imei = @Imei;

        UPDATE dbo.Equipos
           SET Costo_Equipo = @Costo_Nuevo,
               Precio_Detalle = @Precio_Detalle_Nuevo,
               Precio_Por_Mayor = @Precio_Mayor_Nuevo
         WHERE Imei = @Imei;

        UPDATE dbo.Equipo_Cambio_Pieza
           SET Estado_Custodia = 'ENTREGADA_CLIENTE',
               Usuario_Recepcion = @Usuario,
               Fecha_Recepcion = DATEADD(HOUR, -4, GETDATE()),
               Nota_Recepcion = @Nota
         WHERE ID_Cambio = @ID_Cambio
           AND Estado_Custodia = 'PENDIENTE';

        IF @@ROWCOUNT = 0
            THROW 50205, 'La pieza ya fue procesada por otro usuario.', 1;

        COMMIT TRAN;

        SELECT C.*, Costo_Equipo_Actual = @Costo_Nuevo, Mensaje = 'Pieza entregada al cliente satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- dbo.Equipo_Cambio_Pieza_Pendientes
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Pendientes
    @Tecnico     VARCHAR(60) = NULL,
    @ID_Almacen  INT = NULL,
    @Horas_Max   INT = 24
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Cambio,
           C.Imei,
           Modelo = M.Descripcion,
           Marca = Ma.Descripcion,
           Laptop_Disponible = E.Disponible,
           C.Tipo_Pieza,
           C.ID_Accesorio_Retirado,
           Pieza_Retirada = AR.Descripcion_Accesorio,
           C.Costo_Retirado,
           C.Tecnico,
           C.ID_Almacen,
           AC.Descripcion_Almacen,
           C.Usuario_Registro,
           C.Fecha_Registro,
           Horas_Transcurridas = CAST(DATEDIFF(MINUTE, C.Fecha_Registro, DATEADD(HOUR, -4, GETDATE())) / 60.0 AS DECIMAL(10,1)),
           Vencido = CAST(CASE WHEN DATEDIFF(MINUTE, C.Fecha_Registro, DATEADD(HOUR, -4, GETDATE())) > @Horas_Max * 60 THEN 1 ELSE 0 END AS BIT),
           -- NUEVO: el accesorio retirado no tiene costo y precios; la recepcion los pide
           Requiere_Precios = CAST(CASE WHEN ISNULL(AR.Costo_Equipo, 0) <= 0
                                          OR ISNULL(AR.Precio_Por_Mayor, 0) <= 0
                                          OR ISNULL(AR.Precio_Detalle, 0) <= 0
                                        THEN 1 ELSE 0 END AS BIT)
    FROM dbo.Equipo_Cambio_Pieza C
    INNER JOIN dbo.Equipos E ON E.Imei = C.Imei
    INNER JOIN dbo.Modelos M ON M.ID_Modelo = E.ID_Modelo
    INNER JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
    LEFT JOIN dbo.Accesorios AR ON AR.ID_Accesorio = C.ID_Accesorio_Retirado
    LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = C.ID_Almacen
    WHERE C.Estado_Custodia = 'PENDIENTE'
      AND (@Tecnico IS NULL OR C.Tecnico = @Tecnico)
      AND (@ID_Almacen IS NULL OR C.ID_Almacen = @ID_Almacen)
    ORDER BY C.Tecnico, C.Fecha_Registro, C.ID_Cambio;
END
GO

-- ------------------------------------------------------------
-- dbo.Equipo_Cambio_Pieza_Anular
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Anular
    @ID_Cambio INT,
    @Usuario   VARCHAR(60),
    @Motivo    VARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50600, 'El usuario que anula es requerido.', 1;

        IF @Motivo IS NULL OR LTRIM(RTRIM(@Motivo)) = ''
            THROW 50601, 'El motivo de la anulacion es requerido.', 1;

        BEGIN TRAN;

        DECLARE @Imei VARCHAR(30), @Estado VARCHAR(20), @ID_Almacen INT, @ID_Accesorio_Instalado BIGINT,
                @Mod_Ret INT, @Mod_Inst INT, @Costo_Ant DECIMAL(18,2),
                @PrecioDet_Ant DECIMAL(18,2), @PrecioMay_Ant DECIMAL(18,2);

        SELECT @Imei = C.Imei, @Estado = C.Estado_Custodia, @ID_Almacen = C.ID_Almacen,
               @ID_Accesorio_Instalado = C.ID_Accesorio_Instalado,
               @Mod_Ret = C.ID_Modulo_Retirado, @Mod_Inst = C.ID_Modulo_Instalado,
               @Costo_Ant = C.Costo_Equipo_Anterior,
               @PrecioDet_Ant = C.Precio_Detalle_Anterior, @PrecioMay_Ant = C.Precio_Por_Mayor_Anterior
          FROM dbo.Equipo_Cambio_Pieza C WITH (UPDLOCK, ROWLOCK)
         WHERE C.ID_Cambio = @ID_Cambio;

        IF @Imei IS NULL
            THROW 50602, 'El cambio de pieza no existe.', 1;

        IF @Estado = 'ANULADO'
            THROW 50603, 'El cambio de pieza ya fue anulado.', 1;

        IF @Estado NOT IN ('PENDIENTE', 'NO_APLICA')
            THROW 50604, 'No se puede anular: la pieza retirada ya fue recibida o entregada al cliente, y ese movimiento ya afecto el inventario o el costo.', 1;

        IF EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza X
                    WHERE X.Imei = @Imei AND X.ID_Cambio > @ID_Cambio AND X.Estado_Custodia <> 'ANULADO')
            THROW 50605, 'Solo se puede anular el ultimo cambio de pieza de la laptop.', 1;

        DECLARE @Disponible CHAR(1);
        SELECT TOP 1 @Disponible = E.Disponible
          FROM dbo.Equipos E WITH (UPDLOCK, ROWLOCK)
         WHERE E.Imei = @Imei;

        IF @Disponible IS NULL OR @Disponible <> 'S'
            THROW 50606, 'La laptop no esta disponible (vendida o no disponible). No se puede anular el cambio.', 1;

        DECLARE @PreFactura_Pendiente INT = (
            SELECT TOP 1 pf.Numero_PreFactura
              FROM dbo.PreDetalle_Facturas pf
              INNER JOIN dbo.PreFacturas p ON p.Numero_PreFactura = pf.Numero_PreFactura
             WHERE pf.ID_Equipo = @Imei AND pf.ID_Tipo = 1 AND pf.Estado = 'P' AND p.Estado = 'P'
             ORDER BY pf.Numero_PreFactura DESC);

        IF @PreFactura_Pendiente IS NOT NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La laptop esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de anular el cambio de pieza.');
            THROW 50607, @MsgPre, 1;
        END

        -- Foto de la fila actual en el historico de equipos (antes de revertir costo / precios)
        INSERT INTO dbo.Equipos_Historico
            (Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
             Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
             Nota_Historico, Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
             Usuario_Historico, Fecha_registro_Historico, Comision_Detalle, Comision_Por_Mayor)
        SELECT Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
               Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
               'Anulacion de cambio de pieza de laptop: se restablece costo y/o precios', Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
               @Usuario, DATEADD(HOUR, -4, GETDATE()), Comision_Detalle, Comision_Por_Mayor
          FROM dbo.Equipos
         WHERE Imei = @Imei;

        -- Costo (y precios, si el cambio los guardo) vuelven a como estaban
        UPDATE dbo.Equipos
           SET Costo_Equipo     = @Costo_Ant,
               Precio_Detalle   = ISNULL(@PrecioDet_Ant, Precio_Detalle),
               Precio_Por_Mayor = ISNULL(@PrecioMay_Ant, Precio_Por_Mayor)
         WHERE Imei = @Imei;

        -- La pieza instalada vuelve al inventario del almacen del cambio
        UPDATE dbo.Accesorios
           SET Cantidad = ISNULL(Cantidad, 0) + 1
         WHERE ID_Accesorio = @ID_Accesorio_Instalado
           AND ID_Almacen = @ID_Almacen;

        -- Modulos: el instalado pasa a retirado y el que salio vuelve a estar instalado
        IF @Mod_Inst IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET Estado = 'R' WHERE ID_Modulo = @Mod_Inst;

        IF @Mod_Ret IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET Estado = 'A', ID_Cambio_Baja = NULL WHERE ID_Modulo = @Mod_Ret;

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;

        UPDATE dbo.Equipo_Cambio_Pieza
           SET Estado_Custodia = 'ANULADO',
               Usuario_Anula   = @Usuario,
               Fecha_Anula     = DATEADD(HOUR, -4, GETDATE()),
               Motivo_Anula    = LTRIM(RTRIM(@Motivo))
         WHERE ID_Cambio = @ID_Cambio;

        COMMIT TRAN;

        SELECT C.*, Configuracion = dbo.fn_Equipo_Configuracion_Texto(C.Imei),
               Mensaje = 'Cambio de pieza anulado satisfactoriamente'
          FROM dbo.Equipo_Cambio_Pieza C
         WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- dbo.Equipo_Configuracion_Recalcular
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Recalcular
    @Imei    VARCHAR(30),
    @Usuario VARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE C
       SET Ram_GB = (SELECT SUM(M.Capacidad_GB)
                       FROM dbo.Equipo_Configuracion_Modulo M
                      WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM'),
           Ram_Tipo = (SELECT TOP 1 M.Tipo
                         FROM dbo.Equipo_Configuracion_Modulo M
                        WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM'
                        ORDER BY M.ID_Modulo),
           Almacenamiento_GB = (SELECT SUM(M.Capacidad_GB)
                                  FROM dbo.Equipo_Configuracion_Modulo M
                                 WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'ALMACENAMIENTO'),
           Almacenamiento_Tipo = (SELECT TOP 1 M.Tipo
                                    FROM dbo.Equipo_Configuracion_Modulo M
                                   WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'ALMACENAMIENTO'
                                   ORDER BY M.ID_Modulo),
           Usuario_Modifica = ISNULL(@Usuario, C.Usuario_Modifica),
           Fecha_Modifica   = DATEADD(HOUR, -4, GETDATE())
      FROM dbo.Equipo_Configuracion C
     WHERE C.Imei = @Imei;
END
GO

-- ------------------------------------------------------------
-- dbo.Equipo_Configuracion_Modulo_Corregir
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Modulo_Corregir
    @ID_Modulo            INT,
    @ID_Modelo_Componente INT,
    @Capacidad_GB         INT,
    @Motivo               VARCHAR(250),
    @Usuario              VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50500, 'El usuario es requerido.', 1;

        IF @Motivo IS NULL OR LTRIM(RTRIM(@Motivo)) = ''
            THROW 50501, 'El motivo de la correccion es requerido.', 1;

        IF @Capacidad_GB IS NULL OR @Capacidad_GB <= 0 OR @Capacidad_GB > 16000
            THROW 50502, 'La capacidad debe estar entre 1 y 16000 GB.', 1;

        DECLARE @Imei VARCHAR(30), @Tipo_Pieza VARCHAR(20), @Estado CHAR(1),
                @Cap_Ant INT, @Modelo_Ant INT, @Tipo_Ant VARCHAR(30);

        SELECT @Imei = M.Imei, @Tipo_Pieza = M.Tipo_Pieza, @Estado = M.Estado,
               @Cap_Ant = M.Capacidad_GB, @Modelo_Ant = M.ID_Modelo_Componente, @Tipo_Ant = M.Tipo
          FROM dbo.Equipo_Configuracion_Modulo M WITH (UPDLOCK, ROWLOCK)
         WHERE M.ID_Modulo = @ID_Modulo;

        IF @Imei IS NULL
            THROW 50503, 'El modulo no existe.', 1;

        IF @Estado <> 'A'
            THROW 50504, 'Solo se puede corregir un modulo instalado.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Equipos WHERE Imei = @Imei AND Disponible = 'S')
            THROW 50505, 'La laptop no esta disponible (vendida o no disponible). No se puede corregir su configuracion.', 1;

        DECLARE @Tipo_Nuevo VARCHAR(30), @Pieza_Nueva VARCHAR(20);

        SELECT @Tipo_Nuevo = T.Tipo, @Pieza_Nueva = T.Tipo_Pieza
          FROM dbo.Componente_Modelo_Tipo T
         WHERE T.ID_Modelo = @ID_Modelo_Componente;

        IF @Tipo_Nuevo IS NULL
            THROW 50506, 'El modelo indicado no es un modelo de componente.', 1;

        IF @Pieza_Nueva <> @Tipo_Pieza
            THROW 50507, 'El modelo indicado no corresponde al tipo de pieza del modulo (RAM / almacenamiento).', 1;

        IF @Tipo_Pieza = 'RAM'
           AND EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo X
                        WHERE X.Imei = @Imei AND X.Estado = 'A' AND X.Tipo_Pieza = 'RAM'
                          AND X.ID_Modulo <> @ID_Modulo AND X.Tipo <> @Tipo_Nuevo)
            THROW 50508, 'No se pueden mezclar tipos de RAM (DDR3 / DDR4 / DDR5) en una misma laptop.', 1;

        IF @Modelo_Ant = @ID_Modelo_Componente AND @Cap_Ant = @Capacidad_GB
            THROW 50509, 'No hay cambios: el modulo ya tiene ese modelo y esa capacidad.', 1;

        DECLARE @Antes VARCHAR(100) =
            CONCAT(@Cap_Ant, 'GB ', (SELECT Descripcion FROM dbo.Modelos WHERE ID_Modelo = @Modelo_Ant));
        DECLARE @Despues VARCHAR(100) =
            CONCAT(@Capacidad_GB, 'GB ', (SELECT Descripcion FROM dbo.Modelos WHERE ID_Modelo = @ID_Modelo_Componente));

        BEGIN TRAN;

        UPDATE dbo.Equipo_Configuracion_Modulo
           SET ID_Modelo_Componente = @ID_Modelo_Componente,
               Tipo                 = @Tipo_Nuevo,
               Capacidad_GB         = @Capacidad_GB
         WHERE ID_Modulo = @ID_Modulo;

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;

        INSERT INTO dbo.Equipo_Configuracion_Correccion (Imei, ID_Modulo, Antes, Despues, Motivo, Usuario, Fecha)
        VALUES (@Imei, @ID_Modulo, @Antes, @Despues, LTRIM(RTRIM(@Motivo)), @Usuario, DATEADD(HOUR, -4, GETDATE()));

        COMMIT TRAN;

        SELECT @ID_Modulo AS ID_Modulo, @Imei AS Imei,
               Configuracion = dbo.fn_Equipo_Configuracion_Texto(@Imei),
               Mensaje = 'Modulo corregido satisfactoriamente';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- dbo.Equipo_Configuracion_Guardar
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Guardar
    @Imei                VARCHAR(30),
    @Ram_GB              INT = NULL,
    @Ram_Tipo            VARCHAR(30) = NULL,
    @Almacenamiento_GB   INT = NULL,
    @Almacenamiento_Tipo VARCHAR(30) = NULL,
    @Notas               VARCHAR(250) = NULL,
    @Usuario             VARCHAR(60),
    @Modulos             NVARCHAR(MAX) = NULL      -- NUEVO: lista de modulos en JSON
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- R2: se obtiene el ID del grupo por descripcion una sola vez y se compara por ID.
    DECLARE @ID_Grupo_Laptops INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');
    DECLARE @ID_Modelo INT;

    IF @ID_Grupo_Laptops IS NULL
    BEGIN
        RAISERROR('No existe el grupo Laptops. Ejecute CREATE_Laptops_Fase1_Grupos.sql', 16, 1);
        RETURN;
    END

    SELECT TOP 1 @ID_Modelo = ID_Modelo FROM dbo.Equipos WHERE Imei = @Imei;

    IF @ID_Modelo IS NULL
    BEGIN
        RAISERROR('El equipo con serial/IMEI %s no existe.', 16, 1, @Imei);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Modelos WHERE ID_Modelo = @ID_Modelo AND ID_Grupo = @ID_Grupo_Laptops)
    BEGIN
        RAISERROR('El equipo %s no es una laptop (su modelo no pertenece al grupo Laptops).', 16, 1, @Imei);
        RETURN;
    END

    -- --------------------------------------------------------
    -- Lista de modulos (JSON, o armada con los parametros actuales)
    -- --------------------------------------------------------
    DECLARE @Mod TABLE
    (
        Orden        INT IDENTITY(1, 1) PRIMARY KEY,
        ID_Modulo    INT NULL,
        Tipo_Pieza   VARCHAR(20) NOT NULL,
        Capacidad_GB INT NOT NULL,
        ID_Modelo    INT NOT NULL
    );

    IF @Modulos IS NOT NULL
    BEGIN
        IF ISJSON(@Modulos) <> 1
            THROW 50520, 'El formato de los modulos no es un JSON valido.', 1;

        INSERT INTO @Mod (ID_Modulo, Tipo_Pieza, Capacidad_GB, ID_Modelo)
        SELECT j.idModulo, UPPER(j.tipoPieza), j.capacidadGB, j.idModeloComponente
          FROM OPENJSON(@Modulos)
               WITH (idModulo           INT         '$.idModulo',
                     tipoPieza          VARCHAR(20) '$.tipoPieza',
                     capacidadGB        INT         '$.capacidadGB',
                     idModeloComponente INT         '$.idModeloComponente') AS j;

        IF NOT EXISTS (SELECT 1 FROM @Mod)
            THROW 50521, 'Debe indicar al menos un modulo (memoria o disco).', 1;
    END
    ELSE
    BEGIN
        -- Compatibilidad: un modulo por RAM y uno por almacenamiento. El modelo sale del tipo
        -- (SSD SATA -> 'SSD SATA 2.5' por defecto).
        IF @Ram_GB IS NOT NULL
        BEGIN
            DECLARE @ModeloRam INT;
            SELECT TOP 1 @ModeloRam = T.ID_Modelo
              FROM dbo.Componente_Modelo_Tipo T
             WHERE T.Tipo_Pieza = 'RAM' AND T.Tipo = @Ram_Tipo
             ORDER BY T.ID_Modelo;

            IF @ModeloRam IS NULL
                THROW 50522, 'El tipo de RAM es requerido y debe ser DDR3, DDR4 o DDR5.', 1;

            INSERT INTO @Mod (ID_Modulo, Tipo_Pieza, Capacidad_GB, ID_Modelo) VALUES (NULL, 'RAM', @Ram_GB, @ModeloRam);
        END

        IF @Almacenamiento_GB IS NOT NULL
        BEGIN
            DECLARE @ModeloDisco INT;
            SELECT TOP 1 @ModeloDisco = T.ID_Modelo
              FROM dbo.Componente_Modelo_Tipo T
              INNER JOIN dbo.Modelos Mo ON Mo.ID_Modelo = T.ID_Modelo
             WHERE T.Tipo_Pieza = 'ALMACENAMIENTO' AND T.Tipo = @Almacenamiento_Tipo
             ORDER BY CASE WHEN Mo.Descripcion = 'SSD SATA 2.5' THEN 0 ELSE 1 END, T.ID_Modelo;

            IF @ModeloDisco IS NULL
                THROW 50523, 'El tipo de almacenamiento es requerido y debe ser SSD SATA, SSD NVMe o HDD.', 1;

            INSERT INTO @Mod (ID_Modulo, Tipo_Pieza, Capacidad_GB, ID_Modelo) VALUES (NULL, 'ALMACENAMIENTO', @Almacenamiento_GB, @ModeloDisco);
        END
    END

    -- --------------------------------------------------------
    -- Validaciones de los modulos
    -- --------------------------------------------------------
    IF EXISTS (SELECT 1 FROM @Mod)
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza WHERE Imei = @Imei)
            THROW 50524, 'La laptop ya tiene cambios de pieza: su configuracion no se puede reemplazar. Use la correccion de modulos.', 1;

        IF EXISTS (SELECT 1 FROM @Mod WHERE Tipo_Pieza NOT IN ('RAM', 'ALMACENAMIENTO'))
            THROW 50525, 'El tipo de pieza de un modulo debe ser RAM o ALMACENAMIENTO.', 1;

        IF EXISTS (SELECT 1 FROM @Mod WHERE Capacidad_GB IS NULL OR Capacidad_GB <= 0 OR Capacidad_GB > 16000)
            THROW 50526, 'La capacidad de cada modulo debe estar entre 1 y 16000 GB.', 1;

        IF EXISTS (SELECT 1
                     FROM @Mod X
                     LEFT JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo AND T.Tipo_Pieza = X.Tipo_Pieza
                    WHERE T.ID_Modelo IS NULL)
            THROW 50527, 'Un modulo tiene un modelo de componente invalido o que no corresponde a su tipo de pieza.', 1;

        IF (SELECT COUNT(DISTINCT T.Tipo)
              FROM @Mod X
              INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo
             WHERE X.Tipo_Pieza = 'RAM') > 1
            THROW 50528, 'No se pueden mezclar tipos de RAM (DDR3 / DDR4 / DDR5) en una misma laptop.', 1;

        IF EXISTS (SELECT ID_Modulo FROM @Mod WHERE ID_Modulo IS NOT NULL GROUP BY ID_Modulo HAVING COUNT(*) > 1)
            THROW 50529, 'La lista de modulos repite un ID de modulo.', 1;

        IF EXISTS (SELECT 1
                     FROM @Mod X
                    INNER JOIN dbo.Equipo_Configuracion_Modulo M ON M.ID_Modulo = X.ID_Modulo
                    WHERE M.Imei <> @Imei)
            THROW 50530, 'Error: un ID de modulo ya lo usa otra laptop.', 1;
    END

    -- --------------------------------------------------------
    -- Guardar
    -- --------------------------------------------------------
    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion WHERE Imei = @Imei)
    BEGIN
        UPDATE dbo.Equipo_Configuracion
        SET Ram_GB              = @Ram_GB,
            Ram_Tipo            = @Ram_Tipo,
            Almacenamiento_GB   = @Almacenamiento_GB,
            Almacenamiento_Tipo = @Almacenamiento_Tipo,
            Notas               = @Notas,
            Usuario_Modifica    = @Usuario,
            Fecha_Modifica      = DATEADD(HOUR, -4, GETDATE())
        WHERE Imei = @Imei;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Equipo_Configuracion
            (Imei, Ram_GB, Ram_Tipo, Almacenamiento_GB, Almacenamiento_Tipo, Notas, Usuario, Fecha_Registro)
        VALUES
            (@Imei, @Ram_GB, @Ram_Tipo, @Almacenamiento_GB, @Almacenamiento_Tipo, @Notas, @Usuario, DATEADD(HOUR, -4, GETDATE()));
    END

    IF EXISTS (SELECT 1 FROM @Mod)
    BEGIN
        DELETE FROM dbo.Equipo_Configuracion_Modulo WHERE Imei = @Imei;

        -- Modulos nuevos: ID automatico, en el orden recibido
        INSERT INTO dbo.Equipo_Configuracion_Modulo
            (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
        SELECT @Imei, X.Tipo_Pieza, X.Capacidad_GB, X.ID_Modelo, T.Tipo, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE())
          FROM @Mod X
         INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo
         WHERE X.ID_Modulo IS NULL
         ORDER BY X.Orden;

        -- Replicacion: mismo ID que en la base de trabajo
        IF EXISTS (SELECT 1 FROM @Mod WHERE ID_Modulo IS NOT NULL)
        BEGIN
            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo ON;

            INSERT INTO dbo.Equipo_Configuracion_Modulo
                (ID_Modulo, Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
            SELECT X.ID_Modulo, @Imei, X.Tipo_Pieza, X.Capacidad_GB, X.ID_Modelo, T.Tipo, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE())
              FROM @Mod X
             INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo
             WHERE X.ID_Modulo IS NOT NULL
             ORDER BY X.Orden;

            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo OFF;
        END

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;
    END

    COMMIT TRAN;

    SELECT C.ID_Configuracion, C.Imei, C.Ram_GB, C.Ram_Tipo, C.Almacenamiento_GB, C.Almacenamiento_Tipo,
           C.Notas, dbo.fn_Equipo_Configuracion_Texto(C.Imei) AS Configuracion,
           Mensaje = 'Configuracion guardada satisfactoriamente'
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;
END
GO

-- ------------------------------------------------------------
-- Valores por defecto de las columnas de fecha del modulo (hora local)
-- ------------------------------------------------------------
DECLARE @Columnas TABLE (Orden INT IDENTITY(1, 1) PRIMARY KEY, Tabla SYSNAME, Columna SYSNAME);
INSERT INTO @Columnas (Tabla, Columna) VALUES
    ('Equipo_Configuracion',            'Fecha_Registro'),
    ('Equipo_Configuracion_Modulo',     'Fecha_Registro'),
    ('Equipo_Configuracion_Correccion', 'Fecha'),
    ('Equipo_Cambio_Pieza',             'Fecha_Registro');

DECLARE @Orden INT = 1, @Max INT = (SELECT MAX(Orden) FROM @Columnas);
DECLARE @Tabla SYSNAME, @Columna SYSNAME, @Restriccion SYSNAME, @Definicion NVARCHAR(400), @Sql NVARCHAR(MAX);

WHILE @Orden <= @Max
BEGIN
    SELECT @Tabla = Tabla, @Columna = Columna FROM @Columnas WHERE Orden = @Orden;
    SET @Restriccion = NULL;
    SET @Definicion = NULL;

    SELECT @Restriccion = dc.name, @Definicion = dc.definition
      FROM sys.default_constraints dc
     INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
     WHERE dc.parent_object_id = OBJECT_ID('dbo.' + @Tabla) AND c.name = @Columna;

    -- Solo si el default sigue usando la hora del servidor
    IF @Restriccion IS NOT NULL AND @Definicion LIKE '%getdate%' AND @Definicion NOT LIKE '%-4%'
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' DROP CONSTRAINT ' + QUOTENAME(@Restriccion) + N';'
                 + N' ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' ADD CONSTRAINT ' + QUOTENAME('DF_' + @Tabla + '_' + @Columna + '_Local')
                 + N' DEFAULT (DATEADD(HOUR, -4, GETDATE())) FOR ' + QUOTENAME(@Columna) + N';';
        EXEC (@Sql);
    END

    SET @Orden += 1;
END
GO
