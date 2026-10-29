-- ============================================================
-- Feature: Tareas pendientes - Cancelar una reparación abierta
-- Propósito: Crear el permiso REPARACION_CANCELAR en seg.Permisos.
--            Lo exigen el botón "Cancelar reparación" y el endpoint Equipos/CancelarReparacion.
--            Idempotente: solo inserta si no existe.
-- Ejecutar en la BD SEGURIDAD (la que lee PermisosService con la cadena MVSystemSeguridad).
-- Después: asignar el permiso a los usuarios que deban cancelar desde la pantalla de Seguridad
--          (el token se genera al iniciar sesión: deben volver a entrar).
-- Autor: MVSystem
-- Fecha: 2026-10-28
-- ============================================================
-- Mismo módulo que el permiso de consulta de reparaciones (o el de equipos si no existe)
DECLARE @Modulo INT = (SELECT TOP 1 Modulo FROM seg.Permisos WHERE Nombre = 'MNU_CONS_REPARACIONES');

IF @Modulo IS NULL
    SET @Modulo = (SELECT TOP 1 Modulo FROM seg.Permisos WHERE Nombre = 'MNU_MANT_EQUIPOS');

IF @Modulo IS NULL
    THROW 50510, 'No se encontro el modulo de seg.Permisos (MNU_CONS_REPARACIONES / MNU_MANT_EQUIPOS). Verifique que esta en la BD SEGURIDAD.', 1;

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'REPARACION_CANCELAR')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('REPARACION_CANCELAR', 'REPARACIONES: CANCELAR UNA REPARACION ABIERTA', @Modulo, 0, 1, GETDATE());

SELECT Codigo, Nombre, Descripcion, Modulo, Basico, Estado
  FROM seg.Permisos
 WHERE Nombre = 'REPARACION_CANCELAR';
GO
