/*
    Diagnóstico de solo lectura para comprobar la relación entre Cotizacion y Detalle_Cotizacion.
    No modifica datos ni objetos de la base de datos.

    Ejecutar primero en un ambiente seguro y compartir ambos resultados.
*/
SET NOCOUNT ON;

/*
    Resultado 1: últimas 200 cabeceras y conteos de detalles asociados:
    - Por número únicamente: detecta posibles colisiones entre sucursales/literales.
    - Por literal esperado + número: prueba la relación natural de la cotización.
*/
SELECT TOP (200)
    c.Codigo,
    c.Numero_Factura,
    c.ID_Sucursal,
    c.Literal AS LiteralCotizacion,
    LTRIM(RTRIM(ISNULL(c.Literal, ''))) + CONVERT(varchar(10), c.Numero_Factura) AS LiteralFEsperado,
    c.Fecha_registro,
    c.Estado,
    c.facturado,
    c.ID_Cliente,
    co.Nombres,
    co.Apellidos,
    porNumero.Cantidad AS DetallesMismoNumero,
    porLiteral.Cantidad AS DetallesNumeroYLiteralEsperado
FROM dbo.Cotizacion AS c
LEFT JOIN dbo.Clientes AS cl
    ON cl.ID_Cliente = c.ID_Cliente
LEFT JOIN dbo.Contactos AS co
    ON co.ID_Contacto = cl.ID_Contacto
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS Cantidad
    FROM dbo.Detalle_Cotizacion AS d
    WHERE d.Numero_Factura = c.Numero_Factura
) AS porNumero
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS Cantidad
    FROM dbo.Detalle_Cotizacion AS d
    WHERE d.Numero_Factura = c.Numero_Factura
      AND LTRIM(RTRIM(ISNULL(d.LiteralF, ''))) =
          LTRIM(RTRIM(ISNULL(c.Literal, ''))) + CONVERT(varchar(10), c.Numero_Factura)
) AS porLiteral
ORDER BY c.Fecha_registro DESC, c.Codigo DESC;

/*
    Resultado 2: últimas 200 filas de detalle con las cabeceras candidatas.
    CabecerasMismoNumero > 1 indica que Numero_Factura por sí solo no identifica
    de forma única la cotización. CabecerasCoincidenLiteral esperado ayuda a
    verificar si LiteralF permite distinguir la sucursal/cotización correcta.
*/
SELECT TOP (200)
    d.Numero_Factura,
    d.LiteralF,
    d.ID_Equipo,
    d.Descripcion,
    d.cantidad,
    d.Estado AS EstadoDetalle,
    d.Fecha_registro AS FechaDetalle,
    candidatas.CabecerasMismoNumero,
    coincidencias.CabecerasCoincidenLiteralEsperado
FROM dbo.Detalle_Cotizacion AS d
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS CabecerasMismoNumero
    FROM dbo.Cotizacion AS c
    WHERE c.Numero_Factura = d.Numero_Factura
) AS candidatas
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS CabecerasCoincidenLiteralEsperado
    FROM dbo.Cotizacion AS c
    WHERE c.Numero_Factura = d.Numero_Factura
      AND LTRIM(RTRIM(ISNULL(d.LiteralF, ''))) =
          LTRIM(RTRIM(ISNULL(c.Literal, ''))) + CONVERT(varchar(10), c.Numero_Factura)
) AS coincidencias
ORDER BY d.Fecha_registro DESC, d.Numero_Factura DESC;
