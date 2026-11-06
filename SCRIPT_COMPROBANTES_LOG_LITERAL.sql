-- ============================================================
-- COMPROBANTES ELECTRONICOS - LITERAL DE 2 CARACTERES EN LogFacturaElectronica
-- Fecha: 2026-11-06
--
-- Problema: la columna LogFacturaElectronica.Literal es CHAR(2), pero los tres procedimientos reciben
-- @Literal CHAR(1): cualquier literal de 2 letras se corta a 1 (las notas de credito del proveedor 2 se guardan
-- como 'N' y no como 'NC'). La nota de credito del proveedor 1 usa "N" + literal real de la factura ('NA', 'NB'...).
--
-- Que hace: cambia SOLO el tipo del parametro @Literal de CHAR(1) a CHAR(2) en los tres procedimientos.
-- El resto de cada procedimiento queda igual a lo que hay hoy en las bases (definiciones tomadas de TECNO_STYLE_E y
-- MV_SYSTEM; son identicas en ambas). Ampliar el parametro no cambia nada de lo que ya existe: las filas actuales
-- ('A ' y 'N ') se siguen encontrando igual.
--
-- Ejecutar en la base de CADA empresa que trabaja con comprobantes electronicos (la que usa el API para facturar:
-- GAPMOVILES, DATAMOVIL, VISIONTECH, TECNO_STYLE_E, BESTPHONE_STORE_E, etc.). No hace falta en las bases espejo
-- (el log no se replica). Es idempotente: se puede repetir. EJECUTARLO ANTES de publicar la Parte 1 de la nota de credito.
-- ============================================================

CREATE OR ALTER PROCEDURE dbo.Proc_LogFacturaElectronica_Insert
    @Numero_Factura INT,
    @Literal CHAR(2) = 'A',
    @DocumentoId_EasyConexion INT = NULL,
    @VoucherNumber_DGII VARCHAR(50) = NULL,
    @Estado VARCHAR(50) = 'PEND',
    @Respuesta_JSON NVARCHAR(MAX) = NULL,
    @Fecha_Envio DATETIME,
    @Error_Descripcion NVARCHAR(MAX) = NULL,
    @Usuario VARCHAR(50) = NULL,
    @QR_Url VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.LogFacturaElectronica
            (Numero_Factura, Literal, DocumentoId_EasyConexion, VoucherNumber_DGII,
             Estado, Respuesta_JSON, Fecha_Envio, Error_Descripcion, Usuario, QR_Url)
        VALUES
            (@Numero_Factura, @Literal, @DocumentoId_EasyConexion, @VoucherNumber_DGII,
             @Estado, @Respuesta_JSON, @Fecha_Envio, @Error_Descripcion, @Usuario, @QR_Url);
        RETURN 1;
    END TRY
    BEGIN CATCH
        PRINT '? Error: ' + ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.Proc_LogFacturaElectronica_Update
    @Numero_Factura INT,
    @Literal CHAR(2) = 'A',
    @DocumentoId_EasyConexion INT = NULL,
    @VoucherNumber_DGII VARCHAR(50) = NULL,
    @Estado VARCHAR(50),
    @Respuesta_JSON NVARCHAR(MAX) = NULL,
    @Fecha_Confirmacion DATETIME = NULL,
    @Error_Descripcion NVARCHAR(MAX) = NULL,
    @QR_Url VARCHAR(500) = NULL
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
            Error_Descripcion = @Error_Descripcion,
            QR_Url = @QR_Url
        WHERE
            Numero_Factura = @Numero_Factura AND Literal = @Literal;
        RETURN 1;
    END TRY
    BEGIN CATCH
        PRINT '? Error: ' + ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral
    @Numero_Factura INT,
    @Literal CHAR(2) = 'A'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT TOP 1
            ID_Log, Numero_Factura, Literal, DocumentoId_EasyConexion,
            VoucherNumber_DGII, Estado, Respuesta_JSON, Fecha_Envio,
            Fecha_Confirmacion, Error_Descripcion, Usuario, Fecha_Registro, QR_Url
        FROM dbo.LogFacturaElectronica
        WHERE Numero_Factura = @Numero_Factura AND Literal = @Literal
        ORDER BY ID_Log DESC;
        RETURN 0;
    END TRY
    BEGIN CATCH
        PRINT '? Error: ' + ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END
GO

-- Verificacion: los tres parametros deben quedar en CHAR(2) (max_length 2)
SELECT p.name AS Procedimiento, pa.name AS Parametro, t.name AS Tipo, pa.max_length AS Longitud
  FROM sys.procedures p
  JOIN sys.parameters pa ON pa.object_id = p.object_id
  JOIN sys.types t ON t.user_type_id = pa.user_type_id
 WHERE p.name LIKE 'Proc_LogFacturaElectronica%' AND pa.name = '@Literal';
GO
