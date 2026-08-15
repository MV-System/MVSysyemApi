-- Stored Procedure: sp_ObtenerHistorialAbonosCliente
-- Propósito: Obtener historial de abonos/pagos de un cliente específico
-- Solo incluye abonos de facturas activas (Estado = 'A' en tabla Facturas)
-- Excluye abonos de facturas anuladas (Estado = 'X' en tabla Facturas)
-- Se hace JOIN con tabla Facturas porque cuando se anula una factura,
-- el estado en Facturas se actualiza pero no en HISTORIO_PAGOS
-- Autor: MVSystem
-- Fecha: 2026-08-15
-- Nota: Se filtran AMBAS tablas por IdCliente para evitar traer datos de otros clientes

ALTER PROCEDURE [dbo].[sp_ObtenerHistorialAbonosCliente]
    @IdCliente INT,
    @FechaInicio DATETIME = NULL,
    @FechaFinal DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Validar que el IdCliente sea válido
    IF @IdCliente <= 0
    BEGIN
        RETURN;
    END

    SELECT
        hp.Id,
        hp.id_Cliente_Suplidor AS IdCliente,
        hp.Numero_factura AS NumeroFactura,
        hp.Total_Factura AS TotalFactura,
        hp.Total_Abono AS TotalAbono,
        hp.Pendiente AS MontoPendiente,
        hp.Tipo_Factura AS IdTipoFactura,
        hp.Fecha_Limite_de_Pago AS FechaVencimiento,
        f.Estado,  -- Tomamos el estado de la tabla Facturas
        hp.Usuario,
        hp.Fecha_registro AS FechaRegistro,
        hp.ID_Tipo_Pagos AS IdTipoPago,
        ISNULL(tp.Descripcion_Tipo_Pagos, '') AS TipoPago,
        hp.Nota,
        hp.Id_Lote_Abono AS IdLoteAbono
    FROM
        HISTORIO_PAGOS hp
    INNER JOIN
        Facturas f ON hp.Numero_factura = f.Numero_factura
            AND hp.id_Cliente_Suplidor = f.ID_Cliente
    LEFT JOIN
        Tipos_Pagos_Cata tp ON hp.ID_Tipo_Pagos = tp.ID_Tipo_Pagos
    WHERE
        -- Filtro principal: Solo del cliente específico en AMBAS tablas
        hp.id_Cliente_Suplidor = @IdCliente
        AND f.ID_Cliente = @IdCliente
        AND f.ID_Tipo_Pagos = 2
        AND hp.Tipo_Factura = 1
        -- Solo facturas activas
        AND f.Estado = 'A'
        -- Filtros opcionales de fecha
        AND (@FechaInicio IS NULL OR hp.Fecha_registro >= @FechaInicio)
        AND (@FechaFinal IS NULL OR hp.Fecha_registro <= @FechaFinal)
    ORDER BY
        hp.Fecha_registro DESC
END


