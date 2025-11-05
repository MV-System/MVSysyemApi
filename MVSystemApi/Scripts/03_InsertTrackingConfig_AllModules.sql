-- =============================================
-- Script: Configuración de tracking para todos los módulos
-- Base de datos: SEGURIDAD
-- Descripción: Registra todos los módulos con notificaciones DESACTIVADAS
--              Solo activa las que necesites cambiando NotificarWhatsApp = 1
-- =============================================

USE [SEGURIDAD]
GO

-- Insertar configuraciones para todos los módulos (notificaciones DESACTIVADAS por defecto)
INSERT INTO [dbo].[TrackingNotificacionConfig] 
    ([Modulo], [Accion], [NotificarWhatsApp], [Activo])
VALUES 
    -- Dashboard IA (YA configurado anteriormente, pero lo dejamos para referencia)
    -- ('Dashboard IA', NULL, 1, 1),  -- ACTIVO - Notifica todo
    
    -- Utilidades
    ('Utilidades', NULL, 0, 1),                          -- ❌ No notifica nada de Utilidades
    ('Utilidades', 'Acceso', 0, 1),                      -- ❌ No notifica acceso
    ('Utilidades', 'Consultar', 0, 1),                   -- ❌ No notifica consultas
    ('Utilidades', 'Exportar Excel', 0, 1),              -- ❌ No notifica exportaciones
    
    -- Equipos Vendidos
    ('Equipos Vendidos', NULL, 0, 1),                    -- ❌ No notifica nada de Equipos Vendidos
    ('Equipos Vendidos', 'Acceso', 0, 1),                -- ❌ No notifica acceso
    ('Equipos Vendidos', 'Consultar', 0, 1),             -- ❌ No notifica consultas
    ('Equipos Vendidos', 'Filtrar', 0, 1),               -- ❌ No notifica filtros
    ('Equipos Vendidos', 'Exportar Excel', 0, 1),        -- ❌ No notifica exportaciones
    
    -- Equipos Disponibles
    ('Equipos Disponibles', NULL, 0, 1),                 -- ❌ No notifica nada de Equipos Disponibles
    ('Equipos Disponibles', 'Acceso', 0, 1),             -- ❌ No notifica acceso
    ('Equipos Disponibles', 'Consultar', 0, 1),          -- ❌ No notifica consultas
    ('Equipos Disponibles', 'Buscar IMEI', 0, 1),        -- ❌ No notifica búsquedas por IMEI
    ('Equipos Disponibles', 'Filtrar', 0, 1),            -- ❌ No notifica filtros
    ('Equipos Disponibles', 'Exportar Excel', 0, 1),     -- ❌ No notifica exportaciones
    
    -- Consulta de Facturas
    ('Facturas', NULL, 0, 1),                            -- ❌ No notifica nada de Facturas
    ('Facturas', 'Acceso', 0, 1),                        -- ❌ No notifica acceso
    ('Facturas', 'Consultar', 0, 1),                     -- ❌ No notifica consultas
    ('Facturas', 'Ver Detalle', 0, 1),                   -- ❌ No notifica ver detalles
    ('Facturas', 'Crear', 0, 1),                         -- ❌ No notifica creación (cambiar a 1 si quieres notificar)
    ('Facturas', 'Anular', 0, 1),                        -- ❌ No notifica anulaciones (cambiar a 1 si quieres notificar)
    ('Facturas', 'Exportar Excel', 0, 1),                -- ❌ No notifica exportaciones
    
    -- Consulta de Accesorios
    ('Accesorios', NULL, 0, 1),                          -- ❌ No notifica nada de Accesorios
    ('Accesorios', 'Acceso', 0, 1),                      -- ❌ No notifica acceso
    ('Accesorios', 'Consultar', 0, 1),                   -- ❌ No notifica consultas
    ('Accesorios', 'Filtrar', 0, 1),                     -- ❌ No notifica filtros
    ('Accesorios', 'Exportar Excel', 0, 1),              -- ❌ No notifica exportaciones
    
    -- Registro de Equipos
    ('Registro Equipos', NULL, 0, 1),                    -- ❌ No notifica nada de Registro de Equipos
    ('Registro Equipos', 'Acceso', 0, 1),                -- ❌ No notifica acceso
    ('Registro Equipos', 'Crear', 0, 1),                 -- ❌ No notifica creación (cambiar a 1 si quieres notificar)
    ('Registro Equipos', 'Editar', 0, 1),                -- ❌ No notifica edición (cambiar a 1 si quieres notificar)
    ('Registro Equipos', 'Eliminar', 0, 1),              -- ❌ No notifica eliminación (cambiar a 1 si quieres notificar)
    ('Registro Equipos', 'Guardar', 0, 1),               -- ❌ No notifica guardado
    
    -- Registro de Accesorios
    ('Registro Accesorios', NULL, 0, 1),                 -- ❌ No notifica nada de Registro de Accesorios
    ('Registro Accesorios', 'Acceso', 0, 1),             -- ❌ No notifica acceso
    ('Registro Accesorios', 'Crear', 0, 1),              -- ❌ No notifica creación (cambiar a 1 si quieres notificar)
    ('Registro Accesorios', 'Editar', 0, 1),             -- ❌ No notifica edición (cambiar a 1 si quieres notificar)
    ('Registro Accesorios', 'Eliminar', 0, 1),           -- ❌ No notifica eliminación (cambiar a 1 si quieres notificar)
    ('Registro Accesorios', 'Guardar', 0, 1)             -- ❌ No notifica guardado
GO

-- Consulta para ver todas las configuraciones
SELECT 
    IdConfig,
    ISNULL(Modulo, '(Todos)') AS Modulo,
    ISNULL(Accion, '(Todas)') AS Accion,
    CASE WHEN NotificarWhatsApp = 1 THEN '✅ SÍ NOTIFICA' ELSE '❌ NO NOTIFICA' END AS WhatsApp,
    CASE WHEN Activo = 1 THEN '✅ Activa' ELSE '❌ Inactiva' END AS Estado,
    FechaCreacion
FROM [dbo].[TrackingNotificacionConfig]
WHERE Activo = 1
ORDER BY Modulo, Accion
GO

PRINT ''
PRINT '✅ Configuraciones insertadas correctamente'
PRINT '📋 Total de módulos configurados: 7'
PRINT ''
PRINT '💡 PARA ACTIVAR NOTIFICACIONES, ejecuta:'
PRINT '   UPDATE TrackingNotificacionConfig'
PRINT '   SET NotificarWhatsApp = 1'
PRINT '   WHERE Modulo = ''NombreModulo'' AND Accion = ''NombreAccion'''
PRINT ''
PRINT '📌 Ejemplos útiles:'
PRINT '   -- Notificar cuando CREAN una factura'
PRINT '   UPDATE TrackingNotificacionConfig SET NotificarWhatsApp = 1 WHERE Modulo = ''Facturas'' AND Accion = ''Crear'''
PRINT ''
PRINT '   -- Notificar cuando ELIMINAN un equipo'
PRINT '   UPDATE TrackingNotificacionConfig SET NotificarWhatsApp = 1 WHERE Modulo = ''Registro Equipos'' AND Accion = ''Eliminar'''
PRINT ''
PRINT '   -- Notificar TODO el módulo de Facturas'
PRINT '   UPDATE TrackingNotificacionConfig SET NotificarWhatsApp = 1 WHERE Modulo = ''Facturas'''
GO
