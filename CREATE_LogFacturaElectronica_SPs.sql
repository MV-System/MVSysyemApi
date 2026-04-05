-- =====================================================
-- FACTURACIÓN ELECTRÓNICA (DIGI) - SCRIPT DE INSTALACIÓN
-- =====================================================
-- Fecha: April 5, 2026
-- Descripción: Tabla LogFacturaElectronica y Stored Procedures para auditar envíos a DGI
-- =====================================================

USE [MVSystem]; -- Cambiar por el nombre de tu BD de producción si es diferente
GO

-- =====================================================
-- 1. CREAR TABLA LogFacturaElectronica
-- =====================================================

IF OBJECT_ID('dbo.LogFacturaElectronica', 'U') IS NOT NULL 
BEGIN
    DROP TABLE dbo.LogFacturaElectronica;
    PRINT '✓ Tabla LogFacturaElectronica eliminada (existía anteriormente)';
END
GO

CREATE TABLE dbo.LogFacturaElectronica
(
    ID_Log INT PRIMARY KEY IDENTITY(1,1),
    Numero_Factura INT NOT NULL,           -- Referencia a Facturas.Numero_Factura
    Literal CHAR(1) NOT NULL,              -- Referencia a Facturas.Literal (identifica sucursal)
    DocumentoId_EasyConexion INT,          -- ID asignado por EasyConexión API
    VoucherNumber_DGII VARCHAR(50),        -- Número de comprobante en DGI (ej: E3200000003)
    Estado VARCHAR(50) NOT NULL,           -- PEND, ENVIADA, ERROR, CONFIRMADA
    Respuesta_JSON NVARCHAR(MAX),          -- Response completo de EasyConexión (para auditoría)
    Fecha_Envio DATETIME NOT NULL,         -- Fecha del envío
    Fecha_Confirmacion DATETIME NULL,      -- Fecha de confirmación en DGII
    Error_Descripcion NVARCHAR(MAX) NULL,  -- Descripción de error si falló
    Usuario VARCHAR(50),                   -- Usuario que generó la factura
    Fecha_Registro DATETIME DEFAULT GETDATE() -- Fecha de registro del log
);

-- Crear índices
CREATE UNIQUE INDEX IX_LogFacturaElectronica_UnicoEnvio 
ON dbo.LogFacturaElectronica(Numero_Factura, Literal);

CREATE INDEX IX_LogFacturaElectronica_Estado 
ON dbo.LogFacturaElectronica(Estado);

CREATE INDEX IX_LogFacturaElectronica_Fecha 
ON dbo.LogFacturaElectronica(Fecha_Envio DESC);

PRINT '✓ Tabla LogFacturaElectronica creada exitosamente con índices';
GO

-- =====================================================
-- 2. STORED PROCEDURE: Proc_LogFacturaElectronica_Insert
-- =====================================================
-- Descripción: Inserta un nuevo registro de log
-- Parámetros:
--   @Numero_Factura: Número de la factura
--   @Literal: Literal de la sucursal (A, B, C, etc.)
--   @DocumentoId_EasyConexion: ID del documento en EasyConexión
--   @VoucherNumber_DGII: Número de comprobante
--   @Estado: Estado del envío (PEND, ENVIADA, ERROR, CONFIRMADA)
--   @Respuesta_JSON: Response JSON completo
--   @Fecha_Envio: Fecha del envío
--   @Error_Descripcion: Descripción de error (si aplica)
--   @Usuario: Usuario del sistema

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_Insert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Proc_LogFacturaElectronica_Insert;
GO

CREATE PROCEDURE dbo.Proc_LogFacturaElectronica_Insert
    @Numero_Factura INT,
    @Literal CHAR(1) = 'A',
    @DocumentoId_EasyConexion INT = NULL,
    @VoucherNumber_DGII VARCHAR(50) = NULL,
    @Estado VARCHAR(50) = 'PEND',
    @Respuesta_JSON NVARCHAR(MAX) = NULL,
    @Fecha_Envio DATETIME,
    @Error_Descripcion NVARCHAR(MAX) = NULL,
    @Usuario VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        INSERT INTO dbo.LogFacturaElectronica 
            (Numero_Factura, Literal, DocumentoId_EasyConexion, VoucherNumber_DGII, 
             Estado, Respuesta_JSON, Fecha_Envio, Error_Descripcion, Usuario)
        VALUES 
            (@Numero_Factura, @Literal, @DocumentoId_EasyConexion, @VoucherNumber_DGII,
             @Estado, @Respuesta_JSON, @Fecha_Envio, @Error_Descripcion, @Usuario);
        
        PRINT '✓ Log de factura insertado exitosamente (Factura: ' + CAST(@Numero_Factura AS VARCHAR) + ')';
        
        RETURN 1; -- Éxito
    END TRY
    BEGIN CATCH
        PRINT '✗ Error en Proc_LogFacturaElectronica_Insert: ' + ERROR_MESSAGE();
        RETURN -1; -- Error
    END CATCH
END
GO

-- =====================================================
-- 3. STORED PROCEDURE: Proc_LogFacturaElectronica_Update
-- =====================================================
-- Descripción: Actualiza un registro de log existente
-- Parámetros: Similar al Insert, pero updatea registro existente

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_Update', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Proc_LogFacturaElectronica_Update;
GO

