SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[CopiarDetallesPagoPrefacturaAFactura]
    @Numero_PreFactura INT,
    @Numero_Factura INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF NOT EXISTS (
            SELECT 1
            FROM PreFacturas
            WHERE Numero_PreFactura = @Numero_PreFactura
        )
            THROW 50005, 'La prefactura no existe.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM Facturas
            WHERE Numero_Factura = @Numero_Factura
        )
            THROW 50006, 'La factura no existe.', 1;

        IF EXISTS (
            SELECT 1
            FROM DetallesPagos_Factura
            WHERE Numero_Factura = @Numero_Factura
              AND Tipo_Documento = 'F'
        )
            THROW 50007, 'La factura ya tiene detalles de pago.', 1;

        INSERT INTO DetallesPagos_Factura
        (
            Numero_Factura,
            ID_Tipo_Pago,
            Monto_Pago,
            Numero_Referencia,
            Observacion,
            Tipo_Documento,
            Estado,
            Fecha_Registro
        )
        SELECT
            @Numero_Factura,
            ID_Tipo_Pago,
            Monto_Pago,
            Numero_Referencia,
            Observacion,
            'F',
            Estado,
            GETDATE()
        FROM DetallesPagos_Factura
        WHERE Numero_Factura = @Numero_PreFactura
          AND Tipo_Documento = 'P'
          AND Estado = 'A';
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO