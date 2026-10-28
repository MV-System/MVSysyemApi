-- ============================================================
-- Feature: Módulo de Laptops - Fase 1, Tarea 2
-- Propósito: Crear la marca 'Componentes' y sus modelos por tipo de
--            pieza (RAM, SSD, HDD), asignados al grupo 'Componentes'.
--            La capacidad/velocidad va en la descripción del accesorio,
--            no en el modelo. Script idempotente.
-- Requiere: CREATE_Laptops_Fase1_Grupos.sql ejecutado.
-- Nota: Modelos_Insert valida unicidad por Descripcion (global, sin
--       importar la marca); este script mantiene el mismo criterio.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================

DECLARE @ID_Grupo INT = (SELECT ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes');

IF @ID_Grupo IS NULL
BEGIN
    RAISERROR('No existe el grupo Componentes. Ejecute primero CREATE_Laptops_Fase1_Grupos.sql', 16, 1);
    RETURN;
END

-- 1. Marca 'Componentes'
IF NOT EXISTS (SELECT 1 FROM dbo.Marcas WHERE Descripcion = 'Componentes')
BEGIN
    INSERT INTO dbo.Marcas (Descripcion, Estado, Usuario, Fecha_registro)
    VALUES ('Componentes', 'A', 'sistema', DATEADD(HOUR, -4, GETDATE()));
END

DECLARE @ID_Marca INT = (SELECT TOP 1 ID_Marca FROM dbo.Marcas WHERE Descripcion = 'Componentes');

-- 2. Modelos por tipo de pieza
INSERT INTO dbo.Modelos (Descripcion, ID_Marca, Estado, Usuario, Fecha_registro, ID_Grupo)
SELECT V.Descripcion, @ID_Marca, 'A', 'sistema', DATEADD(HOUR, -4, GETDATE()), @ID_Grupo
FROM (VALUES
    ('RAM DDR3 SODIMM'),
    ('RAM DDR4 SODIMM'),
    ('RAM DDR5 SODIMM'),
    ('SSD SATA 2.5'),
    ('SSD M.2 SATA'),
    ('SSD M.2 NVMe'),
    ('HDD 2.5')
) AS V(Descripcion)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Modelos M WHERE M.Descripcion = V.Descripcion);

-- 3. Verificación: deben salir 7 modelos con marca Componentes y grupo Componentes.
--    Si alguno ya existía con otra marca/grupo, aparecerá en la segunda consulta.
SELECT M.ID_Modelo, M.Descripcion, Ma.Descripcion AS Marca, G.Descripcion AS Grupo, M.Estado
FROM dbo.Modelos M
JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
LEFT JOIN dbo.Grupos_Cata G ON G.ID_Grupo = M.ID_Grupo
WHERE M.Descripcion IN ('RAM DDR3 SODIMM','RAM DDR4 SODIMM','RAM DDR5 SODIMM',
                        'SSD SATA 2.5','SSD M.2 SATA','SSD M.2 NVMe','HDD 2.5')
ORDER BY M.Descripcion;

SELECT M.ID_Modelo, M.Descripcion, Ma.Descripcion AS Marca, G.Descripcion AS Grupo, 'CONFLICTO: ya existía con otra marca/grupo' AS Aviso
FROM dbo.Modelos M
JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
LEFT JOIN dbo.Grupos_Cata G ON G.ID_Grupo = M.ID_Grupo
WHERE M.Descripcion IN ('RAM DDR3 SODIMM','RAM DDR4 SODIMM','RAM DDR5 SODIMM',
                        'SSD SATA 2.5','SSD M.2 SATA','SSD M.2 NVMe','HDD 2.5')
  AND (M.ID_Marca <> @ID_Marca OR ISNULL(M.ID_Grupo, 0) <> @ID_Grupo);
GO
