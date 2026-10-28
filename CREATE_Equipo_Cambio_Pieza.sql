-- ============================================================
-- Feature: Módulo de Laptops - Fase 3, Tarea 8
-- Propósito: Historial de cambios de pieza (RAM / almacenamiento) de
--            una laptop y control de custodia de la pieza retirada.
--            Estado_Custodia: PENDIENTE | RECIBIDA | ENTREGADA_CLIENTE | NO_APLICA
--            Script idempotente.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================

IF OBJECT_ID('dbo.Equipo_Cambio_Pieza', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipo_Cambio_Pieza (
        ID_Cambio               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Imei                    VARCHAR(30)   NOT NULL,
        Tipo_Pieza              VARCHAR(20)   NOT NULL,   -- 'RAM' | 'ALMACENAMIENTO'
        ID_Accesorio_Instalado  BIGINT        NULL,
        Costo_Instalado         DECIMAL(18,2) NOT NULL DEFAULT 0,
        ID_Accesorio_Retirado   BIGINT        NULL,
        Costo_Retirado          DECIMAL(18,2) NOT NULL DEFAULT 0,
        Config_Anterior         VARCHAR(100)  NULL,       -- ej. '8GB / 256GB SSD SATA'
        Config_Nueva            VARCHAR(100)  NULL,
        Costo_Equipo_Anterior   DECIMAL(18,2) NOT NULL,
        Costo_Equipo_Nuevo      DECIMAL(18,2) NOT NULL,
        ID_Almacen              INT           NOT NULL,
        ID_PreFactura           INT           NULL,
        ID_Reparacion           INT           NULL,
        Tecnico                 VARCHAR(60)   NOT NULL,
        Estado_Custodia         VARCHAR(20)   NOT NULL,
        Usuario_Registro        VARCHAR(60)   NOT NULL,
        Fecha_Registro          DATETIME      NOT NULL DEFAULT GETDATE(),
        Usuario_Recepcion       VARCHAR(60)   NULL,
        Fecha_Recepcion         DATETIME      NULL,
        Nota_Recepcion          VARCHAR(250)  NULL,
        CONSTRAINT CK_Equipo_Cambio_Pieza_Tipo
            CHECK (Tipo_Pieza IN ('RAM', 'ALMACENAMIENTO')),
        CONSTRAINT CK_Equipo_Cambio_Pieza_Estado
            CHECK (Estado_Custodia IN ('PENDIENTE', 'RECIBIDA', 'ENTREGADA_CLIENTE', 'NO_APLICA'))
    );
END
GO

-- Índices
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipo_Cambio_Pieza_Imei' AND object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza'))
    CREATE INDEX IX_Equipo_Cambio_Pieza_Imei ON dbo.Equipo_Cambio_Pieza (Imei);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipo_Cambio_Pieza_Custodia_Tecnico' AND object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza'))
    CREATE INDEX IX_Equipo_Cambio_Pieza_Custodia_Tecnico ON dbo.Equipo_Cambio_Pieza (Estado_Custodia, Tecnico);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipo_Cambio_Pieza_Fecha_Registro' AND object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza'))
    CREATE INDEX IX_Equipo_Cambio_Pieza_Fecha_Registro ON dbo.Equipo_Cambio_Pieza (Fecha_Registro);
GO

-- Verificación
SELECT name AS Indice FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza') AND name IS NOT NULL;
GO
