-- ============================================================
-- Catalogo de RNC de la DGII (Parte 1)
-- Ejecutar SOLO en la BD SEGURIDAD. Idempotente (se puede correr mas de una vez).
--   dbo.RNC_DGII             : listado vigente (una fila por RNC o cedula)
--   dbo.RNC_DGII_Carga       : tabla de paso; el cargador la llena y se vacia en cada carga
--   dbo.RNC_DGII_Aplicar_Carga : pasa Carga -> RNC_DGII (inserta nuevos, actualiza cambios, no borra)
--   dbo.RNC_DGII_Consulta    : busca un RNC
-- No toca las tablas RNC_cata ni los SP RNC_Consulta / RNC_Guarda de las bases de empresa.
-- ============================================================
USE [SEGURIDAD]
GO

IF OBJECT_ID(N'dbo.RNC_DGII', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RNC_DGII (
        RNC                 VARCHAR(11)  NOT NULL CONSTRAINT PK_RNC_DGII PRIMARY KEY,
        Razon_Social        VARCHAR(100) NOT NULL,
        Nombre_Comercial    VARCHAR(100) NULL,
        Estado              VARCHAR(15)  NOT NULL,   -- texto de la DGII: ACTIVO, SUSPENDIDO, DADO DE BAJA...
        Fecha_Actualizacion DATETIME     NOT NULL CONSTRAINT DF_RNC_DGII_Fecha DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.RNC_DGII_Carga', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RNC_DGII_Carga (
        RNC              VARCHAR(11)  NOT NULL,
        Razon_Social     VARCHAR(100) NOT NULL,
        Nombre_Comercial VARCHAR(100) NULL,
        Estado           VARCHAR(15)  NOT NULL
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.RNC_DGII_Aplicar_Carga
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Acciones TABLE (Accion NVARCHAR(10));

    BEGIN TRANSACTION;

    MERGE dbo.RNC_DGII AS destino
    USING dbo.RNC_DGII_Carga AS origen
        ON destino.RNC = origen.RNC
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (RNC, Razon_Social, Nombre_Comercial, Estado, Fecha_Actualizacion)
        VALUES (origen.RNC, origen.Razon_Social, origen.Nombre_Comercial, origen.Estado, GETDATE())
    WHEN MATCHED AND (   destino.Razon_Social <> origen.Razon_Social
                      OR destino.Estado <> origen.Estado
                      OR ISNULL(destino.Nombre_Comercial, '') <> ISNULL(origen.Nombre_Comercial, '')) THEN
        UPDATE SET Razon_Social        = origen.Razon_Social,
                   Nombre_Comercial    = origen.Nombre_Comercial,
                   Estado              = origen.Estado,
                   Fecha_Actualizacion = GETDATE()
    OUTPUT $action INTO @Acciones;      -- los RNC que ya no vengan en el archivo NO se borran

    DECLARE @Insertados INT = (SELECT COUNT(*) FROM @Acciones WHERE Accion = 'INSERT');
    DECLARE @Actualizados INT = (SELECT COUNT(*) FROM @Acciones WHERE Accion = 'UPDATE');
    DECLARE @EnCarga INT = (SELECT COUNT(*) FROM dbo.RNC_DGII_Carga);

    TRUNCATE TABLE dbo.RNC_DGII_Carga;

    COMMIT TRANSACTION;

    SELECT @Insertados AS Insertados,
           @Actualizados AS Actualizados,
           @EnCarga - @Insertados - @Actualizados AS SinCambio;
END
GO

CREATE OR ALTER PROCEDURE dbo.RNC_DGII_Consulta
    @RNC VARCHAR(11)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT RNC, Razon_Social, Nombre_Comercial, Estado
    FROM dbo.RNC_DGII
    WHERE RNC = @RNC;
END
GO

SELECT 'RNC_DGII' AS Tabla, COUNT(*) AS Filas FROM dbo.RNC_DGII
UNION ALL
SELECT 'RNC_DGII_Carga', COUNT(*) FROM dbo.RNC_DGII_Carga;
GO
