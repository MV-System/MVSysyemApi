-- Modificación del SP para agregar totales agregados de ITBIS y SubTotal
-- Fecha: 2026-04-23

ALTER   PROCEDURE [dbo].[Proc_Facturas_Consulta]
    @PageIndex INT = 1,
    @PageSize INT = 10,
    @Criterio VARCHAR(100) = NULL,
    @Almacen INT = 0,
    @Vendedor INT = 0,
    @Comprobante INT = 1,
    @Fecha_Inicio DATETIME = NULL,
    @Fecha_Final DATETIME = NULL,
    @EstadoFactura CHAR(1) = NULL,
    @IdTipoPago INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @Almacen = 4 SET @Almacen = 0;
    IF @PageIndex < 1 SET @PageIndex = 1;
    IF @Fecha_Inicio IS NULL OR @Fecha_Inicio = '' SET @Fecha_Inicio = CAST(GETDATE() AS DATE);
    IF @Fecha_Final IS NULL OR @Fecha_Final = '' SET @Fecha_Final = CAST(GETDATE() AS DATE);

    DECLARE @BusquedaGlobal BIT = CASE
        WHEN NULLIF(LTRIM(RTRIM(@Criterio)), '') IS NULL THEN 0
        ELSE 1
    END;
    SET @Criterio = NULLIF(LTRIM(RTRIM(@Criterio)), '');

    -- Calcular totales agregados
    DECLARE @TotalFacturado MONEY;
    DECLARE @TotalItbis MONEY;
    DECLARE @TotalSinItbis MONEY;

    SELECT @TotalFacturado = SUM(F.Total),
           @TotalItbis = SUM(F.Itbis),
           @TotalSinItbis = SUM(F.SubTotal)
    FROM Facturas F
    INNER JOIN Clientes C ON F.ID_Cliente = C.ID_Cliente
    INNER JOIN Contactos Co ON C.ID_Contacto = Co.ID_Contacto
    INNER JOIN Telefonos te ON te.ID_Contacto = C.ID_Contacto
    INNER JOIN Tipos_Facturas_Cata TF ON F.ID_Tipo_Factura = TF.ID_Tipo_Factura
    INNER JOIN Tipos_Pagos_Cata TP ON F.ID_Tipo_Pagos = TP.ID_Tipo_Pagos
    INNER JOIN Sucursales_cata SC ON SC.id_sucursal = F.id_sucursal
    LEFT JOIN TIPO_NFC TN ON TN.NFCTipoNumero = F.NFCTipoNumero
    LEFT JOIN Vendedores v ON v.ID_Vendedor = F.ID_Vendedor
    LEFT JOIN Contactos CS ON CS.ID_Contacto = v.ID_Contacto
    WHERE
        (@Criterio IS NULL OR (Co.Nombres + ' ' + Co.Apellidos LIKE '%' + @Criterio + '%')
            OR F.Numero_Factura LIKE @Criterio
            OR te.Numero_Telefono LIKE @Criterio
            OR TF.Descripcion_Factura_Tipo = @Criterio)
        AND (@BusquedaGlobal = 1 OR @Almacen = 0 OR F.id_sucursal = @Almacen)
        AND (@Vendedor = 0 OR F.ID_Vendedor = @Vendedor)
        AND (@IdTipoPago = 0 OR F.ID_Tipo_Pagos = @IdTipoPago)
        AND (@BusquedaGlobal = 1 OR (f.Fecha_Registro >= @Fecha_Inicio
            AND f.Fecha_Registro < DATEADD(DAY, 1, @Fecha_Final)))
        AND (@EstadoFactura IS NULL OR F.Estado = @EstadoFactura)
        AND (@Comprobante <> 2 OR F.NFCTipoNumero <> 0);

    ;WITH FacturasCTE AS (
        SELECT
            F.Numero_Factura AS [Numero de Factura],
            Co.Nombres,
            Co.Apellidos,
            F.ID_Cliente,
            te.Numero_Telefono AS Telefono,
            TF.Descripcion_Factura_Tipo AS [Tipo De Factura],
            ISNULL(CS.Nombres, '') AS [Vendedor],
            TP.Descripcion_Tipo_Pagos AS [Tipo de Pago],
            SC.Sucursal_Descripcion AS Almacen,
			F.ID_Sucursal AS IdSucursal,
			F.Abono,
            F.SubTotal,
            F.Descuento,
            F.NCF_NUMERO AS NCF,
            F.NFCTipoNumero,
            TN.NFCTipoDescripcion AS TipoComprobante,
            F.RNC,
            F.Total,
            F.Itbis,
            F.Nota,
            CAST(F.Fecha_registro AS DATE) AS [Fecha de la Factura],
            F.Estado AS EstadoFactura,
            @TotalFacturado AS TotalFacturado,
            @TotalItbis AS TotalItbis,
            @TotalSinItbis AS TotalSinItbis,
            ROW_NUMBER() OVER (ORDER BY F.Fecha_registro DESC) AS Linea,
            COUNT(*) OVER() AS Cantidad_Registros
        FROM Facturas F
        INNER JOIN Clientes C ON F.ID_Cliente = C.ID_Cliente
        INNER JOIN Contactos Co ON C.ID_Contacto = Co.ID_Contacto
        INNER JOIN Telefonos te ON te.ID_Contacto = C.ID_Contacto
        INNER JOIN Tipos_Facturas_Cata TF ON F.ID_Tipo_Factura = TF.ID_Tipo_Factura
        INNER JOIN Tipos_Pagos_Cata TP ON F.ID_Tipo_Pagos = TP.ID_Tipo_Pagos
        INNER JOIN Sucursales_cata SC ON SC.id_sucursal = F.id_sucursal
        LEFT JOIN TIPO_NFC TN ON TN.NFCTipoNumero = F.NFCTipoNumero
        LEFT JOIN Vendedores v ON v.ID_Vendedor = F.ID_Vendedor
        LEFT JOIN Contactos CS ON CS.ID_Contacto = v.ID_Contacto
        WHERE
            (@Criterio IS NULL OR (Co.Nombres + ' ' + Co.Apellidos LIKE '%' + @Criterio + '%')
                OR F.Numero_Factura LIKE @Criterio
                OR te.Numero_Telefono LIKE @Criterio
                OR TF.Descripcion_Factura_Tipo = @Criterio)
            AND (@BusquedaGlobal = 1 OR @Almacen = 0 OR F.id_sucursal = @Almacen)
            AND (@Vendedor = 0 OR F.ID_Vendedor = @Vendedor)
            AND (@IdTipoPago = 0 OR F.ID_Tipo_Pagos = @IdTipoPago)
            AND (@BusquedaGlobal = 1 OR (f.Fecha_Registro >= @Fecha_Inicio
                AND f.Fecha_Registro < DATEADD(DAY, 1, @Fecha_Final)))
            AND (@EstadoFactura IS NULL OR F.Estado = @EstadoFactura)
            AND (@Comprobante <> 2 OR F.NFCTipoNumero <> 0)
    )
    SELECT *,
        CASE
            WHEN Cantidad_Registros < (@PageIndex * @PageSize)
                THEN Cantidad_Registros
            ELSE (@PageIndex * @PageSize)
        END AS Ultima_Linea
    FROM FacturasCTE
    WHERE Linea BETWEEN ((@PageIndex - 1) * @PageSize) + 1
                    AND (@PageIndex * @PageSize)
    ORDER BY Linea
    OPTION (RECOMPILE);

END