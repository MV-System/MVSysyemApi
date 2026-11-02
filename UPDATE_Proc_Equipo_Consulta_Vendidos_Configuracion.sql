-- ============================================================
-- Proc_Equipo_Consulta_Vendidos + columna Configuracion (modulo de laptops)
-- (Reemplaza la version anterior de este archivo, que traia una version vieja del SP.)
-- El script del modulo de laptops reemplazo la version vigente (2025-12-10, "simplificada y funcional")
-- por una version vieja (CTE, INNER JOIN a Telefonos, otro orden y otros filtros de fecha).
-- Este script restaura la version vigente y SOLO le agrega la columna Configuracion (laptops).
-- Ejecutar en las bases donde ya se corrio SCRIPT_MODULO_LAPTOPS_COMPLETO.sql (principal y espejo).
-- Requiere dbo.fn_Equipo_Configuracion_Texto (ya creada por el script de laptops).
-- Fecha: 2026-11-01
-- ============================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[Proc_Equipo_Consulta_Vendidos]
@PageIndex int,
@PageSize Int,
@Almacen int = 0,
@Imei varchar(30) = '',
@Telefono bigint = 0,
@Suplidor varchar(150) = '',
@Vendedor varchar(150) = '',
@ID_Modelo int = 0,
@Fecha_factura_Inicial datetime = '',
@Fecha_factura_Final datetime = '',
@Fecha_Inicio datetime = '',
@Fecha_Final datetime = ''
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @PageIndex = CASE WHEN @PageIndex <= 0 THEN 1 ELSE @PageIndex END;
    
    -- Convertir almacén si es necesario
    IF @Almacen = 4 SET @Almacen = 0;
    IF @Almacen > 0 SET @Almacen = ISNULL((SELECT TOP 1 Id_almacen FROM Sucursales_cata WHERE ID_Sucursal = @Almacen), 0);
    
    -- Tabla temporal para resultados
    CREATE TABLE #Resultado (
        RowNum INT,
        ID_Equipo INT,
        Imei VARCHAR(50),
        Descripcion VARCHAR(MAX),
        [Por Mayor] DECIMAL(18,2),
        [Al Detalle] DECIMAL(18,2),
        [Costo] DECIMAL(18,2),
        [Precio Vendido] DECIMAL(18,2),
        [Tipo Factura] VARCHAR(100),
        Desb VARCHAR(50),
        Itbis DECIMAL(18,2),
        Nota VARCHAR(MAX),
        Descripcion_Almacen VARCHAR(100),
        Suplidor VARCHAR(MAX),
        Vendedor VARCHAR(MAX),
        cliente VARCHAR(MAX),
        [Fecha facturado] DATETIME,
        Factura VARCHAR(50),
        Fecha_registro DATETIME,
        [Comision Detalle] DECIMAL(18,2),
        [Comision Por Mayor] DECIMAL(18,2),
        Configuracion VARCHAR(100)
    );
    
    -- CTE para numeración eficiente
    WITH DatosNumerados AS (
        SELECT 
            ROW_NUMBER() OVER (ORDER BY ft.fecha_registro DESC, e.Imei ASC) as RowNum,
            E.ID_Equipo,
            e.Imei,
            M.Descripcion,
            E.Precio_Por_Mayor AS [Por Mayor],
            E.Precio_Detalle AS [Al Detalle],
            E.Costo_Equipo AS [Costo],
            df.total AS [Precio Vendido],
            ISNULL(tpc.descripcion_tipo_pagos, '') AS [Tipo Factura],
            ISNULL(E.Desbloqueado, '') AS Desb,
            ISNULL(df.Itbis, 0) AS Itbis,
            ISNULL(e.Nota_Adicional, '') AS Nota,
            ISNULL(al.Descripcion_Almacen, '') AS Descripcion_Almacen,
            ISNULL(cs.Nombres + ' ' + ISNULL(cs.Apellidos, ''), '') AS Suplidor,
            ISNULL(c.Nombres + ' ' + ISNULL(c.Apellidos, ''), '') AS Vendedor,
            ISNULL(co.Nombres + ' ' + ISNULL(co.Apellidos, ''), '') AS cliente,
            df.fecha_registro AS [Fecha facturado],
            ft.Numero_Factura AS Factura,
            E.Fecha_registro,
            ISNULL(e.Comision_Detalle, 0) AS [Comision Detalle],
            ISNULL(e.Comision_Por_Mayor, 0) AS [Comision Por Mayor],
            dbo.fn_Equipo_Configuracion_Texto(e.Imei) AS Configuracion
        FROM Equipos e WITH(NOLOCK)
        INNER JOIN Modelos M WITH(NOLOCK) ON e.ID_Modelo = M.ID_Modelo
        INNER JOIN Detalle_Facturas df WITH(NOLOCK) ON e.Imei = df.ID_Equipo AND df.estado = 'A' AND df.ID_Tipo = 1
        INNER JOIN Facturas ft WITH(NOLOCK) ON ft.numero_factura = df.numero_factura AND ft.estado = 'A' AND ft.ID_Tipo_Factura <> 3
        INNER JOIN Almacen_Cata al WITH(NOLOCK) ON E.ID_Almacen = al.ID_Almacen
        LEFT JOIN Vendedores v WITH(NOLOCK) ON v.ID_Vendedor = ft.ID_Vendedor
        LEFT JOIN Contactos c WITH(NOLOCK) ON c.ID_Contacto = v.ID_Contacto
        LEFT JOIN Clientes cl WITH(NOLOCK) ON cl.id_cliente = ft.id_cliente
        LEFT JOIN Contactos co WITH(NOLOCK) ON co.id_contacto = cl.id_contacto

        LEFT JOIN Suplidores su WITH(NOLOCK) ON su.id_suplidor = e.ID_Suplidor
        LEFT JOIN Contactos cs WITH(NOLOCK) ON cs.id_contacto = su.id_contacto
        LEFT JOIN tipos_pagos_cata tpc WITH(NOLOCK) ON tpc.id_tipo_pagos = ft.id_tipo_pagos
        WHERE e.disponible = 'N'
          AND ((@Almacen = 0) OR (E.ID_Almacen = @Almacen))
          AND ((@Imei = '') OR (e.Imei = @Imei))
          AND ((@Suplidor = '') OR (cs.Nombres + ' ' + ISNULL(cs.Apellidos, '') LIKE '%' + @Suplidor + '%'))
          AND ((@Vendedor = '') OR (c.Nombres + ' ' + ISNULL(c.Apellidos, '') LIKE '%' + @Vendedor + '%'))
          AND ((@ID_Modelo = 0) OR (M.ID_Modelo = @ID_Modelo))
          AND ((@Fecha_factura_Inicial = '' OR @Fecha_factura_Final = '') OR (CAST(df.fecha_registro AS DATE) BETWEEN CAST(@Fecha_factura_Inicial AS DATE) AND CAST(@Fecha_factura_Final AS DATE)))
          AND ((@Fecha_Inicio = '' OR @Fecha_Final = '') OR (CAST(e.Fecha_registro AS DATE) BETWEEN CAST(@Fecha_Inicio AS DATE) AND CAST(@Fecha_Final AS DATE)))
    )
    INSERT INTO #Resultado
    SELECT * FROM DatosNumerados;
    
    -- Calcular totales de manera eficiente
    DECLARE @TotalRegistros INT = (SELECT COUNT(*) FROM #Resultado);
    DECLARE @TotalInventario MONEY = (SELECT ISNULL(SUM([Costo]), 0) FROM #Resultado);
    DECLARE @TotalFacturado MONEY = (SELECT ISNULL(SUM([Precio Vendido]), 0) FROM #Resultado);
    DECLARE @TotalGanancia MONEY = (@TotalFacturado - @TotalInventario);
    DECLARE @TotalComisionDetalle MONEY = (SELECT ISNULL(SUM([Comision Detalle]), 0) FROM #Resultado);
    DECLARE @TotalComisionXMayor MONEY = (SELECT ISNULL(SUM([Comision Por Mayor]), 0) FROM #Resultado);
    DECLARE @TotalItbis MONEY = (SELECT ISNULL(SUM(Itbis), 0) FROM #Resultado);
    
    -- Retornar datos paginados
    SELECT 
        *,
        TotalInventario = @TotalInventario,
        TotalFacturado = @TotalFacturado,
        TotalGanancia = @TotalGanancia,
        TotalComisionDetalle = @TotalComisionDetalle,
        TotalComisionXMayor = @TotalComisionXMayor,
        TotalItbis = @TotalItbis,
        Linea = (@PageSize * (@PageIndex - 1) + 1),
        Ultima_Linea = CASE WHEN (@PageSize * @PageIndex) > @TotalRegistros THEN @TotalRegistros ELSE (@PageSize * @PageIndex) END,
        Cantidad_Registros = @TotalRegistros
    FROM #Resultado
    WHERE RowNum > (@PageSize * (@PageIndex - 1))
      AND RowNum <= (@PageSize * @PageIndex)
    ORDER BY RowNum;
    
    DROP TABLE #Resultado;
END
GO
