/*
    Procedimientos de solo lectura para detalle y reimpresión de cotizaciones.
    Usan Codigo (PK de cabecera) para identificar la cotización de forma única.
    No ejecutar sin revisar en el ambiente correspondiente.
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.Cotizacion_Consulta_PorCodigo
    @Codigo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        C.Codigo,
        C.Numero_Factura AS NumeroFactura,
        C.ID_Cliente AS IdCliente,
        C.ID_Tipo_Factura AS IdTipoFactura,
        COALESCE(C.ID_Vendedor, 0) AS IdVendedor,
        COALESCE(C.ID_Tipo_Pagos, 0) AS IdTipoPago,
        C.ID_Sucursal AS IdSucursal,
        C.SubTotal,
        COALESCE(C.Descuento, 0) AS Descuento,
        COALESCE(C.Itbis, 0) AS Itbis,
        COALESCE(C.Abono, 0) AS Abono,
        C.Tiempo_Credito AS FechaLimitePago,
        C.Total,
        C.Cantidad_Articulos AS CantidadArticulos,
        COALESCE(C.Nota, '') AS Nota,
        C.Usuario,
        C.Fecha_registro AS FechaRegistro,
        COALESCE(C.NFCTipoNumero, '') AS NcfTipoNumero,
        COALESCE(C.RNC, '') AS Rnc,
        C.Literal,
        COALESCE(Co.Nombres, '') AS Nombre,
        COALESCE(Co.Apellidos, '') AS Apellido,
        COALESCE(Ven.Nombres, '') AS Vendedor,
        COALESCE(CONVERT(VARCHAR(15), Telefono.Numero_Telefono), '') AS Telefono
    FROM dbo.Cotizacion AS C
    LEFT JOIN dbo.Clientes AS CL
        ON CL.ID_Cliente = C.ID_Cliente
    LEFT JOIN dbo.Contactos AS Co
        ON Co.ID_Contacto = CL.ID_Contacto
    LEFT JOIN dbo.Vendedores AS V
        ON V.ID_Vendedor = C.ID_Vendedor
    LEFT JOIN dbo.Contactos AS Ven
        ON Ven.ID_Contacto = V.ID_Contacto
    OUTER APPLY
    (
        SELECT TOP (1) T.Numero_Telefono
        FROM dbo.Telefonos AS T
        WHERE T.ID_Contacto = CL.ID_Contacto
        ORDER BY T.Numero_Telefono
    ) AS Telefono
    WHERE C.Codigo = @Codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Detalle_Cotizacion_Consulta_PorCodigo
    @Codigo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        D.Numero_Factura AS NumeroFactura,
        D.ID_Equipo AS IdEquipo,
        COALESCE(D.Descripcion, '') AS Descripcion,
        D.ID_Tipo AS IdTipo,
        D.ID_Garantia AS IdGarantia,
        COALESCE(D.cantidad, 0) AS Cantidad,
        COALESCE(D.precio, 0) AS Precio,
        D.SubTotal,
        COALESCE(D.Descuento, 0) AS Descuento,
        COALESCE(D.Itbis, 0) AS Itbis,
        D.Total
    FROM dbo.Cotizacion AS C
    INNER JOIN dbo.Detalle_Cotizacion AS D
        ON D.Numero_Factura = C.Numero_Factura
       AND
       (
           D.LiteralF = RTRIM(C.Literal) + CONVERT(VARCHAR(10), C.Numero_Factura)
           OR
           (
               D.LiteralF IS NULL
               AND
               (
                   SELECT COUNT(*)
                   FROM dbo.Cotizacion AS CUnica
                   WHERE CUnica.Numero_Factura = C.Numero_Factura
               ) = 1
           )
       )
    WHERE C.Codigo = @Codigo
      AND D.Estado = 'A'
    ORDER BY D.Fecha_registro, D.ID_Equipo;
END;
GO
