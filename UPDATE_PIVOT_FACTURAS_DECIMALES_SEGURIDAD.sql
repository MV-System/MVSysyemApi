-- ============================================================
-- Feature: Notas de credito - montos con decimales en la copia local de PIVOT
-- Problema:
--   Las columnas de importes de PIVOT_FACTURAS_ENC y PIVOT_FACTURAS_DET en SEGURIDAD son numeric(18,0)
--   (sin decimales). La base remota del proveedor 2 las tiene con 6 decimales (ej. 718.190000). Al guardar
--   la copia local se redondea a enteros (718.19 -> 718) y la nota de credito que se armaba desde esa
--   copia se transmitia con el monto redondeado (847.00 contra una factura de 847.46).
-- Cambio:
--   Los importes pasan a numeric(18,6), igual que la remota. Se conserva la nulabilidad de cada columna.
--   No se tocan CANTIDAD, TASA_IMPUESTO ni PORC_DESCUENTO (no llevan centavos).
--   Los datos que ya estan guardados NO se pueden recuperar (quedan como estan: enteros); solo cambian
--   las filas nuevas. El API ya toma los montos de la factura real al armar la nota (paso 1).
-- Donde ejecutarlo: SOLO en la base SEGURIDAD (la tabla local es unica, no hay espejo).
--   Idempotente: si una columna ya tiene 6 decimales o mas, no se toca. Todo va en una transaccion.
-- Verificado antes de escribirlo (solo lectura, 16/11/2026): sin indices, restricciones, valores por
--   defecto, columnas calculadas ni triggers sobre esas columnas. Los unicos objetos que referencian las
--   tablas son Proc_Pivot_ActualizarEstatus y Proc_Pivot_ObtenerFacturaCompleta (no requieren cambios).
-- Autor: MVSystem
-- Fecha: 2026-11-16
-- ============================================================

-- Seguridad: si no es SEGURIDAD, no se ejecuta nada
IF DB_NAME() <> 'SEGURIDAD'
BEGIN
    DECLARE @baseActual sysname = DB_NAME();
    RAISERROR('Este script se ejecuta SOLO en la base SEGURIDAD (base actual: %s). No se hizo ningun cambio.', 16, 1, @baseActual);
    SET NOEXEC ON;
END
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY

    -- ---------------- PIVOT_FACTURAS_ENC ----------------
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_ENC') AND name = 'SUB_TOTAL' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_ENC ALTER COLUMN SUB_TOTAL numeric(18,6) NOT NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_ENC') AND name = 'DESCUENTO' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_ENC ALTER COLUMN DESCUENTO numeric(18,6) NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_ENC') AND name = 'IMPUESTO' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_ENC ALTER COLUMN IMPUESTO numeric(18,6) NOT NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_ENC') AND name = 'TOTAL' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_ENC ALTER COLUMN TOTAL numeric(18,6) NOT NULL;

    -- ---------------- PIVOT_FACTURAS_DET ----------------
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_DET') AND name = 'PRECIO' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_DET ALTER COLUMN PRECIO numeric(18,6) NOT NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_DET') AND name = 'MONTO_DESCUENTO' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_DET ALTER COLUMN MONTO_DESCUENTO numeric(18,6) NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_DET') AND name = 'SUB_TOTAL' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_DET ALTER COLUMN SUB_TOTAL numeric(18,6) NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_DET') AND name = 'IMPUESTO' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_DET ALTER COLUMN IMPUESTO numeric(18,6) NULL;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PIVOT_FACTURAS_DET') AND name = 'TOTAL' AND scale < 6)
        ALTER TABLE dbo.PIVOT_FACTURAS_DET ALTER COLUMN TOTAL numeric(18,6) NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

-- Comprobacion: todas deben mostrar scale = 6
SELECT OBJECT_NAME(object_id) AS Tabla, name AS Columna, precision AS Precision, scale AS Escala, is_nullable AS Acepta_Null
FROM sys.columns
WHERE object_id IN (OBJECT_ID('dbo.PIVOT_FACTURAS_ENC'), OBJECT_ID('dbo.PIVOT_FACTURAS_DET'))
  AND name IN ('SUB_TOTAL', 'DESCUENTO', 'IMPUESTO', 'TOTAL', 'PRECIO', 'MONTO_DESCUENTO')
ORDER BY Tabla, Columna;
GO

SET NOEXEC OFF;
GO
