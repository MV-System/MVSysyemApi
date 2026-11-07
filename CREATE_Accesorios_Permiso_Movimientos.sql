-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 13 (vista Movimientos de accesorios)
--            ACCESORIOS_MOVIMIENTOS -> ver los movimientos de stock de los accesorios (consulta de accesorios)
--            Idempotente: solo inserta si no existe.
-- Ejecutar en la BD SEGURIDAD (la que lee PermisosService con la cadena MVSystemSeguridad).
-- Despues: asignar el permiso a los usuarios desde la pantalla de Seguridad.
-- Autor: MVSystem
-- Fecha: 2026-11-02
-- ============================================================
-- Mismo modulo que el menu de accesorios
DECLARE @Modulo INT = (SELECT TOP 1 Modulo FROM seg.Permisos WHERE Nombre = 'MNU_MANT_ACCESORIOS');

IF @Modulo IS NULL
    THROW 50301, 'No se encontro el permiso MNU_MANT_ACCESORIOS en seg.Permisos. Verifique que esta en la BD SEGURIDAD.', 1;

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'ACCESORIOS_MOVIMIENTOS')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('ACCESORIOS_MOVIMIENTOS', 'ACCESORIOS: VER LOS MOVIMIENTOS DE STOCK', @Modulo, 0, 1, DATEADD(HOUR, -4, GETDATE()));

SELECT Codigo, Nombre, Descripcion, Modulo, Basico, Estado
  FROM seg.Permisos
 WHERE Nombre = 'ACCESORIOS_MOVIMIENTOS';
GO
