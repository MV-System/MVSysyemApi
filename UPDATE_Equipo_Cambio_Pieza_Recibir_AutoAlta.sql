-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 4 (recepcion sin error para cambios viejos)
-- Proposito: desde la Tarea 3 todo cambio nuevo deja creado el accesorio de la pieza retirada en todos los almacenes.
--            Quedan los cambios ANTERIORES cuyo accesorio retirado existe solo en otro almacen: la recepcion
--            daba error 50105 ("el accesorio retirado no esta registrado en el almacen del cambio").
--   Equipo_Cambio_Pieza_Recibir ahora, si no encuentra el accesorio en el almacen del cambio, lo CREA copiando
--   modelo, descripcion, costo, precios, minimo/maximo y estado del accesorio retirado (Cantidad = 0,
--   Codigo_Barra = 1000000000 + ID, igual que Accesorio_Insert) y luego suma 1.
--   - Nuevo parametro OPCIONAL @ID_Accesorio_Nuevo: solo lo envia el API al replicar, para que el accesorio
--     creado tenga el mismo ID en la espejo.
--   - El resultado agrega la columna Accesorio_Creado (ID creado, o NULL si no hizo falta crearlo).
--   - Si el cambio no tiene accesorio retirado se mantiene el error 50104 (no deberia ocurrir).
--   El resto (R9, costo y precios obligatorios si el accesorio no los tiene, suma al almacen del cambio) no cambia.
-- Parte de la version vigente: UPDATE_Equipo_Cambio_Pieza_Recibir_Precios.sql.
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER).
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Recibir
    @ID_Cambio         INT,
    @Usuario           VARCHAR(60),
    @Nota              VARCHAR(250) = NULL,
    @Costo_Equipo      DECIMAL(18,2) = NULL,     -- solo si el accesorio no tiene costo / precios
    @Precio_Por_Mayor  DECIMAL(18,2) = NULL,
    @Precio_Detalle    DECIMAL(18,2) = NULL,
    @ID_Accesorio_Nuevo BIGINT = NULL            -- NUEVO: solo al replicar (mismo ID del accesorio creado)
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
        DECLARE @ID_Accesorio_Destino BIGINT, @ID_Accesorio_Creado BIGINT = NULL;

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
                       O.Precio_Por_Mayor, O.Precio_Detalle, O.Costo_Equipo, O.Disponible, O.Disponible_Detalle, O.Estado, @Usuario
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
                       O.Precio_Por_Mayor, O.Precio_Detalle, O.Costo_Equipo, O.Disponible, O.Disponible_Detalle, O.Estado, @Usuario
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

        SELECT C.*, Accesorio_Creado = @ID_Accesorio_Creado, Mensaje = 'Pieza recibida satisfactoriamente'
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
