-- =============================================
-- Script: Debugging de Tracking y Notificaciones
-- Base de datos: SEGURIDAD
-- =============================================

USE [SEGURIDAD]
GO

PRINT '=========================================='
PRINT '1. CONFIGURACIÓN DE NOTIFICACIONES'
PRINT '=========================================='
SELECT 
    IdConfig,
    ISNULL(Modulo, '(Todos)') AS Modulo,
    ISNULL(Accion, '(Todas)') AS Accion,
    CASE WHEN NotificarWhatsApp = 1 THEN '✅ SÍ' ELSE '❌ NO' END AS Notifica,
    CASE WHEN Activo = 1 THEN '✅ Activa' ELSE '❌ Inactiva' END AS Estado
FROM [dbo].[TrackingNotificacionConfig]
WHERE Modulo = 'Equipos Disponibles'
ORDER BY Modulo, Accion
GO

PRINT ''
PRINT '=========================================='
PRINT '2. ÚLTIMOS 10 REGISTROS DE TRACKING'
PRINT '=========================================='
SELECT TOP 10
    IdTracking,
    Usuario,
    Modulo,
    Accion,
    FechaHora,
    NotificacionEnviada,
    FechaNotificacion,
    SUBSTRING(Metadata, 1, 100) AS Metadata_Snippet
FROM [dbo].[TrackingLogs]
ORDER BY FechaHora DESC
GO

PRINT ''
PRINT '=========================================='
PRINT '3. REGISTROS DE EQUIPOS DISPONIBLES'
PRINT '=========================================='
SELECT 
    IdTracking,
    Usuario,
    Accion,
    FechaHora,
    NotificacionEnviada,
    FechaNotificacion
FROM [dbo].[TrackingLogs]
WHERE Modulo = 'Equipos Disponibles'
ORDER BY FechaHora DESC
GO

PRINT ''
PRINT '=========================================='
PRINT '4. PRUEBA DE LÓGICA DebeNotificar'
PRINT '=========================================='

-- Verificar regla exacta
DECLARE @Modulo VARCHAR(100) = 'Equipos Disponibles'
DECLARE @Accion VARCHAR(100) = 'Acceso'

SELECT 
    'Regla Exacta' AS TipoRegla,
    IdConfig,
    Modulo,
    Accion,
    NotificarWhatsApp,
    Activo
FROM TrackingNotificacionConfig
WHERE Modulo = @Modulo 
  AND Accion = @Accion
  AND Activo = 1

UNION ALL

-- Verificar regla de módulo (cualquier acción)
SELECT 
    'Regla Módulo' AS TipoRegla,
    IdConfig,
    Modulo,
    Accion,
    NotificarWhatsApp,
    Activo
FROM TrackingNotificacionConfig
WHERE Modulo = @Modulo 
  AND Accion IS NULL
  AND Activo = 1

UNION ALL

-- Verificar regla de acción (cualquier módulo)
SELECT 
    'Regla Acción' AS TipoRegla,
    IdConfig,
    Modulo,
    Accion,
    NotificarWhatsApp,
    Activo
FROM TrackingNotificacionConfig
WHERE Modulo IS NULL 
  AND Accion = @Accion
  AND Activo = 1

UNION ALL

-- Verificar regla genérica
SELECT 
    'Regla Genérica' AS TipoRegla,
    IdConfig,
    Modulo,
    Accion,
    NotificarWhatsApp,
    Activo
FROM TrackingNotificacionConfig
WHERE Modulo IS NULL 
  AND Accion IS NULL
  AND Activo = 1
GO

PRINT ''
PRINT '=========================================='
PRINT '5. ACTIVAR NOTIFICACIÓN PARA PRUEBA'
PRINT '=========================================='

-- Activar notificación para Equipos Disponibles - Acceso
UPDATE TrackingNotificacionConfig 
SET NotificarWhatsApp = 1 
WHERE Modulo = 'Equipos Disponibles' 
  AND Accion = 'Acceso'

SELECT 
    '✅ Notificación ACTIVADA' AS Resultado,
    Modulo,
    Accion,
    NotificarWhatsApp,
    Activo
FROM TrackingNotificacionConfig
WHERE Modulo = 'Equipos Disponibles' 
  AND Accion = 'Acceso'
GO

PRINT ''
PRINT '=========================================='
PRINT 'RESUMEN:'
PRINT 'Si NotificarWhatsApp = 1 y Activo = 1,'
PRINT 'la próxima vez que accedas a Equipos'
PRINT 'Disponibles DEBE llegar el mensaje.'
PRINT '=========================================='
