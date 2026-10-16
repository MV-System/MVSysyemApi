CREATE OR ALTER PROCEDURE dbo.Ver_Utilidades_Abonos_Credito_Detalle
    @Sucursal INT = NULL,
    @Fecha_Inicio DATETIME,
    @Fecha_Final DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        hp.Id,
        hp.id_Cliente_Suplidor AS IdCliente,
        COALESCE(
            NULLIF(LTRIM(RTRIM(CONCAT(COALESCE(co.Nombres, ''), ' ', COALESCE(co.Apellidos, '')))), ''),
            CONCAT('Cliente #', hp.id_Cliente_Suplidor)
        ) AS Cliente,
        ft.Numero_Factura AS NumeroFactura,
        hp.Fecha_registro AS FechaAbono,
        TRY_CONVERT(DECIMAL(18, 2), hp.Total_Abono) AS MontoAbono,
        hp.Usuario,
        hp.Nota
    FROM dbo.Historio_Pagos hp
    INNER JOIN dbo.Facturas ft
        ON ft.Numero_Factura = TRY_CONVERT(INT, hp.Numero_factura)
       AND ft.ID_Sucursal = hp.Id_Sucursal
       AND ft.ID_Cliente = hp.id_Cliente_Suplidor
    LEFT JOIN dbo.Clientes cl
        ON cl.ID_Cliente = ft.ID_Cliente
    LEFT JOIN dbo.Contactos co
        ON co.ID_Contacto = cl.ID_Contacto
    WHERE ft.Estado = 'A'
      AND ft.ID_Tipo_Pagos = 2
      AND ft.ID_Tipo_Factura <> 3
      AND hp.Tipo_Factura = 1
      AND hp.Id_Sucursal = COALESCE(@Sucursal, hp.Id_Sucursal)
      AND hp.Total_Abono > 0
      AND hp.Fecha_registro >= CONVERT(DATE, @Fecha_Inicio)
      AND hp.Fecha_registro < DATEADD(DAY, 1, CONVERT(DATE, @Fecha_Final))
    ORDER BY hp.Fecha_registro DESC, hp.Id DESC;
END;
GO