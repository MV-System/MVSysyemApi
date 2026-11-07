-- ============================================================
-- Feature: Módulo de Laptops - Fase 1, Tarea 1
-- Propósito: Crear los grupos 'Laptops' y 'Componentes' en
--            dbo.Grupos_Cata. 'Laptops' identifica los modelos que
--            requieren configuración (RAM/almacenamiento) y
--            'Componentes' agrupa los modelos de piezas (accesorios).
--            Script idempotente.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops')
BEGIN
    INSERT INTO dbo.Grupos_Cata (Descripcion, Estado, Usuario, Fecha_Registro)
    VALUES ('Laptops', 'A', 'sistema', GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes')
BEGIN
    INSERT INTO dbo.Grupos_Cata (Descripcion, Estado, Usuario, Fecha_Registro)
    VALUES ('Componentes', 'A', 'sistema', GETDATE());
END
GO

-- Verificación
SELECT ID_Grupo, Descripcion, Estado, Usuario, Fecha_Registro
FROM dbo.Grupos_Cata
WHERE Descripcion IN ('Laptops', 'Componentes');
GO
