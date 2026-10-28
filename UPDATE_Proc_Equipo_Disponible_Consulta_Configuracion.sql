-- ============================================================
-- Feature: Módulo de Laptops - Fase 2, Tarea 6
-- Propósito: Proc_Equipo_Disponible_Consulta devuelve la columna nueva
--            Configuracion = dbo.fn_Equipo_Configuracion_Texto(Imei)
--            (ej. '12GB DDR4 / 256GB SSD SATA'; NULL si no tiene).
--            No se quita ni se renombra ninguna columna ni parámetro.
-- Requiere: CREATE_Equipo_Configuracion.sql ejecutado.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[Proc_Equipo_Disponible_Consulta]
    @PageIndex INT,
    @PageSize INT,
    @Almacen INT = 0,
    @Suplidor VARCHAR(60) = '',
    @Imei VARCHAR(60) = '',
    @ID_Modelo INT = 0,
    @Grupo INT = NULL,
    @Fecha_Inicio DATETIME = '',
    @Fecha_Final DATETIME = ''
AS
BEGIN
    DECLARE @FechaI DATETIME, @FechaF DATETIME

    SET @FechaI = @Fecha_Inicio
    SET @FechaF = @Fecha_Final

    IF @Almacen = 4
        SET @Almacen = 0;
    ELSE
        SET @Almacen = (SELECT Id_almacen FROM Sucursales_cata WHERE ID_Sucursal = @Almacen);

    DECLARE @FIni INT, @FFin INT

    SET @FIni = YEAR(@FechaI) * 10000 + MONTH(@FechaI) * 100 + DAY(@FechaI)
    SET @FFin = YEAR(@FechaF) * 10000 + MONTH(@FechaF) * 100 + DAY(@FechaF)
    DECLARE @PageIni INT, @PageEnd INT, @Linea INT

    SET @PageIni = 0;
    SET @PageEnd = 0;

    SET @PageIndex = CASE WHEN @PageIndex = 0 THEN 1 ELSE @PageIndex END;
    SET @PageEnd = (@PageIndex * @PageSize);
    SET @PageIni = (@PageEnd - @PageSize);

    -- Suma total inventario
    DECLARE @TotalInventario MONEY;
    SELECT @TotalInventario = SUM(Costo_Equipo)
      FROM Equipos E
      INNER JOIN Modelos AS M ON E.ID_Modelo = M.ID_Modelo
      LEFT JOIN dbo.Suplidores AS su ON su.id_suplidor = E.ID_Suplidor
      LEFT JOIN contactos AS CS ON CS.id_contacto = su.id_contacto
      WHERE (CS.Nombres = '' OR CS.Nombres + ' ' + CS.Apellidos LIKE '%' + @Suplidor + '%')
        AND ((@Almacen = 0) OR (E.ID_Almacen = @Almacen))
        AND ((@ID_Modelo = 0) OR (E.ID_Modelo = @ID_Modelo))
        AND (@Grupo IS NULL OR M.ID_Grupo = @Grupo)
        AND ((@Fecha_Inicio = '' AND @Fecha_Final = '') OR (YEAR(E.Fecha_registro) * 10000 + MONTH(E.Fecha_registro) * 100 + DAY(E.Fecha_registro)) BETWEEN @FIni AND @FFin)
        AND E.Disponible = 'S';

    SELECT  E.ID_Equipo numero,
            E.Imei,
            M.Descripcion,
            T.Tecnologia_Descripcion AS Tecnologia,
            G.Descripcion_Garantia AS Garantia,
            Es.Descripcion_Estado_Bloqueo AS [Estado Bloqueo],
            E.Precio_Por_Mayor AS [Por Mayor],
            E.Precio_Detalle AS [Al Detalle],
            E.Costo_Equipo AS [Costo],
            E.Comision_Detalle AS [Comision Detalle],
            E.Comision_Por_Mayor AS [Comision Por Mayor],
            E.Desbloqueado,
            E.Disponible,
            E.Nota_Adicional AS Nota,
            AC.Descripcion_Almacen,
            C.Nombres + ' ' + C.Apellidos AS Suplidor,
            S.ID_Suplidor,
            M.ID_Modelo,
            E.ID_Almacen,
            E.Fecha_registro,
            TotalInventario = @TotalInventario,
            M.ID_Grupo,
            ISNULL(GC.Descripcion, '') AS Grupo,
            dbo.fn_Equipo_Configuracion_Texto(E.Imei) AS Configuracion
    INTO #Temporal
    FROM Equipos AS E WITH(NOLOCK)
        JOIN Modelos AS M ON E.ID_Modelo = M.ID_Modelo
        JOIN dbo.Tecnologias_Cata AS T ON E.ID_Tecnologia = T.ID_Tecnologia
        JOIN dbo.Garantias_Cata AS G ON E.ID_Garantia = G.ID_Garantia
        JOIN dbo.Estados_Bloqueos_Cata AS Es ON E.ID_Estado_Bloqueo = Es.ID_Estado_Bloqueo
        LEFT JOIN dbo.Suplidores AS S ON E.ID_Suplidor = S.ID_Suplidor
        JOIN dbo.Almacen_Cata AS AC ON E.ID_Almacen = AC.ID_Almacen
        JOIN Contactos C ON S.ID_Contacto = C.ID_Contacto
        LEFT JOIN dbo.Grupos_Cata AS GC ON M.ID_Grupo = GC.ID_Grupo
    WHERE
        (C.Nombres = '' OR C.Nombres + ' ' + C.Apellidos LIKE '%' + @Suplidor + '%')
        AND ((@Almacen = 0) OR (E.ID_Almacen = @Almacen))
        AND ((@Imei = '') OR (E.Imei = @Imei))
        AND ((@ID_Modelo = 0) OR (E.ID_Modelo = @ID_Modelo))
        AND (@Grupo IS NULL OR M.ID_Grupo = @Grupo)
        AND ((@Fecha_Inicio = '' AND @Fecha_Final = '') OR (YEAR(E.Fecha_registro) * 10000 + MONTH(E.Fecha_registro) * 100 + DAY(E.Fecha_registro)) BETWEEN @FIni AND @FFin)
        AND E.Disponible = 'S'
    ORDER BY M.Descripcion

    -- Registros
    DECLARE @Registros INT
    SET @Registros = (SELECT COUNT(numero) FROM #Temporal);

    IF @PageEnd > @Registros
        SET @PageEnd = @Registros

    SELECT *,
           Linea = @PageIni + 1,
           Ultima_Linea = @PageEnd,
           Cantidad_Registros = @Registros
    FROM #Temporal
    ORDER BY numero
    OFFSET @PageSize * (@PageIndex - 1) ROWS
    FETCH NEXT @PageSize ROWS ONLY OPTION (RECOMPILE);

    DROP TABLE #Temporal
END
GO
