-- ============================================================
-- Feature: Módulo de Laptops - Fase 3, Tarea 11
-- Propósito: SPs de consulta para la pantalla de cambio de pieza.
--   1) Equipo_Cambio_Pieza_Equipo_Consultar: datos de la laptop por serial
--      (modelo, marca, costo actual, configuración, almacén y si es laptop).
--   2) Equipo_Cambio_Pieza_Accesorios: accesorios del grupo Componentes de un
--      almacén, filtrables por tipo de pieza y por existencia.
-- Requiere: CREATE_Laptops_Fase1_Grupos.sql, CREATE_Equipo_Configuracion.sql
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================

-- ============================================================
-- 1. Datos de la laptop por serial
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Equipo_Consultar
    @Imei VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ID_Grupo_Laptops INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');

    SELECT TOP 1
           E.Imei,
           Modelo = M.Descripcion,
           Marca = Ma.Descripcion,
           Costo_Equipo = CAST(E.Costo_Equipo AS DECIMAL(18,2)),
           Precio_Detalle = CAST(E.Precio_Detalle AS DECIMAL(18,2)),
           Precio_Por_Mayor = CAST(E.Precio_Por_Mayor AS DECIMAL(18,2)),
           Configuracion = dbo.fn_Equipo_Configuracion_Texto(E.Imei),
           E.ID_Almacen,
           AC.Descripcion_Almacen,
           E.Disponible,
           Es_Laptop = CAST(CASE WHEN M.ID_Grupo = @ID_Grupo_Laptops THEN 1 ELSE 0 END AS BIT)
    FROM dbo.Equipos E
    INNER JOIN dbo.Modelos M ON M.ID_Modelo = E.ID_Modelo
    INNER JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
    INNER JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = E.ID_Almacen
    WHERE E.Imei = @Imei;
END
GO

-- ============================================================
-- 2. Accesorios (Componentes) de un almacén
--    @Tipo_Pieza: NULL = todos | 'RAM' (modelos RAM*) | 'ALMACENAMIENTO' (modelos SSD*/HDD*)
--    @SoloConStock = 1 -> solo con existencia (para la pieza instalada)
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Accesorios
    @ID_Almacen    INT,
    @Tipo_Pieza    VARCHAR(20) = NULL,
    @Filtro        VARCHAR(100) = '',
    @SoloConStock  BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ID_Grupo_Componentes INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes');

    SELECT A.ID_Accesorio,
           A.Descripcion_Accesorio,
           Modelo = M.Descripcion,
           Cantidad = ISNULL(A.Cantidad, 0),
           Costo_Equipo = CAST(A.Costo_Equipo AS DECIMAL(18,2))
    FROM dbo.Accesorios A
    INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
    WHERE M.ID_Grupo = @ID_Grupo_Componentes
      AND A.ID_Almacen = @ID_Almacen
      AND A.Estado = 'A'
      AND (@SoloConStock = 0 OR ISNULL(A.Cantidad, 0) > 0)
      AND (@Tipo_Pieza IS NULL
           OR (@Tipo_Pieza = 'RAM' AND M.Descripcion LIKE 'RAM%')
           OR (@Tipo_Pieza = 'ALMACENAMIENTO' AND (M.Descripcion LIKE 'SSD%' OR M.Descripcion LIKE 'HDD%')))
      AND (ISNULL(@Filtro, '') = ''
           OR A.Descripcion_Accesorio LIKE '%' + @Filtro + '%'
           OR M.Descripcion LIKE '%' + @Filtro + '%')
    ORDER BY M.Descripcion, A.Descripcion_Accesorio;
END
GO
