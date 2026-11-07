-- ============================================================
-- MODULO DE LAPTOPS - ACTUALIZACION DE DATOS (se ejecuta UNA sola vez por base)
-- Fecha: 2026-10-30
--
-- Ejecutar DESPUES de SCRIPT_MODULO_LAPTOPS_COMPLETO.sql, en TODAS las bases (principal y espejo).
-- Esta protegido: la primera vez aplica los cambios y deja una marca en la base de datos
-- (propiedad extendida MV_Laptops_Datos_PreFacturas_Estados); si se vuelve a ejecutar, no hace nada.
--
-- Que hace:
--   1. Las prefacturas y detalles anulados con el estado antiguo 'A' pasan al estado estandar de anuladas: 'X'.
--   2. El detalle de las prefacturas ya cerradas (estado 'C') que sigue en 'P' pasa a 'C'.
--   (Procesar_PreFactura y PreFacturas_Anular ya dejan los estados nuevos en lo que se haga de aqui en adelante.)
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 0 AND name = N'MV_Laptops_Datos_PreFacturas_Estados')
BEGIN
    PRINT 'Estos cambios de datos ya se aplicaron en esta base. No se hizo nada.';
END
ELSE
BEGIN
    BEGIN TRAN;

    DECLARE @PreFacturasX INT, @DetallesX INT, @DetallesC INT;

    -- 1. Anuladas: estado 'A' -> 'X'
    UPDATE PreFacturas
       SET Estado = 'X'
     WHERE Estado = 'A';
    SET @PreFacturasX = @@ROWCOUNT;

    UPDATE PreDetalle_Facturas
       SET Estado = 'X'
     WHERE Estado = 'A';
    SET @DetallesX = @@ROWCOUNT;

    -- 2. Detalle de prefacturas cerradas: 'P' -> 'C'
    UPDATE d
       SET d.Estado = 'C'
      FROM PreDetalle_Facturas d
      JOIN PreFacturas p ON p.Numero_PreFactura = d.Numero_PreFactura
     WHERE p.Estado = 'C' AND d.Estado = 'P';
    SET @DetallesC = @@ROWCOUNT;

    EXEC sp_addextendedproperty
         @name  = N'MV_Laptops_Datos_PreFacturas_Estados',
         @value = N'Aplicado: prefacturas anuladas A->X, detalles anulados A->X y detalles de prefacturas cerradas P->C';

    COMMIT TRAN;

    SELECT PreFacturas_Anuladas_A_X = @PreFacturasX,
           Detalles_Anulados_A_X    = @DetallesX,
           Detalles_Cerrados_P_A_C  = @DetallesC;
END
GO
