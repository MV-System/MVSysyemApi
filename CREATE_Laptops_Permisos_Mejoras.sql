-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - permisos nuevos en seg.Permisos
--            LAPTOP_CAMBIO_ANULAR  -> Tarea 6: boton Anular en el historial de cambios de pieza
--            LAPTOP_CORREGIR_CONFIG -> Tarea 2: boton Corregir modulos de una laptop
--            LAPTOP_CONSUMO_COMPONENTES -> Tarea 7: reporte general de consumo de componentes
--            Idempotente: solo inserta los que no existen.
-- Ejecutar en la BD SEGURIDAD (la que lee PermisosService con la cadena MVSystemSeguridad).
-- Despues: asignar los permisos a los usuarios desde la pantalla de Seguridad.
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================
-- Mismo modulo que MNU_MANT_EQUIPOS
DECLARE @Modulo INT = (SELECT TOP 1 Modulo FROM seg.Permisos WHERE Nombre = 'MNU_MANT_EQUIPOS');

IF @Modulo IS NULL
    THROW 50300, 'No se encontro el modulo de seg.Permisos (MNU_MANT_EQUIPOS). Verifique que esta en la BD SEGURIDAD.', 1;

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'LAPTOP_CAMBIO_ANULAR')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('LAPTOP_CAMBIO_ANULAR', 'LAPTOPS: ANULAR UN CAMBIO DE PIEZA', @Modulo, 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'LAPTOP_CORREGIR_CONFIG')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('LAPTOP_CORREGIR_CONFIG', 'LAPTOPS: CORREGIR LOS MODULOS (RAM / DISCOS) DE UNA LAPTOP', @Modulo, 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'LAPTOP_CONSUMO_COMPONENTES')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('LAPTOP_CONSUMO_COMPONENTES', 'LAPTOPS: REPORTE DE CONSUMO DE COMPONENTES', @Modulo, 0, 1, GETDATE());

SELECT Codigo, Nombre, Descripcion, Modulo, Basico, Estado
  FROM seg.Permisos
 WHERE Nombre LIKE 'LAPTOP[_]%'
 ORDER BY Nombre;
GO
