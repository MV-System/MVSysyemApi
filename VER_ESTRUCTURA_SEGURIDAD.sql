-- ============================================================================
-- SCRIPT: Ver Estructura Completa de SEGURIDAD
-- Propósito: Verificar la estructura de todas las tablas en seg.*
-- Ejecutar en: MVSystemSeguridad
-- ============================================================================

USE MVSystemSeguridad;
GO

PRINT '========================================';
PRINT 'TABLAS EN SCHEMA seg';
PRINT '========================================';

SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA='seg'
ORDER BY TABLE_NAME;

PRINT '';
PRINT '========================================';
PRINT 'ESTRUCTURA DE seg.Permisos';
PRINT '========================================';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='seg' AND TABLE_NAME='Permisos'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'ESTRUCTURA DE seg.Modulos';
PRINT '========================================';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='seg' AND TABLE_NAME='Modulos'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'ESTRUCTURA DE seg.Sistemas';
PRINT '========================================';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='seg' AND TABLE_NAME='Sistemas'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'ESTRUCTURA DE seg.Departamentos';
PRINT '========================================';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='seg' AND TABLE_NAME='Departamentos'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'ESTRUCTURA DE seg.Perfiles';
PRINT '========================================';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='seg' AND TABLE_NAME='Perfiles'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'ESTRUCTURA DE seg.PerfilPermisos (si existe)';
PRINT '========================================';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='seg' AND TABLE_NAME='PerfilPermisos'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'DATOS DE EJEMPLO: Primeros 5 de seg.Permisos';
PRINT '========================================';

SELECT TOP 5 * FROM seg.Permisos;

PRINT '';
PRINT '========================================';
PRINT 'DATOS DE EJEMPLO: Primeros 5 de seg.Modulos';
PRINT '========================================';

SELECT TOP 5 * FROM seg.Modulos;

PRINT '';
PRINT '========================================';
PRINT 'DATOS DE EJEMPLO: Primeros 5 de seg.Sistemas';
PRINT '========================================';

SELECT TOP 5 * FROM seg.Sistemas;

PRINT '';
PRINT 'FIN DEL SCRIPT';
