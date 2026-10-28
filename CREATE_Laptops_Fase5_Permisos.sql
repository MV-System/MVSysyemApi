-- ============================================================
-- Feature: Módulo de Laptops - Fase 5, Tarea 18
-- Propósito: Crear los permisos del módulo de Laptops en seg.Permisos:
--            LAPTOP_CAMBIO_PIEZA     -> pantalla/endpoints de cambio de pieza (y su historial)
--            LAPTOP_CUSTODIA_CONSULTA-> pantalla de custodia (lista de pendientes) e historial
--            LAPTOP_CUSTODIA_RECIBIR -> botones Recibir / Entregada al cliente
--            Idempotente: solo inserta los que no existen.
-- Ejecutar en la BD SEGURIDAD (la que lee PermisosService con la cadena MVSystemSeguridad).
-- Después: asignar los permisos a los usuarios desde la pantalla de Seguridad.
-- Autor: MVSystem
-- Fecha: 2026-10-28
-- ============================================================
-- Mismo módulo que MNU_MANT_EQUIPOS
DECLARE @Modulo INT = (SELECT TOP 1 Modulo FROM seg.Permisos WHERE Nombre = 'MNU_MANT_EQUIPOS');

IF @Modulo IS NULL
    THROW 50300, 'No se encontro el modulo de seg.Permisos (MNU_MANT_EQUIPOS). Verifique que esta en la BD SEGURIDAD.', 1;

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'LAPTOP_CAMBIO_PIEZA')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('LAPTOP_CAMBIO_PIEZA', 'LAPTOPS: CAMBIO DE PIEZA (RAM / ALMACENAMIENTO)', @Modulo, 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'LAPTOP_CUSTODIA_CONSULTA')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('LAPTOP_CUSTODIA_CONSULTA', 'LAPTOPS: CONSULTA DE PIEZAS EN CUSTODIA E HISTORIAL', @Modulo, 0, 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'LAPTOP_CUSTODIA_RECIBIR')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('LAPTOP_CUSTODIA_RECIBIR', 'LAPTOPS: RECIBIR PIEZA EN CUSTODIA / ENTREGAR AL CLIENTE', @Modulo, 0, 1, GETDATE());

SELECT Codigo, Nombre, Descripcion, Modulo, Basico, Estado
  FROM seg.Permisos
 WHERE Nombre LIKE 'LAPTOP[_]%'
 ORDER BY Nombre;
GO
