-- =============================================
-- Script: Crear tabla TrackingNotificacionConfig
-- Base de datos: SEGURIDAD
-- Descripción: Configuración de módulos y acciones 
--              que envían notificaciones WhatsApp
-- =============================================

USE [SEGURIDAD]
GO

-- Crear la tabla de configuración
CREATE TABLE [dbo].[TrackingNotificacionConfig] (
    [IdConfig] INT IDENTITY(1,1) PRIMARY KEY,
    [Modulo] NVARCHAR(100) NULL,          -- Si es NULL, aplica a todos los módulos
    [Accion] NVARCHAR(100) NULL,          -- Si es NULL, aplica a todas las acciones
    [NotificarWhatsApp] BIT NOT NULL DEFAULT 1,
    [Activo] BIT NOT NULL DEFAULT 1,
    [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
    [FechaModificacion] DATETIME NULL,
    
    -- Índices para mejorar el rendimiento
    INDEX IX_TrackingConfig_Modulo NONCLUSTERED ([Modulo]),
    INDEX IX_TrackingConfig_Accion NONCLUSTERED ([Accion]),
    INDEX IX_TrackingConfig_Activo NONCLUSTERED ([Activo]) WHERE [Activo] = 1
)
GO

-- Insertar configuraciones por defecto
INSERT INTO [dbo].[TrackingNotificacionConfig] 
    ([Modulo], [Accion], [NotificarWhatsApp], [Activo])
VALUES 
    -- Módulos completos que siempre notifican
    ('Dashboard IA', NULL, 1, 1),                    -- Todas las acciones del Dashboard IA
    ('Equipos Disponibles', 'Acceso', 1, 1),        -- Solo cuando acceden a Equipos Disponibles
    ('Facturas', 'Crear', 1, 1),                    -- Solo cuando crean una factura
    ('Facturas', 'Eliminar', 1, 1),                 -- Solo cuando eliminan una factura
    
    -- Acciones específicas que siempre notifican (sin importar el módulo)
    (NULL, 'Eliminar', 1, 1),                       -- Cualquier eliminación
    (NULL, 'Modificar Configuración', 1, 1),        -- Cualquier cambio de configuración
    
    -- Ejemplos de configuraciones desactivadas
    ('Reportes', NULL, 0, 1),                       -- No notificar nada de Reportes
    ('Clientes', 'Consultar', 0, 1)                 -- No notificar consultas de clientes
GO

-- Consulta de verificación
SELECT 
    IdConfig,
    ISNULL(Modulo, '(Todos)') AS Modulo,
    ISNULL(Accion, '(Todas)') AS Accion,
    NotificarWhatsApp,
    Activo,
    FechaCreacion
FROM [dbo].[TrackingNotificacionConfig]
ORDER BY Modulo, Accion
GO

PRINT '✅ Tabla TrackingNotificacionConfig creada correctamente.'
PRINT '📋 Se insertaron configuraciones por defecto.'
PRINT ''
PRINT '💡 Instrucciones de uso:'
PRINT '   - Modulo = NULL, Accion = NULL: Aplica a TODO el sistema'
PRINT '   - Modulo = "X", Accion = NULL: Todas las acciones del módulo X'
PRINT '   - Modulo = NULL, Accion = "Y": Acción Y en cualquier módulo'
PRINT '   - Modulo = "X", Accion = "Y": Solo acción Y del módulo X'
PRINT '   - NotificarWhatsApp = 1: Envía notificación'
PRINT '   - NotificarWhatsApp = 0: NO envía notificación'
PRINT '   - Activo = 0: Configuración deshabilitada (ignorada)'
GO