CREATE PROCEDURE dbo.Proc_LogFacturaElectronica_Update
    @Numero_Factura INT,
    @Literal CHAR(1) = 'A',
    @DocumentoId_EasyConexion INT = NULL,
    @VoucherNumber_DGII VARCHAR(50) = NULL,
    @Estado VARCHAR(50),
    @Respuesta_JSON NVARCHAR(MAX) = NULL,
    @Fecha_Confirmacion DATETIME = NULL,
    @Error_Descripcion NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        UPDATE dbo.LogFacturaElectronica
        SET 
            DocumentoId_EasyConexion = @DocumentoId_EasyConexion,
            VoucherNumber_DGII = @VoucherNumber_DGII,
            Estado = @Estado,
            Respuesta_JSON = @Respuesta_JSON,
            Fecha_Confirmacion = @Fecha_Confirmacion,
            Error_Descripcion = @Error_Descripcion
        WHERE 
            Numero_Factura = @Numero_Factura 
            AND Literal = @Literal;
        
        PRINT '✓ Log de factura actualizado exitosamente';
        
        RETURN 1; -- Éxito
    END TRY
    BEGIN CATCH
        PRINT '✗ Error en Proc_LogFacturaElectronica_Update: ' + ERROR_MESSAGE();
        RETURN -1; -- Error
    END CATCH
END
GO

-- =====================================================
-- 4. STORED PROCEDURE: Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral
-- =====================================================
-- Descripción: Obtiene el log de una factura específica
-- Parámetros:
--   @Numero_Factura: Número de la factura
--   @Literal: Literal de la sucursal

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral;
GO

CREATE PROCEDURE dbo.Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral
    @Numero_Factura INT,
    @Literal CHAR(1) = 'A'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT TOP 1
            ID_Log,
            Numero_Factura,
            Literal,
            DocumentoId_EasyConexion,
            VoucherNumber_DGII,
            Estado,
            Respuesta_JSON,
            Fecha_Envio,
            Fecha_Confirmacion,
            Error_Descripcion,
            Usuario,
            Fecha_Registro
        FROM dbo.LogFacturaElectronica
        WHERE 
            Numero_Factura = @Numero_Factura
            AND Literal = @Literal
        ORDER BY 
            ID_Log DESC;
        
        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        PRINT '✗ Error en Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral: ' + ERROR_MESSAGE();
        RETURN -1; -- Error
    END CATCH
END
GO

-- =====================================================
-- 5. STORED PROCEDURE: Proc_LogFacturaElectronica_GetPendientes
-- =====================================================
-- Descripción: Obtiene todas las facturas con estado PEND
-- Parámetros: Ninguno

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_GetPendientes', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Proc_LogFacturaElectronica_GetPendientes;
GO

CREATE PROCEDURE dbo.Proc_LogFacturaElectronica_GetPendientes
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT
            ID_Log,
            Numero_Factura,
            Literal,
            DocumentoId_EasyConexion,
            VoucherNumber_DGII,
            Estado,
            Respuesta_JSON,
            Fecha_Envio,
            Fecha_Confirmacion,
            Error_Descripcion,
            Usuario,
            Fecha_Registro
        FROM dbo.LogFacturaElectronica
        WHERE Estado = 'PEND'
        ORDER BY Fecha_Envio ASC;
        
        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        PRINT '✗ Error en Proc_LogFacturaElectronica_GetPendientes: ' + ERROR_MESSAGE();
        RETURN -1; -- Error
    END CATCH
END
GO

-- =====================================================
-- 6. VERIFICACIÓN FINAL
-- =====================================================

PRINT '';
PRINT '==================================================';
PRINT 'INSTALACIÓN COMPLETADA';
PRINT '==================================================';
PRINT '';
PRINT 'Tabla creada: dbo.LogFacturaElectronica';
PRINT 'Procedimientos creados:';
PRINT '  - Proc_LogFacturaElectronica_Insert';
PRINT '  - Proc_LogFacturaElectronica_Update';
PRINT '  - Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral';
PRINT '  - Proc_LogFacturaElectronica_GetPendientes';
PRINT '';

-- Verificar que todo se creó correctamente
IF OBJECT_ID('dbo.LogFacturaElectronica', 'U') IS NOT NULL
    PRINT '✓ Tabla LogFacturaElectronica existe';
ELSE
    PRINT '✗ ERROR: Tabla LogFacturaElectronica NO se creó';

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_Insert', 'P') IS NOT NULL
    PRINT '✓ Procedimiento Proc_LogFacturaElectronica_Insert existe';
ELSE
    PRINT '✗ ERROR: Procedimiento Proc_LogFacturaElectronica_Insert NO se creó';

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_Update', 'P') IS NOT NULL
    PRINT '✓ Procedimiento Proc_LogFacturaElectronica_Update existe';
ELSE
    PRINT '✗ ERROR: Procedimiento Proc_LogFacturaElectronica_Update NO se creó';

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral', 'P') IS NOT NULL
    PRINT '✓ Procedimiento Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral existe';
ELSE
    PRINT '✗ ERROR: Procedimiento Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral NO se creó';

IF OBJECT_ID('dbo.Proc_LogFacturaElectronica_GetPendientes', 'P') IS NOT NULL
    PRINT '✓ Procedimiento Proc_LogFacturaElectronica_GetPendientes existe';
ELSE
    PRINT '✗ ERROR: Procedimiento Proc_LogFacturaElectronica_GetPendientes NO se creó';

PRINT '';
PRINT '==================================================';
GO
