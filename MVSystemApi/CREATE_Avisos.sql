-- =============================================
-- Avisos y novedades del sistema (mensajes que se muestran a los usuarios)
-- Base de datos: SEGURIDAD (compartida por todas las empresas)
-- Idempotente: se puede ejecutar mas de una vez sin danar nada.
-- Solo crea tablas nuevas; no modifica ninguna tabla existente.
-- =============================================
USE [SEGURIDAD]
GO

-- Un aviso o novedad. Fechas en hora de Republica Dominicana (UTC-4).
IF OBJECT_ID(N'dbo.Avisos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Avisos (
        IdAviso       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Avisos PRIMARY KEY,
        Tipo          VARCHAR(10)   NOT NULL,          -- 'NOVEDAD' o 'AVISO'
        Prioridad     CHAR(1)       NOT NULL CONSTRAINT DF_Avisos_Prioridad DEFAULT ('N'),  -- 'N' normal, 'U' urgente
        Titulo        NVARCHAR(150) NOT NULL,
        Mensaje       NVARCHAR(1000) NOT NULL,
        Donde         NVARCHAR(150) NULL,              -- en que parte del sistema esta (opcional)
        Destino       VARCHAR(10)   NOT NULL CONSTRAINT DF_Avisos_Destino DEFAULT ('TODOS'), -- 'TODOS', 'EMPRESA' o 'USUARIOS'
        DestinoValor  NVARCHAR(1000) NULL,             -- nombre de la empresa, o logins separados por coma
        Permiso       NVARCHAR(100) NULL,              -- si se indica, solo lo ven quienes tengan ese permiso
        FechaInicio   DATETIME      NOT NULL,          -- desde cuando se muestra
        FechaVence    DATETIME      NULL,              -- hasta cuando se muestra (NULL = sin vencimiento)
        Activo        BIT           NOT NULL CONSTRAINT DF_Avisos_Activo DEFAULT (1),
        CreadoPor     NVARCHAR(100) NOT NULL,
        FechaCreacion DATETIME      NOT NULL
    );
END
GO

-- Quien ya vio cada aviso (para no repetirlo y para saber quien lo vio)
IF OBJECT_ID(N'dbo.AvisosVistos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AvisosVistos (
        IdAviso    INT           NOT NULL,
        Usuario    NVARCHAR(100) NOT NULL,
        FechaVisto DATETIME      NOT NULL,
        CONSTRAINT PK_AvisosVistos PRIMARY KEY (IdAviso, Usuario),
        CONSTRAINT FK_AvisosVistos_Avisos FOREIGN KEY (IdAviso) REFERENCES dbo.Avisos (IdAviso)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Avisos_Vigencia' AND object_id = OBJECT_ID(N'dbo.Avisos'))
    CREATE NONCLUSTERED INDEX IX_Avisos_Vigencia
        ON dbo.Avisos (Activo, FechaInicio, FechaVence);
GO

SELECT 'Avisos' AS Tabla, COUNT(*) AS Filas FROM dbo.Avisos
UNION ALL
SELECT 'AvisosVistos', COUNT(*) FROM dbo.AvisosVistos;
GO
