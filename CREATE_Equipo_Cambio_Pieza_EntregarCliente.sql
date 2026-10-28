-- ============================================================
-- Feature: Módulo de Laptops - Fase 4, Tarea 13
-- Propósito: SP Equipo_Cambio_Pieza_EntregarCliente. La pieza retirada que estaba
--            en custodia se la lleva el cliente junto con la laptop:
--            - Solo desde Estado_Custodia = 'PENDIENTE' y si la laptop NO está vendida.
--            - Vuelve a sumar Costo_Retirado al Costo_Equipo de la laptop (R10),
--              porque la pieza se va con ella (anula el descuento de R4).
--            - No suma stock al inventario.
--            - Marca el cambio como 'ENTREGADA_CLIENTE' (usuario, fecha y nota en los
--              campos de recepción).
--            - Si el costo resultante supera el precio de venta, exige precios nuevos
--              (@Nuevo_Precio_Detalle / @Nuevo_Precio_Por_Mayor), igual que el cambio de pieza.
--            Tarea 17: se rechaza si la laptop esta en una prefactura pendiente.
--            Antes de modificar costo/precios guarda una foto en Equipos_Historico.
--            Transaccional (R13).
-- Requiere: CREATE_Equipo_Cambio_Pieza.sql
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
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
               Fecha_Recepcion = GETDATE(),
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
