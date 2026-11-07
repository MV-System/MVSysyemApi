-- ============================================================
-- Permiso para ver la columna de costo en equipos disponibles y equipos vendidos
--   EQP_VER_COSTO -> muestra la columna/dato "Costo" en ambas consultas
-- Idempotente: solo inserta si no existe.
-- Ejecutar en la BD SEGURIDAD (la que lee PermisosService con la cadena MVSystemSeguridad).
-- Despues: asignar el permiso a los usuarios que pueden ver costos desde la pantalla de Seguridad
-- (sin el permiso, la columna Costo no se muestra).
-- Fecha: 2026-11-06
-- ============================================================
-- Mismo modulo que MNU_MANT_EQUIPOS
DECLARE @Modulo INT = (SELECT TOP 1 Modulo FROM seg.Permisos WHERE Nombre = 'MNU_MANT_EQUIPOS');

IF @Modulo IS NULL
    THROW 50300, 'No se encontro el modulo de seg.Permisos (MNU_MANT_EQUIPOS). Verifique que esta en la BD SEGURIDAD.', 1;

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'EQP_VER_COSTO')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('EQP_VER_COSTO', 'EQUIPOS: VER COSTO EN EQUIPOS DISPONIBLES Y VENDIDOS', @Modulo, 0, 1, GETDATE());

SELECT Codigo, Nombre, Descripcion, Modulo, Basico, Estado
  FROM seg.Permisos
 WHERE Nombre = 'EQP_VER_COSTO';
GO
