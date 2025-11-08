-- =============================================
-- Stored Procedures para módulo de Inventario
-- Autor: Sistema MVSystem
-- Fecha: 2025-11-08
-- =============================================

-- Crear tabla InventarioLog si no existe
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InventarioLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InventarioLog] (
        [ID_Log] [bigint] IDENTITY(1,1) NOT NULL,
        [ID_Sesion] [bigint] NOT NULL,
        [Accion] [varchar](50) NOT NULL,
        [Descripcion] [varchar](500) NULL,
        [Usuario] [varchar](100) NOT NULL,
        [Fecha] [datetime] NOT NULL DEFAULT DATEADD(HOUR, -4, GETDATE()),
        [Datos_Adicionales] [varchar](1000) NULL,
        CONSTRAINT [PK_InventarioLog] PRIMARY KEY CLUSTERED ([ID_Log] ASC)
    )
    
    -- Índices
    CREATE INDEX [IX_InventarioLog_Sesion] ON [dbo].[InventarioLog] ([ID_Sesion])
    CREATE INDEX [IX_InventarioLog_Fecha] ON [dbo].[InventarioLog] ([Fecha])
    
    PRINT 'Tabla InventarioLog creada correctamente'
END
GO

-- Verificar y agregar columna Fecha_Reanudacion si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InventarioSesion]') AND name = 'Fecha_Reanudacion')
BEGIN
    ALTER TABLE [dbo].[InventarioSesion] 
    ADD [Fecha_Reanudacion] [datetime] NULL
    
    PRINT 'Columna Fecha_Reanudacion agregada a InventarioSesion'
END
GO

-- =============================================
-- SP_Inventario_Reanudar
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SP_Inventario_Reanudar]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[SP_Inventario_Reanudar]
GO

CREATE PROCEDURE [dbo].[SP_Inventario_Reanudar]
    @ID_Sesion BIGINT,
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION
        
        -- Verificar que la sesión existe y está pausada
        IF NOT EXISTS (SELECT 1 FROM InventarioSesion WHERE ID_Sesion = @ID_Sesion AND Estado = 'PAUSADO')
        BEGIN
            RAISERROR('La sesión no existe o no está pausada', 16, 1)
            RETURN
        END
        
        -- Actualizar estado a EN_PROCESO
        UPDATE InventarioSesion 
        SET Estado = 'EN_PROCESO',
            Fecha_Reanudacion = DATEADD(HOUR, -4, GETDATE())
        WHERE ID_Sesion = @ID_Sesion
        
        -- Registrar en log
        INSERT INTO InventarioLog (ID_Sesion, Accion, Descripcion, Usuario)
        VALUES (@ID_Sesion, 'REANUDAR', 'Inventario reanudado', @Usuario)
        
        COMMIT TRANSACTION
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
        DECLARE @ErrorState INT = ERROR_STATE()
        
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
    END CATCH
END
GO

-- =============================================
-- SP_Inventario_Pausar
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SP_Inventario_Pausar]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[SP_Inventario_Pausar]
GO

CREATE PROCEDURE [dbo].[SP_Inventario_Pausar]
    @ID_Sesion BIGINT,
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION
        
        -- Verificar que la sesión existe y está en proceso
        IF NOT EXISTS (SELECT 1 FROM InventarioSesion WHERE ID_Sesion = @ID_Sesion AND Estado = 'EN_PROCESO')
        BEGIN
            RAISERROR('La sesión no existe o no está en proceso', 16, 1)
            RETURN
        END
        
        -- Actualizar estado a PAUSADO
        UPDATE InventarioSesion 
        SET Estado = 'PAUSADO'
        WHERE ID_Sesion = @ID_Sesion
        
        -- Registrar en log
        INSERT INTO InventarioLog (ID_Sesion, Accion, Descripcion, Usuario)
        VALUES (@ID_Sesion, 'PAUSAR', 'Inventario pausado', @Usuario)
        
        COMMIT TRANSACTION
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
        DECLARE @ErrorState INT = ERROR_STATE()
        
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
    END CATCH
END
GO

-- Verificar que los SPs se crearon correctamente
SELECT 
    name AS 'Stored Procedure',
    create_date AS 'Fecha Creación'
FROM sys.objects 
WHERE type = 'P' 
AND name LIKE 'SP_Inventario_%'
ORDER BY name

PRINT 'Scripts de inventario ejecutados correctamente'
-- =============================================
-- SP_Inventario_EquiposFaltantes
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SP_Inventario_EquiposFaltantes]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[SP_Inventario_EquiposFaltantes]
GO

CREATE PROCEDURE [dbo].[SP_Inventario_EquiposFaltantes]
    @ID_Sesion BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    -- Devuelve los equipos que están en la sesión pero no han sido escaneados
    SELECT e.*
    FROM InventarioEquipos e
    WHERE e.ID_Sesion = @ID_Sesion
      AND e.Escaneado = 0
END
GO

-- =============================================
-- SP_Inventario_EquiposSobrantes
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SP_Inventario_EquiposSobrantes]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[SP_Inventario_EquiposSobrantes]
GO

CREATE PROCEDURE [dbo].[SP_Inventario_EquiposSobrantes]
    @ID_Sesion BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    -- Devuelve los equipos que fueron escaneados pero no estaban en la lista original
    SELECT l.*
    FROM InventarioLog l
    WHERE l.ID_Sesion = @ID_Sesion
      AND l.Accion = 'ESCANEO_SOBRANTE'
END
GO