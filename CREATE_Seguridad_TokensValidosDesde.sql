-- ============================================================
-- Seguridad Parte 3: cortar el acceso al instante (usuario desactivado o desconectado)
--   1) dbo.Usuarios.TokensValidosDesde: los tokens emitidos ANTES de esta fecha (UTC) dejan de valer.
--      NULL = ningun corte.
--   2) Permiso USO_SISTEMA_DESCONECTAR: boton "Desconectar" en Uso del sistema.
-- Idempotente. Ejecutar en la BD SEGURIDAD, antes de publicar el API.
-- Fecha: 2026-11-10
-- ============================================================
IF COL_LENGTH('dbo.Usuarios', 'TokensValidosDesde') IS NULL
    ALTER TABLE dbo.Usuarios ADD TokensValidosDesde DATETIME NULL;
GO

-- Mismo modulo que ModoDesarrollador
DECLARE @Modulo INT = (SELECT TOP 1 Modulo FROM seg.Permisos WHERE Nombre = 'ModoDesarrollador');

IF @Modulo IS NULL
    THROW 50300, 'No se encontro el modulo de seg.Permisos (ModoDesarrollador). Verifique que esta en la BD SEGURIDAD.', 1;

IF NOT EXISTS (SELECT 1 FROM seg.Permisos WHERE Nombre = 'USO_SISTEMA_DESCONECTAR')
    INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro)
    VALUES ('USO_SISTEMA_DESCONECTAR', 'USO DEL SISTEMA: DESCONECTAR USUARIOS', @Modulo, 0, 1, GETDATE());

SELECT COL_LENGTH('dbo.Usuarios', 'TokensValidosDesde') AS ColumnaCreada;
SELECT Codigo, Nombre, Descripcion, Modulo, Basico, Estado FROM seg.Permisos WHERE Nombre = 'USO_SISTEMA_DESCONECTAR';
GO
