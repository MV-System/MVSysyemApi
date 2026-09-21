SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetDetallesPagoFactura]
    @NumeroFactura INT,
    @Tipo_Documento CHAR(1) = 'F'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Tipo_Documento NOT IN ('F', 'P')
        THROW 50004, 'Tipo de documento invalido. Use F o P.', 1;

    SELECT
        dpf.ID_Tipo_Pago,
        tp.Descripcion_Tipo_Pagos,
        dpf.Monto_Pago,
        dpf.Numero_Referencia,
        dpf.Observacion,
        dpf.Tipo_Documento,
        dpf.Fecha_Registro
    FROM DetallesPagos_Factura dpf
    INNER JOIN Tipos_Pagos_Cata tp
        ON dpf.ID_Tipo_Pago = tp.ID_Tipo_Pagos
    WHERE dpf.Numero_Factura = @NumeroFactura
      AND dpf.Tipo_Documento = @Tipo_Documento
      AND dpf.Estado = 'A'
    ORDER BY dpf.Fecha_Registro;
END;
GO