-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 6 (anular un cambio de pieza)
-- Proposito: Equipo_Cambio_Pieza_Registrar guarda en el cambio los precios de venta que tenia la laptop ANTES
--   (Precio_Detalle_Anterior, Precio_Por_Mayor_Anterior), para que Equipo_Cambio_Pieza_Anular pueda restablecerlos
--   cuando el cambio los hubiera modificado. El resto del SP no cambia.
-- Parte de la version vigente: UPDATE_Equipo_Cambio_Pieza_Registrar_Usado.sql.
-- Requiere: CREATE_Equipo_Cambio_Pieza_Anular.sql ANTES (agrega las columnas).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER).
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================
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
                          AND DATEDIFF(MINUTE, P.Fecha_Registro, GETDATE()) > @Horas_Max_Custodia * 60)
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
            VALUES (@Imei, @Usuario, GETDATE());
        END

        IF @ID_Modulo_Retirado IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET Estado = 'R' WHERE ID_Modulo = @ID_Modulo_Retirado;

        DECLARE @ID_Modulo_Nuevo INT;

        IF @ID_Modulo_Instalado IS NULL
        BEGIN
            INSERT INTO dbo.Equipo_Configuracion_Modulo
                (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, ID_Accesorio, Estado, Usuario, Fecha_Registro)
            VALUES
                (@Imei, @Tipo_Pieza, @Cap_Inst, @ID_Modelo_Inst, @Tipo_Inst, @ID_Accesorio_Instalado, 'A', @Usuario, GETDATE());
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
                (@ID_Modulo_Instalado, @Imei, @Tipo_Pieza, @Cap_Inst, @ID_Modelo_Inst, @Tipo_Inst, @ID_Accesorio_Instalado, 'A', @Usuario, GETDATE());
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
             @ID_PreFactura, @ID_Reparacion, @Tecnico, @Estado_Custodia, @Usuario, GETDATE(),
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
             @ID_PreFactura, @ID_Reparacion, @Tecnico, @Estado_Custodia, @Usuario, GETDATE(),
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
