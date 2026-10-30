-- =============================================
-- Uso del sistema: pantallas abiertas, botones pulsados y usuarios conectados
-- Base de datos: SEGURIDAD (compartida por todas las empresas)
-- Idempotente: se puede ejecutar mas de una vez sin danar nada.
-- Solo crea tablas nuevas; no modifica TrackingLogs ni ninguna otra tabla.
-- =============================================
USE [SEGURIDAD]
GO

-- Una fila por pantalla abierta (Tipo = 'P') o boton pulsado (Tipo = 'B').
-- FechaHora en hora de Republica Dominicana (igual que TrackingLogs).
IF OBJECT_ID(N'dbo.UsoSistema', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsoSistema (
        IdUso     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsoSistema PRIMARY KEY,
        Usuario   NVARCHAR(100) NOT NULL,
        Empresa   NVARCHAR(150) NULL,
        Tipo      CHAR(1)       NOT NULL,        -- 'P' = pantalla, 'B' = boton
        Ruta      NVARCHAR(200) NOT NULL,        -- ruta normalizada, ej. /home/equipo/registro
        Detalle   NVARCHAR(150) NULL,            -- texto del boton (solo Tipo = 'B')
        FechaHora DATETIME      NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UsoSistema_Fecha' AND object_id = OBJECT_ID(N'dbo.UsoSistema'))
    CREATE NONCLUSTERED INDEX IX_UsoSistema_Fecha
        ON dbo.UsoSistema (FechaHora)
        INCLUDE (Tipo, Ruta, Detalle, Usuario, Empresa);
GO

-- Una fila por sesion de usuario; el front la mantiene viva con un latido cada 2 minutos.
IF OBJECT_ID(N'dbo.SesionesUsuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SesionesUsuario (
        SesionId        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SesionesUsuario PRIMARY KEY,
        Usuario         NVARCHAR(100)  NOT NULL,
        Empresa         NVARCHAR(150)  NULL,
        Inicio          DATETIME       NOT NULL,
        UltimaActividad DATETIME       NOT NULL,
        Ruta            NVARCHAR(200)  NULL,
        DireccionIP     NVARCHAR(50)   NULL,
        UserAgent       NVARCHAR(500)  NULL,
        Activa          BIT            NOT NULL CONSTRAINT DF_SesionesUsuario_Activa DEFAULT (1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SesionesUsuario_Actividad' AND object_id = OBJECT_ID(N'dbo.SesionesUsuario'))
    CREATE NONCLUSTERED INDEX IX_SesionesUsuario_Actividad
        ON dbo.SesionesUsuario (Activa, UltimaActividad);
GO

SELECT 'UsoSistema' AS Tabla, COUNT(*) AS Filas FROM dbo.UsoSistema
UNION ALL
SELECT 'SesionesUsuario', COUNT(*) FROM dbo.SesionesUsuario;
GO
