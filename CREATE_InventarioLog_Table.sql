-- Script para crear la tabla InventarioLog
-- Esta tabla registra todas las acciones realizadas durante el proceso de inventario

USE [MVSystem]
GO

-- Verificar si la tabla ya existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InventarioLog')
BEGIN
    CREATE TABLE [dbo].[InventarioLog] (
        [ID_Log] [int] IDENTITY(1,1) NOT NULL,
        [ID_Sesion] [int] NOT NULL,
        [Accion] [varchar](50) NOT NULL, -- 'INICIO', 'PAUSA', 'REANUDACION', 'ESCANEO', 'FINALIZACION', 'CANCELACION'
        [Detalle] [varchar](500) NULL,
        [Usuario] [varchar](50) NOT NULL,
        [Fecha_Accion] [datetime] NOT NULL DEFAULT GETDATE(),
        [ID_Equipo] [int] NULL, -- Para acciones relacionadas con equipos específicos
        [IMEI] [varchar](20) NULL, -- Para registrar el IMEI escaneado
        [Datos_Adicionales] [varchar](1000) NULL, -- JSON u otros datos adicionales
        [IP_Cliente] [varchar](45) NULL,
        
        CONSTRAINT [PK_InventarioLog] PRIMARY KEY CLUSTERED ([ID_Log] ASC),
        CONSTRAINT [FK_InventarioLog_InventarioSesion] FOREIGN KEY ([ID_Sesion]) 
            REFERENCES [dbo].[InventarioSesion] ([ID_Sesion])
            ON DELETE CASCADE
    )
    
    -- Crear índices para mejorar el rendimiento
    CREATE NONCLUSTERED INDEX [IX_InventarioLog_ID_Sesion] ON [dbo].[InventarioLog] ([ID_Sesion] ASC)
    CREATE NONCLUSTERED INDEX [IX_InventarioLog_Usuario] ON [dbo].[InventarioLog] ([Usuario] ASC)
    CREATE NONCLUSTERED INDEX [IX_InventarioLog_Fecha_Accion] ON [dbo].[InventarioLog] ([Fecha_Accion] ASC)
    CREATE NONCLUSTERED INDEX [IX_InventarioLog_Accion] ON [dbo].[InventarioLog] ([Accion] ASC)
    
    PRINT 'Tabla InventarioLog creada exitosamente'
END
ELSE
BEGIN
    PRINT 'La tabla InventarioLog ya existe'
END

-- Verificar la estructura de la tabla creada
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'InventarioLog'
ORDER BY ORDINAL_POSITION

GO

-- Script adicional para insertar algunos registros de prueba (opcional)
/*
-- Ejemplos de registros que se pueden insertar
INSERT INTO InventarioLog (ID_Sesion, Accion, Detalle, Usuario, ID_Equipo, IMEI) 
VALUES 
(1, 'INICIO', 'Inventario iniciado en Almacén Principal', 'mvpruebas', NULL, NULL),
(1, 'ESCANEO', 'Equipo escaneado correctamente', 'mvpruebas', 123, '123456789012345'),
(1, 'PAUSA', 'Inventario pausado por usuario', 'mvpruebas', NULL, NULL)
*/