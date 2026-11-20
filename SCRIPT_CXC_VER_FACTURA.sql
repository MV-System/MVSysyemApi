-- =====================================================================
-- Cuentas por cobrar: ver el PDF de una factura pendiente
-- ObtenerFacturasPendientesPorCliente devuelve ademas ID_Sucursal (la factura se identifica
-- por Numero_Factura + ID_Sucursal). Filtros y orden sin cambios.
-- Idempotente (CREATE OR ALTER). Primero en MV_SYSTEM; al desplegar, principal y espejo.
-- =====================================================================

CREATE OR ALTER PROCEDURE [dbo].[ObtenerFacturasPendientesPorCliente]
    @Id_Cliente INT  -- ID del cliente que deseas buscar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        f.Numero_Factura,
        f.Total,
        (f.Total - f.Abono) AS MontoPendiente,
        f.fecha_registro,
        f.Tiempo_Credito,
        f.ID_Sucursal
    FROM
        facturas f
    INNER JOIN
        Clientes c ON c.ID_Cliente = f.ID_Cliente
    INNER JOIN
        Contactos co ON c.ID_Contacto = co.ID_Contacto
    WHERE
        f.ID_Tipo_Pagos = 2      -- Facturas a credito
        AND f.Estado = 'A'       -- Facturas activas
        AND f.Abono <> f.Total   -- Factura pendiente de pago
        AND f.ID_Cliente = @Id_Cliente
    ORDER BY
        f.fecha_registro DESC;
END;
GO
