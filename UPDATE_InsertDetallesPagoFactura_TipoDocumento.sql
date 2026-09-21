SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[InsertDetallesPagoFactura]
    @Numero_Factura INT,
    @ID_Tipo_Pago INT,
    @Monto_Pago DECIMAL(18,2),
    @Numero_Referencia NVARCHAR(100) = NULL,
    @Observacion NVARCHAR(500) = NULL,
    @Tipo_Documento CHAR(1) = 'F'
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Tipo_Documento NOT IN ('F', 'P')
            THROW 50001, 'Tipo de documento invalido. Use F o P.', 1;

        IF @Tipo_Documento = 'F'
           AND NOT EXISTS (
                SELECT 1
                FROM Facturas
                WHERE Numero_Factura = @Numero_Factura
           )
            THROW 50002, 'La factura no existe.', 1;

        IF @Tipo_Documento = 'P'
           AND NOT EXISTS (
                SELECT 1
                FROM PreFacturas
                WHERE Numero_PreFactura = @Numero_Factura
           )
            THROW 50003, 'La prefactura no existe.', 1;

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
        VALUES
        (
            @Numero_Factura,
            @ID_Tipo_Pago,
            @Monto_Pago,
            @Numero_Referencia,
            @Observacion,
            @Tipo_Documento,
            'A',
            GETDATE()
        );
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO