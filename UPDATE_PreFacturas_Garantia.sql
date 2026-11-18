-- =====================================================================
-- Garantia en la PreFactura (cabecera)
-- La impresion de la factura toma la garantia de Facturas.ID_Garantia (SP_Reporte_Factura).
-- Al procesar una prefactura, Procesar_PreFactura no copiaba la garantia a la cabecera de la factura.
--   1. PreFacturas.ID_Garantia (nuevo campo)
--   2. PreFacturas_Insert recibe @ID_Garantia (opcional)
--   3. Procesar_PreFactura copia la garantia a Facturas.ID_Garantia
--      (si la prefactura no la tiene, la toma de sus lineas; si tampoco, 7 = No Aplica)
-- Idempotente. Ejecutar en cada base (principal y espejo). API y front se publican junto con este script.
-- =====================================================================

IF COL_LENGTH('dbo.PreFacturas', 'ID_Garantia') IS NULL
    ALTER TABLE dbo.PreFacturas ADD ID_Garantia INT NULL;
GO

CREATE OR ALTER PROCEDURE [dbo].[PreFacturas_Insert]
@Numero_PreFactura INT OUTPUT,
@ID_Cliente INT,
@ID_Tipo_Factura INT,
@ID_Vendedor INT = NULL,
@ID_Tipo_Pagos INT = NULL,
@ID_Sucursal INT,
@SubTotal MONEY = NULL,
@Descuento MONEY = NULL,
@Itbis MONEY = NULL,
@NFCTipoNumero VARCHAR(50) = NULL,
@NFCFISCAL VARCHAR(50) = NULL,
@NCF_NUMERO VARCHAR(50) = NULL,
@RNC VARCHAR(11) = NULL,
@Abono MONEY = NULL,
@Tiempo_Credito DATETIME = NULL,
@Total MONEY = NULL,
@Cantidad_Articulos INT,
@Nota VARCHAR(500) = NULL,
@Estado CHAR(1) = 'P',
@Usuario VARCHAR(50),
@Fecha_Registro DATETIME = NULL,
@Fecha_Procesamiento DATETIME = NULL,
@Usuario_Procesamiento VARCHAR(50) = NULL,
@Literal CHAR(1) = 'X',
@Cotizacion_Numero INT = 0,
@ID_Garantia INT = NULL
AS
BEGIN
    IF @Numero_PreFactura IS NULL
    BEGIN
        SET @Numero_PreFactura = ISNULL((SELECT MAX(Numero_PreFactura) FROM PreFacturas), 0) + 1
    END

    IF @Fecha_Registro IS NULL
    BEGIN
        SET @Fecha_Registro = dateadd (hour,-4,getdate())
    END

	 set @Literal = (select literal from Almacen_Cata where ID_Almacen = (select ID_Almacen from sucursales_cata where ID_Sucursal = @ID_Sucursal))

  Set @Numero_PreFactura = (Select IsNull(Max(Numero_PreFactura),1)
                              From PreFacturas
                              Where Literal = @Literal)

  Set @Numero_PreFactura = @Numero_PreFactura+1 -- Incrementa la Factura

    INSERT INTO PreFacturas (
        Numero_PreFactura, ID_Cliente, ID_Tipo_Factura, ID_Vendedor, ID_Tipo_Pagos, ID_Sucursal,
        SubTotal, Descuento, Itbis, NFCTipoNumero, NFCFISCAL, NCF_NUMERO, RNC, Abono, Tiempo_Credito, Total,
        Cantidad_Articulos, Nota, Estado, Usuario, Fecha_Registro, Fecha_Procesamiento, Usuario_Procesamiento, Literal, Cotizacion_Numero,
        ID_Garantia
    )
    VALUES (
        @Numero_PreFactura, @ID_Cliente, @ID_Tipo_Factura, @ID_Vendedor, @ID_Tipo_Pagos, @ID_Sucursal,
        @SubTotal, @Descuento, @Itbis, @NFCTipoNumero, @NFCFISCAL, @NCF_NUMERO, @RNC, @Abono, @Tiempo_Credito, @Total,
        @Cantidad_Articulos, @Nota, @Estado, @Usuario, @Fecha_Registro, @Fecha_Procesamiento, @Usuario_Procesamiento, @Literal, @Cotizacion_Numero,
        @ID_Garantia
    )

    -- Retornar el registro recién insertado
    SELECT TOP 1 *
    FROM PreFacturas
    WHERE Numero_PreFactura = @Numero_PreFactura
    ORDER BY Fecha_Registro DESC
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Procesar_PreFactura]
    @Numero_PreFactura INT,
    @Usuario_Procesamiento VARCHAR(50),
    @Nuevo_Numero_Factura INT OUTPUT,
    @Mensaje NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RowsAffected INT = 0;
    DECLARE @TipoPago INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TipoPago = ID_Tipo_Pagos
        FROM PreFacturas
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        IF @TipoPago IS NULL
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'PreFactura no encontrada o no esta en estado Pendiente (P)';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        SELECT @Nuevo_Numero_Factura =
            ISNULL(MAX(Numero_Factura), 0) + 1
        FROM Facturas;

        INSERT INTO Facturas
        (
            Numero_Factura, ID_Cliente, ID_Tipo_Factura, ID_Vendedor,
            ID_Tipo_Pagos, ID_Sucursal, SubTotal, Descuento, Itbis,
            NFCTipoNumero, RNC, Abono, Tiempo_Credito, Total,
            Cantidad_Articulos, Nota, Estado, Usuario, Fecha_Registro,
            Literal, Cotizacion_Numero, ID_Garantia
        )
        SELECT
            @Nuevo_Numero_Factura,
            ID_Cliente,
            ID_Tipo_Factura,
            ID_Vendedor,
            ID_Tipo_Pagos,
            ID_Sucursal,
            SubTotal,
            Descuento,
            Itbis,
            NFCTipoNumero,
            RNC,
            Abono,
            Tiempo_Credito,
            Total,
            Cantidad_Articulos,
            Nota,
            'A',
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE()),
            Literal,
            Cotizacion_Numero,
            -- Garantia: la de la prefactura; si no la tiene (prefacturas anteriores), la de sus lineas; si no, 7 = No Aplica
            COALESCE(ID_Garantia,
                     (SELECT MAX(d.ID_Garantia) FROM PreDetalle_Facturas d
                       WHERE d.Numero_PreFactura = @Numero_PreFactura AND d.Estado = 'P'),
                     7)
        FROM PreFacturas
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se pudo insertar la cabecera de la factura';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        INSERT INTO Detalle_Facturas
        (
            Numero_Factura, ID_Equipo, Descripcion, ID_Tipo,
            ID_Garantia, cantidad, precio, SubTotal, Descuento,
            LiteralF, Itbis, Total, Estado, Usuario,
            Fecha_registro, Costo_Equipo
        )
        SELECT
            @Nuevo_Numero_Factura,
            pf.ID_Equipo,
            pf.Descripcion,
            pf.ID_Tipo,
            pf.ID_Garantia,
            pf.cantidad,
            pf.precio,
            pf.SubTotal,
            pf.Descuento,
            CONCAT(p.Literal, @Nuevo_Numero_Factura),
            pf.Itbis,
            pf.Total,
            'A',
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE()),
            pf.Costo_Equipo
        FROM PreDetalle_Facturas pf
        INNER JOIN PreFacturas p
            ON pf.Numero_PreFactura = p.Numero_PreFactura
        WHERE p.Numero_PreFactura = @Numero_PreFactura
          AND pf.Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se encontraron detalles para procesar';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        -- Copiar pagos mixtos P -> F sin modificar la prefactura
        IF @TipoPago = 6
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM DetallesPagos_Factura
                WHERE Numero_Factura = @Numero_PreFactura
                  AND Tipo_Documento = 'P'
                  AND Estado = 'A'
            )
            BEGIN
                SET @Nuevo_Numero_Factura = 0;
                SET @Mensaje = 'Error: La prefactura Mixta no tiene detalles de pago';
                ROLLBACK TRANSACTION;
                SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
                RETURN;
            END;

            EXEC dbo.CopiarDetallesPagoPrefacturaAFactura
                @Numero_PreFactura = @Numero_PreFactura,
                @Numero_Factura = @Nuevo_Numero_Factura;
        END;

        INSERT INTO Equipos_Historico
        (
            Imei, ID_Modelo, ID_Tecnologia, ID_Garantia,
            ID_Condicion, ID_Estado_Bloqueo, ID_Almacen,
            ID_Suplidor, Numero_de_Factura, Precio_Por_Mayor,
            Precio_Detalle, Costo_Equipo, Desbloqueado,
            Disponible, Nota_Adicional, Nota_Historico,
            Usuario, Fecha_registro, Numero_Invetario,
            Numero_Registro, Usuario_Historico,
            Fecha_registro_Historico
        )
        SELECT
            Imei,
            ID_Modelo,
            ID_Tecnologia,
            ID_Garantia,
            ID_Condicion,
            ID_Estado_Bloqueo,
            ID_Almacen,
            ID_Suplidor,
            Numero_de_Factura,
            Precio_Por_Mayor,
            Precio_Detalle,
            Costo_Equipo,
            Desbloqueado,
            Disponible,
            Nota_Adicional,
            'Vendido en factura #' + CONVERT(VARCHAR(50), @Nuevo_Numero_Factura),
            Usuario,
            Fecha_registro,
            Numero_Invetario,
            Numero_Registro,
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE())
        FROM Equipos WITH (NOLOCK)
        WHERE Imei IN
        (
            SELECT DISTINCT ID_Equipo
            FROM PreDetalle_Facturas
            WHERE Numero_PreFactura = @Numero_PreFactura
              AND ID_Tipo = 1
              AND Estado = 'P'
        );

        UPDATE Equipos
        SET Disponible = 'N',
            Nota_Adicional = 'Vendido en factura #' +
                CONVERT(VARCHAR(50), @Nuevo_Numero_Factura),
            Numero_de_Factura = @Nuevo_Numero_Factura
        WHERE Imei IN
        (
            SELECT DISTINCT ID_Equipo
            FROM PreDetalle_Facturas
            WHERE Numero_PreFactura = @Numero_PreFactura
              AND ID_Tipo = 1
              AND Estado = 'P'
        );

        UPDATE a
        SET a.Cantidad = a.Cantidad - pf.cantidad
        FROM Accesorios a
        INNER JOIN PreDetalle_Facturas pf
            ON a.ID_Accesorio = pf.ID_Equipo
        WHERE pf.Numero_PreFactura = @Numero_PreFactura
          AND pf.ID_Tipo = 2
          AND pf.Estado = 'P';

        UPDATE Reparaciones
        SET ID_Estado_Taller = 3,
            SubTotal = pf.SubTotal,
            Descuento = pf.Descuento,
            Itbis = pf.Itbis,
            Numero_Factura = @Nuevo_Numero_Factura
        FROM PreDetalle_Facturas pf
        WHERE CONVERT(INT, pf.ID_Equipo) = Reparaciones.ID_Reparacion
          AND pf.Numero_PreFactura = @Numero_PreFactura
          AND pf.ID_Tipo IN (3, 4)
          AND pf.Estado = 'P';

        UPDATE PreFacturas
        SET Estado = 'C',
            Fecha_Procesamiento = DATEADD(HOUR, -4, GETDATE()),
            Usuario_Procesamiento = @Usuario_Procesamiento
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se pudo actualizar el estado de la PreFactura';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        -- El detalle de la prefactura tambien pasa a 'C'
        UPDATE PreDetalle_Facturas
           SET Estado = 'C'
         WHERE Numero_PreFactura = @Numero_PreFactura
           AND Estado = 'P';

        COMMIT TRANSACTION;

        SET @Mensaje =
            'Factura creada satisfactoriamente. Factura #' +
            CONVERT(VARCHAR(50), @Nuevo_Numero_Factura);
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Nuevo_Numero_Factura = 0;
        SET @Mensaje = 'ERROR: ' + ERROR_MESSAGE();
    END CATCH;

    SELECT
        @Nuevo_Numero_Factura AS Cod_Factura,
        @Mensaje AS Mensaje;
END;
GO
