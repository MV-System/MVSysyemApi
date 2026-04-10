-- =====================================================
-- FACTURACIÓN ELECTRÓNICA (DIGI) - SCRIPT DE INSTALACIÓN
-- =====================================================
-- Fecha: April 9, 2026
-- Descripción: 
--   1) Actualizar tabla TIPO_NFC (códigos de comprobante para facturación electrónica)
--   2) Crear tabla LogFacturaElectronica y Stored Procedures para auditar envíos a DGI
-- =====================================================
-- IMPORTANTE: Ejecutar en producción con precaución.
--   - La sección TIPO_NFC usa transacción (ROLLBACK automático si falla)
--   - Verificar el USE [BD] antes de ejecutar
-- =====================================================

USE [MVSystem]; -- Cambiar por el nombre de tu BD de producción si es diferente
GO

-- =====================================================
-- 0. ACTUALIZAR TABLA TIPO_NFC (códigos de comprobante)
-- =====================================================
-- Cambia NFCTipoNumero a los códigos nuevos (31, 32, 33, 34, 45)
-- NO modifica: NFCTipoEstructura, NFCTipoCantidad, NFCTipoSecuencia
-- Visible_Venta = 1 solo para NO REQUERIDO (aparece primero en dropdown)
-- Registros 11 y 14 se dejan intactos (no se usan actualmente)

PRINT '';
PRINT '==================================================';
PRINT 'PASO 0: Actualizar TIPO_NFC';
PRINT '==================================================';

-- Agregar columna Visible_Venta si no existe
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TIPO_NFC' AND COLUMN_NAME = 'Visible_Venta'
)
BEGIN
    ALTER TABLE [dbo].[TIPO_NFC] ADD Visible_Venta BIT NOT NULL DEFAULT 0;
    PRINT '  ✓ Columna Visible_Venta agregada a TIPO_NFC (default = 0)';
END
ELSE
BEGIN
    PRINT '  → Columna Visible_Venta ya existe en TIPO_NFC';
END

BEGIN TRANSACTION;
BEGIN TRY

    -- Verificar que los registros originales existen antes de actualizar
    IF NOT EXISTS (SELECT 1 FROM [dbo].[TIPO_NFC] WHERE NFCTipoNumero = 0)
    BEGIN
        RAISERROR('No se encontró registro con NFCTipoNumero = 0 (NO REQUERIDO). Abortando.', 16, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[TIPO_NFC] WHERE NFCTipoNumero = 1)
    BEGIN
        RAISERROR('No se encontró registro con NFCTipoNumero = 1 (CREDITO FISCAL). Abortando.', 16, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[TIPO_NFC] WHERE NFCTipoNumero = 2)
    BEGIN
        RAISERROR('No se encontró registro con NFCTipoNumero = 2 (CONSUMIDOR FINAL). Abortando.', 16, 1);
    END

    -- NO REQUERIDO: mantener en 0, visible de primero
    UPDATE [dbo].[TIPO_NFC]
    SET NFCTipoNumero = 0, NFCTipoEstado = 1, Visible_Venta = 1
    WHERE NFCTipoNumero = 0;
    PRINT '  ✓ NO REQUERIDO (0) - Visible_Venta = 1';

    -- CREDITO FISCAL: 1 → 31
    UPDATE [dbo].[TIPO_NFC]
    SET NFCTipoNumero = 31, NFCTipoEstado = 1, Visible_Venta = 0
    WHERE NFCTipoNumero = 1;
    PRINT '  ✓ CREDITO FISCAL: 1 → 31';

    -- CONSUMIDOR FINAL: 2 → 32
    UPDATE [dbo].[TIPO_NFC]
    SET NFCTipoNumero = 32, NFCTipoEstado = 1, Visible_Venta = 0
    WHERE NFCTipoNumero = 2;
    PRINT '  ✓ CONSUMIDOR FINAL: 2 → 32';

    -- NOTA DE DEBITO: 3 → 33
    UPDATE [dbo].[TIPO_NFC]
    SET NFCTipoNumero = 33, NFCTipoEstado = 0, Visible_Venta = 0
    WHERE NFCTipoNumero = 3;
    PRINT '  ✓ NOTA DE DEBITO: 3 → 33';

    -- NOTA DE CREDITO: 4 → 34
    UPDATE [dbo].[TIPO_NFC]
    SET NFCTipoNumero = 34, NFCTipoEstado = 0, Visible_Venta = 0
    WHERE NFCTipoNumero = 4;
    PRINT '  ✓ NOTA DE CREDITO: 4 → 34';

    -- COMPROBANTES GUBERNAMENTALES: 15 → 45
    UPDATE [dbo].[TIPO_NFC]
    SET NFCTipoNumero = 45, NFCTipoEstado = 0, Visible_Venta = 0
    WHERE NFCTipoNumero = 15;
    PRINT '  ✓ COMPROBANTES GUBERNAMENTALES: 15 → 45';

    COMMIT TRANSACTION;
    PRINT '✓ TIPO_NFC actualizada exitosamente (COMMIT)';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '✗ ERROR en TIPO_NFC - ROLLBACK ejecutado. Ningún cambio aplicado.';
    PRINT '  Detalle: ' + ERROR_MESSAGE();
    -- No continuar con el resto del script
    RETURN;
END CATCH
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
PRINT 'Cambios aplicados:';
PRINT '  [TIPO_NFC] NFCTipoNumero actualizado: 1→31, 2→32, 3→33, 4→34, 15→45';
PRINT '  [TABLA]    dbo.LogFacturaElectronica creada';
PRINT '  [SP]       Proc_LogFacturaElectronica_Insert';
PRINT '  [SP]       Proc_LogFacturaElectronica_Update';
PRINT '  [SP]       Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral';
PRINT '  [SP]       Proc_LogFacturaElectronica_GetPendientes';
PRINT '';

-- Verificar TIPO_NFC
IF EXISTS (SELECT 1 FROM [dbo].[TIPO_NFC] WHERE NFCTipoNumero = 31)
    PRINT '✓ TIPO_NFC: CREDITO FISCAL = 31';
ELSE
    PRINT '✗ ERROR: CREDITO FISCAL no tiene NFCTipoNumero = 31';

IF EXISTS (SELECT 1 FROM [dbo].[TIPO_NFC] WHERE NFCTipoNumero = 32)
    PRINT '✓ TIPO_NFC: CONSUMIDOR FINAL = 32';
ELSE
    PRINT '✗ ERROR: CONSUMIDOR FINAL no tiene NFCTipoNumero = 32';

-- Verificar LogFacturaElectronica
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
