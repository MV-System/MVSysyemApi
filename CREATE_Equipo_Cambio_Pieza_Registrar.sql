-- ============================================================
-- Feature: Módulo de Laptops - Fase 3, Tarea 9
-- Propósito: SP Equipo_Cambio_Pieza_Registrar. Registra el cambio de una
--            pieza (RAM / almacenamiento) en una laptop disponible:
--            - Descuenta 1 del accesorio instalado en el almacén (R9/stock).
--            - Costo nuevo = Costo actual - Costo retirado + Costo instalado (R4);
--              si la pieza retirada se va con el cliente no se resta (R10).
--            - Costos de las piezas = Costo_Equipo del accesorio en catálogo (R5).
--            - Actualiza Equipo_Configuracion con @Nueva_Capacidad_GB / @Nuevo_Tipo.
--            - Inserta el historial con Estado_Custodia:
--                 PENDIENTE (retirada en custodia), ENTREGADA_CLIENTE o NO_APLICA (R8, R10, R11).
--            Todo en una transacción (R13). La pieza retirada NO suma stock
--            hasta que se confirme la recepción (Tarea 12).
--            Precios: el costo nuevo nunca puede quedar por encima del precio al detalle
--            ni del precio por mayor. Si lo supera, el llamador debe enviar
--            @Nuevo_Precio_Detalle / @Nuevo_Precio_Por_Mayor y se actualizan en la misma
--            transacción. Antes de cambiar costo/precios se guarda una foto de la fila
--            anterior en Equipos_Historico (igual que Equipo_Actualiza_Estado_y_precio).
--            Tarea 17: rechaza el cambio si la laptop esta en una prefactura pendiente.
-- Requiere: CREATE_Laptops_Fase1_Grupos.sql, CREATE_Equipo_Configuracion.sql,
--           CREATE_Equipo_Cambio_Pieza.sql
-- Nota: Accesorios tiene una fila por (accesorio, almacén). El accesorio instalado
--       debe ser la fila del @ID_Almacen del cambio.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Registrar
    @Imei                   VARCHAR(30),
    @Tipo_Pieza             VARCHAR(20),          -- 'RAM' | 'ALMACENAMIENTO'
    @ID_Accesorio_Instalado BIGINT,
    @Nueva_Capacidad_GB     INT,
    @Nuevo_Tipo             VARCHAR(30),          -- ej. 'DDR4' | 'SSD SATA' | 'SSD NVMe' | 'HDD'
    @ID_Accesorio_Retirado  BIGINT = NULL,
    @Destino_Retirada       VARCHAR(10) = NULL,   -- 'CUSTODIA' | 'CLIENTE' (se ignora si no hay retirada)
    @Tecnico                VARCHAR(60),
    @ID_Almacen             INT,
    @ID_PreFactura          INT = NULL,
    @ID_Reparacion          INT = NULL,
    @Nuevo_Precio_Detalle   DECIMAL(18,2) = NULL,
    @Nuevo_Precio_Por_Mayor DECIMAL(18,2) = NULL,
    @Usuario                VARCHAR(60),
    @Horas_Max_Custodia     INT = 24,             -- Tarea 16: viene de appsettings Laptops:HorasMaxCustodia
    @Bloquear_Vencidos      BIT = 0               -- Tarea 16: Laptops:BloquearTecnicoConPendientesVencidos
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- --------------------------------------------------------
        -- Validaciones de parámetros
        -- --------------------------------------------------------
        IF @Tipo_Pieza NOT IN ('RAM', 'ALMACENAMIENTO')
            THROW 50001, 'Tipo de pieza invalido. Use RAM o ALMACENAMIENTO.', 1;

        IF @Nueva_Capacidad_GB IS NULL OR @Nueva_Capacidad_GB <= 0
            THROW 50002, 'La capacidad nueva (GB) es requerida y debe ser mayor a 0.', 1;

        IF @Nuevo_Tipo IS NULL OR LTRIM(RTRIM(@Nuevo_Tipo)) = ''
            THROW 50003, 'El tipo de la pieza instalada es requerido.', 1;

        IF @Tecnico IS NULL OR LTRIM(RTRIM(@Tecnico)) = ''
            THROW 50004, 'El tecnico es requerido.', 1;

        IF @ID_Accesorio_Instalado IS NULL
            THROW 50005, 'Debe indicar el accesorio instalado.', 1;

        IF @ID_Accesorio_Retirado IS NOT NULL AND ISNULL(@Destino_Retirada, '') NOT IN ('CUSTODIA', 'CLIENTE')
            THROW 50006, 'Destino de la pieza retirada invalido. Use CUSTODIA o CLIENTE.', 1;

        -- Tarea 16: un tecnico con piezas en custodia vencidas no puede registrar mas cambios
        IF @Bloquear_Vencidos = 1
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

        IF @PreFactura_Pendiente IS NOT NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La laptop esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de cambiar piezas.');
            THROW 50014, @MsgPre, 1;
        END

        -- --------------------------------------------------------
        -- 2. Validar y descontar el accesorio instalado (R3: debe ser un Componente)
        -- --------------------------------------------------------
        DECLARE @Costo_Instalado DECIMAL(18,2);

        SELECT @Costo_Instalado = CAST(A.Costo_Equipo AS DECIMAL(18,2))
        FROM dbo.Accesorios A
        INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
        WHERE A.ID_Accesorio = @ID_Accesorio_Instalado
          AND A.ID_Almacen = @ID_Almacen
          AND A.Estado = 'A'
          AND M.ID_Grupo = @ID_Grupo_Componentes;

        IF @Costo_Instalado IS NULL
            THROW 50020, 'El accesorio instalado no existe en este almacen, esta inactivo o no es un Componente.', 1;

        UPDATE dbo.Accesorios
           SET Cantidad = Cantidad - 1
         WHERE ID_Accesorio = @ID_Accesorio_Instalado
           AND ID_Almacen = @ID_Almacen
           AND ISNULL(Cantidad, 0) > 0;

        IF @@ROWCOUNT = 0
            THROW 50021, 'No hay existencia del accesorio instalado en este almacen.', 1;

        -- --------------------------------------------------------
        -- 3. Pieza retirada (opcional): costo de catalogo, R5
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
        -- 4. Costo nuevo (R4 / R10): si la pieza se va con el cliente no se resta
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
        -- 5. Configuracion de la laptop (se crea si aun no tenia)
        -- --------------------------------------------------------
        DECLARE @Config_Anterior VARCHAR(100) = dbo.fn_Equipo_Configuracion_Texto(@Imei);

        IF NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion WHERE Imei = @Imei)
        BEGIN
            INSERT INTO dbo.Equipo_Configuracion (Imei, Usuario, Fecha_Registro)
            VALUES (@Imei, @Usuario, GETDATE());
        END

        UPDATE dbo.Equipo_Configuracion
           SET Ram_GB              = CASE WHEN @Tipo_Pieza = 'RAM' THEN @Nueva_Capacidad_GB ELSE Ram_GB END,
               Ram_Tipo            = CASE WHEN @Tipo_Pieza = 'RAM' THEN @Nuevo_Tipo ELSE Ram_Tipo END,
               Almacenamiento_GB   = CASE WHEN @Tipo_Pieza = 'ALMACENAMIENTO' THEN @Nueva_Capacidad_GB ELSE Almacenamiento_GB END,
               Almacenamiento_Tipo = CASE WHEN @Tipo_Pieza = 'ALMACENAMIENTO' THEN @Nuevo_Tipo ELSE Almacenamiento_Tipo END,
               Usuario_Modifica    = @Usuario,
               Fecha_Modifica      = GETDATE()
         WHERE Imei = @Imei;

        DECLARE @Config_Nueva VARCHAR(100) = dbo.fn_Equipo_Configuracion_Texto(@Imei);

        -- --------------------------------------------------------
        -- 6. Historial + custodia
        -- --------------------------------------------------------
        INSERT INTO dbo.Equipo_Cambio_Pieza
            (Imei, Tipo_Pieza, ID_Accesorio_Instalado, Costo_Instalado, ID_Accesorio_Retirado, Costo_Retirado,
             Config_Anterior, Config_Nueva, Costo_Equipo_Anterior, Costo_Equipo_Nuevo, ID_Almacen,
             ID_PreFactura, ID_Reparacion, Tecnico, Estado_Custodia, Usuario_Registro, Fecha_Registro)
        VALUES
            (@Imei, @Tipo_Pieza, @ID_Accesorio_Instalado, @Costo_Instalado, @ID_Accesorio_Retirado, @Costo_Retirado,
             @Config_Anterior, @Config_Nueva, @Costo_Anterior, @Costo_Nuevo, @ID_Almacen,
             @ID_PreFactura, @ID_Reparacion, @Tecnico, @Estado_Custodia, @Usuario, GETDATE());

        DECLARE @ID_Cambio INT = SCOPE_IDENTITY();

        COMMIT TRAN;

        -- --------------------------------------------------------
        -- 7. Devolver el registro insertado
        -- --------------------------------------------------------
        SELECT C.*, Mensaje = 'Cambio de pieza registrado satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO
