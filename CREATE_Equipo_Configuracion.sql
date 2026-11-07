-- ============================================================
-- Feature: Módulo de Laptops - Fase 2, Tarea 3
-- Propósito: Configuración variable (RAM / almacenamiento) por equipo.
--            - Tabla dbo.Equipo_Configuracion (1 fila por laptop, por Imei)
--            - SP Equipo_Configuracion_Guardar (upsert, valida laptop)
--            - SP Equipo_Configuracion_Consultar
--            - Función dbo.fn_Equipo_Configuracion_Texto
--            Script idempotente.
-- Requiere: CREATE_Laptops_Fase1_Grupos.sql ejecutado.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================

-- ============================================================
-- 1. Tabla
-- ============================================================
IF OBJECT_ID('dbo.Equipo_Configuracion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipo_Configuracion (
        ID_Configuracion    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Imei                VARCHAR(30)  NOT NULL,
        Ram_GB              INT          NULL,
        Ram_Tipo            VARCHAR(30)  NULL,
        Almacenamiento_GB   INT          NULL,
        Almacenamiento_Tipo VARCHAR(30)  NULL,
        Notas               VARCHAR(250) NULL,
        Usuario             VARCHAR(60)  NOT NULL,
        Fecha_Registro      DATETIME     NOT NULL DEFAULT GETDATE(),
        Usuario_Modifica    VARCHAR(60)  NULL,
        Fecha_Modifica      DATETIME     NULL,
        CONSTRAINT UQ_Equipo_Configuracion_Imei UNIQUE (Imei)
    );
END
GO

-- ============================================================
-- 2. Función: texto de la configuración, ej. '12GB DDR4 / 256GB SSD SATA'
--    Devuelve NULL si el equipo no tiene configuración.
-- ============================================================
CREATE OR ALTER FUNCTION dbo.fn_Equipo_Configuracion_Texto (@Imei VARCHAR(30))
RETURNS VARCHAR(100)
AS
BEGIN
    DECLARE @Texto VARCHAR(100);

    SELECT @Texto =
        NULLIF(
            LTRIM(RTRIM(
                ISNULL(CASE WHEN C.Ram_GB IS NOT NULL
                            THEN CAST(C.Ram_GB AS VARCHAR(10)) + 'GB' + ISNULL(' ' + C.Ram_Tipo, '')
                       END, '')
                + CASE WHEN C.Ram_GB IS NOT NULL AND C.Almacenamiento_GB IS NOT NULL THEN ' / ' ELSE '' END
                + ISNULL(CASE WHEN C.Almacenamiento_GB IS NOT NULL
                              THEN CAST(C.Almacenamiento_GB AS VARCHAR(10)) + 'GB' + ISNULL(' ' + C.Almacenamiento_Tipo, '')
                         END, '')
            )), '')
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;

    RETURN @Texto;
END
GO

-- ============================================================
-- 3. SP: Guardar (upsert por Imei)
--    Valida que el equipo exista y que su modelo sea del grupo Laptops.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Guardar
    @Imei                VARCHAR(30),
    @Ram_GB              INT = NULL,
    @Ram_Tipo            VARCHAR(30) = NULL,
    @Almacenamiento_GB   INT = NULL,
    @Almacenamiento_Tipo VARCHAR(30) = NULL,
    @Notas               VARCHAR(250) = NULL,
    @Usuario             VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    -- R2: se obtiene el ID del grupo por descripción una sola vez y se compara por ID.
    DECLARE @ID_Grupo_Laptops INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');
    DECLARE @ID_Modelo INT;

    IF @ID_Grupo_Laptops IS NULL
    BEGIN
        RAISERROR('No existe el grupo Laptops. Ejecute CREATE_Laptops_Fase1_Grupos.sql', 16, 1);
        RETURN;
    END

    SELECT TOP 1 @ID_Modelo = ID_Modelo FROM dbo.Equipos WHERE Imei = @Imei;

    IF @ID_Modelo IS NULL
    BEGIN
        RAISERROR('El equipo con serial/IMEI %s no existe.', 16, 1, @Imei);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Modelos WHERE ID_Modelo = @ID_Modelo AND ID_Grupo = @ID_Grupo_Laptops)
    BEGIN
        RAISERROR('El equipo %s no es una laptop (su modelo no pertenece al grupo Laptops).', 16, 1, @Imei);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion WHERE Imei = @Imei)
    BEGIN
        UPDATE dbo.Equipo_Configuracion
        SET Ram_GB              = @Ram_GB,
            Ram_Tipo            = @Ram_Tipo,
            Almacenamiento_GB   = @Almacenamiento_GB,
            Almacenamiento_Tipo = @Almacenamiento_Tipo,
            Notas               = @Notas,
            Usuario_Modifica    = @Usuario,
            Fecha_Modifica      = GETDATE()
        WHERE Imei = @Imei;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Equipo_Configuracion
            (Imei, Ram_GB, Ram_Tipo, Almacenamiento_GB, Almacenamiento_Tipo, Notas, Usuario, Fecha_Registro)
        VALUES
            (@Imei, @Ram_GB, @Ram_Tipo, @Almacenamiento_GB, @Almacenamiento_Tipo, @Notas, @Usuario, GETDATE());
    END

    SELECT C.ID_Configuracion, C.Imei, C.Ram_GB, C.Ram_Tipo, C.Almacenamiento_GB, C.Almacenamiento_Tipo,
           C.Notas, dbo.fn_Equipo_Configuracion_Texto(C.Imei) AS Configuracion,
           Mensaje = 'Configuracion guardada satisfactoriamente'
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;
END
GO

-- ============================================================
-- 4. SP: Consultar por Imei
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Consultar
    @Imei VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Configuracion, C.Imei, C.Ram_GB, C.Ram_Tipo, C.Almacenamiento_GB, C.Almacenamiento_Tipo,
           C.Notas, dbo.fn_Equipo_Configuracion_Texto(C.Imei) AS Configuracion,
           C.Usuario, C.Fecha_Registro, C.Usuario_Modifica, C.Fecha_Modifica
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;
END
GO
