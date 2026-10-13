/*
    Consulta paginada de cotizaciones.
    No ejecutar sin revisar en el ambiente correspondiente.

    Criterio global (cliente, teléfono, tipo de factura o número) ignora
    sucursal y rango de fechas, para recuperar cotizaciones históricas.
    Sin criterio, se aplican sucursal y fechas.
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.Cotizacion_Consulta
    @PageIndex INT = 1,
    @PageSize INT = 10,
    @Criterio VARCHAR(100) = NULL,
    @Sucursal INT = 0,
    @Vendedor INT = 0,
    @IdTipoPago INT = 0,
    @Fecha_Inicio DATE = NULL,
    @Fecha_Final DATE = NULL,
    @Estado CHAR(1) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageIndex < 1 SET @PageIndex = 1;
    IF @PageSize < 1 SET @PageSize = 10;
    IF @Fecha_Inicio IS NULL SET @Fecha_Inicio = CONVERT(DATE, GETDATE());
    IF @Fecha_Final IS NULL SET @Fecha_Final = CONVERT(DATE, GETDATE());

    SET @Criterio = NULLIF(LTRIM(RTRIM(@Criterio)), '');

    DECLARE @BusquedaGlobal BIT = CASE WHEN @Criterio IS NULL THEN 0 ELSE 1 END;

    ;WITH CotizacionesFiltradas AS
    (
        SELECT
            C.Codigo,
            C.Numero_Factura AS NumeroFactura,
            C.ID_Sucursal AS IdSucursal,
            C.Literal,
            C.ID_Cliente AS ClienteId,
            COALESCE(Co.Nombres, '') AS Nombres,
            COALESCE(Co.Apellidos, '') AS Apellidos,
            Telefono.Telefono,
            COALESCE(TF.Descripcion_Factura_Tipo, '') AS TipoFactura,
            COALESCE(CV.Nombres, '') AS Vendedor,
            COALESCE(TP.Descripcion_Tipo_Pagos, '') AS TipoPago,
            C.SubTotal,
            COALESCE(C.Descuento, 0) AS Descuento,
            COALESCE(C.Itbis, 0) AS Itbis,
            COALESCE(C.Abono, 0) AS Abono,
            C.Total,
            COALESCE(C.Nota, '') AS Nota,
            C.Fecha_registro AS FechaFacturacion,
            C.Estado,
            C.facturado AS Facturado,
            ROW_NUMBER() OVER (ORDER BY C.Fecha_registro DESC, C.Codigo DESC) AS Linea,
            COUNT(*) OVER() AS TotalRecord,
            SUM(C.Total) OVER() AS TotalCotizado
        FROM dbo.Cotizacion AS C
        INNER JOIN dbo.Clientes AS CL
            ON CL.ID_Cliente = C.ID_Cliente
        INNER JOIN dbo.Contactos AS Co
            ON Co.ID_Contacto = CL.ID_Contacto
        LEFT JOIN dbo.Tipos_Facturas_Cata AS TF
            ON TF.ID_Tipo_Factura = C.ID_Tipo_Factura
        LEFT JOIN dbo.Tipos_Pagos_Cata AS TP
            ON TP.ID_Tipo_Pagos = C.ID_Tipo_Pagos
        LEFT JOIN dbo.Vendedores AS V
            ON V.ID_Vendedor = C.ID_Vendedor
        LEFT JOIN dbo.Contactos AS CV
            ON CV.ID_Contacto = V.ID_Contacto
        OUTER APPLY
        (
            SELECT TOP (1)
                T.Numero_Telefono AS Telefono
            FROM dbo.Telefonos AS T
            WHERE T.ID_Contacto = CL.ID_Contacto
            ORDER BY T.Numero_Telefono
        ) AS Telefono
        WHERE
            (
                @Criterio IS NULL
                OR (COALESCE(Co.Nombres, '') + ' ' + COALESCE(Co.Apellidos, '') LIKE '%' + @Criterio + '%')
                OR CONVERT(VARCHAR(20), C.Numero_Factura) LIKE '%' + @Criterio + '%'
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.Telefonos AS TBusca
                    WHERE TBusca.ID_Contacto = CL.ID_Contacto
                      AND TBusca.Numero_Telefono LIKE '%' + @Criterio + '%'
                )
                OR TF.Descripcion_Factura_Tipo LIKE '%' + @Criterio + '%'
            )
            AND (@BusquedaGlobal = 1 OR @Sucursal = 0 OR C.ID_Sucursal = @Sucursal)
            AND (@BusquedaGlobal = 1 OR
                (C.Fecha_registro >= @Fecha_Inicio
                 AND C.Fecha_registro < DATEADD(DAY, 1, @Fecha_Final)))
            AND (@Vendedor = 0 OR C.ID_Vendedor = @Vendedor)
            AND (@IdTipoPago = 0 OR C.ID_Tipo_Pagos = @IdTipoPago)
            AND (@Estado IS NULL OR C.Estado = @Estado)
    )
    SELECT
        Codigo,
        NumeroFactura,
        IdSucursal,
        Literal,
        ClienteId,
        Nombres,
        Apellidos,
        Telefono,
        TipoFactura,
        Vendedor,
        TipoPago,
        SubTotal,
        Descuento,
        Itbis,
        Abono,
        Total,
        Nota,
        FechaFacturacion,
        Estado,
        Facturado,
        Linea,
        TotalRecord,
        TotalCotizado,
        CASE
            WHEN TotalRecord < (@PageIndex * @PageSize) THEN TotalRecord
            ELSE (@PageIndex * @PageSize)
        END AS Ultima_Linea
    FROM CotizacionesFiltradas
    WHERE Linea BETWEEN ((@PageIndex - 1) * @PageSize) + 1
                    AND (@PageIndex * @PageSize)
    ORDER BY Linea;
END;
GO
