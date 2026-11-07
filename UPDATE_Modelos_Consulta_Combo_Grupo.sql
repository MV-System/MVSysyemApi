-- ============================================================
-- Feature: Módulo de Laptops - Fase 2, Tarea 5
-- Propósito: Modelos_Consulta_Combo ahora devuelve también ID_Grupo
--            para que el front sepa si el modelo es del grupo Laptops
--            (muestra los campos de RAM/almacenamiento). Solo se AGREGA
--            una columna; el resto del SP queda igual.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[Modelos_Consulta_Combo]
    @criterio INT = NULL
AS
SELECT ID_Modelo, Descripcion, ID_Grupo
  FROM Modelos
 WHERE ID_Marca = @criterio
    OR @criterio IS NULL
ORDER BY Descripcion ASC
GO
