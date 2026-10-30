-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 3 (cambio de pieza por modulos)
-- Proposito: la pieza retirada puede estar registrada como accesorio SIN costo ni precios (el cambio de pieza la
--            crea inactiva y en cero cuando no existia en el catalogo). Al recibirla:
--   1) Equipo_Cambio_Pieza_Recibir: nuevos parametros OPCIONALES @Costo_Equipo, @Precio_Por_Mayor y
--      @Precio_Detalle. Si el accesorio no tiene costo y precios registrados (alguno en 0), son obligatorios
--      (costo <= precio por mayor <= precio al detalle). Al guardar se actualizan en TODOS los almacenes
--      (mismo modelo y descripcion) y el accesorio queda activo. Si el accesorio ya tiene precios, se ignoran.
--      El costo de la laptop NO se recalcula (puede estar ya vendida): ese caso lo cubre la Tarea 5.
--   2) Equipo_Cambio_Pieza_Pendientes: columna nueva Requiere_Precios (1 cuando el accesorio retirado no tiene
--      costo y precios) para que la pantalla de custodia pida los precios al recibir.
--   El resto del comportamiento no cambia (R9, suma 1 al almacen del cambio, busqueda del equivalente por almacen).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER).
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================

-- ------------------------------------------------------------
-- 1. Recibir
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Recibir
    @ID_Cambio         INT,
    @Usuario           VARCHAR(60),
    @Nota              VARCHAR(250) = NULL,
    @Costo_Equipo      DECIMAL(18,2) = NULL,     -- NUEVO: solo si el accesorio no tiene costo / precios
    @Precio_Por_Mayor  DECIMAL(18,2) = NULL,     -- NUEVO
    @Precio_Detalle    DECIMAL(18,2) = NULL      -- NUEVO
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
        DECLARE @ID_Accesorio_Destino BIGINT;

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
            THROW 50105, 'El accesorio retirado no esta registrado en el almacen del cambio. Registrelo en ese almacen antes de recibirlo.', 1;

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
                   Estado           = 'A'
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
               Fecha_Recepcion = GETDATE(),
               Nota_Recepcion = @Nota
         WHERE ID_Cambio = @ID_Cambio
           AND Estado_Custodia = 'PENDIENTE';

        IF @@ROWCOUNT = 0
            THROW 50106, 'La pieza ya fue procesada por otro usuario.', 1;

        COMMIT TRAN;

        SELECT C.*, Mensaje = 'Pieza recibida satisfactoriamente'
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
-- 2. Pendientes (agrega Requiere_Precios)
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
           Horas_Transcurridas = CAST(DATEDIFF(MINUTE, C.Fecha_Registro, GETDATE()) / 60.0 AS DECIMAL(10,1)),
           Vencido = CAST(CASE WHEN DATEDIFF(MINUTE, C.Fecha_Registro, GETDATE()) > @Horas_Max * 60 THEN 1 ELSE 0 END AS BIT),
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
