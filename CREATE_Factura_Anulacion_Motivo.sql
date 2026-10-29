-- ============================================================
-- Feature: Tareas pendientes - Nota (motivo) al anular una factura
-- Propósito:
--   1. Columnas nuevas y opcionales en Facturas: Motivo_Anulacion, Usuario_Anulacion, Fecha_Anulacion.
--      Las facturas ya anuladas quedan con estos campos en NULL (sin registro).
--   2. Proc_Anular_Factura recibe @Motivo VARCHAR(500) = NULL (parametro nuevo al final; los
--      existentes no cambian). Toda la logica original queda IDENTICA. Solo se agrega, al final y
--      fuera del cursor, un UPDATE que guarda motivo, usuario y fecha de la anulacion.
--      Se hace fuera del cursor para que quede guardado aunque el detalle tenga tipos que el
--      cursor no actualiza (solo procesa los tipos 1, 2 y 4).
--   El motivo es obligatorio para las anulaciones manuales: lo exige el API y el front. El SP lo
--   acepta NULL para no romper otros llamadores.
-- Orden: ejecutar este script antes de UPDATE_Proc_Facturas_Consulta_Anulacion.sql.
-- Autor: MVSystem
-- Fecha: 2026-10-28
-- ============================================================

-- ------------------------------------------------------------
-- 1. Columnas de auditoria de la anulacion
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Facturas', 'Motivo_Anulacion') IS NULL
    ALTER TABLE dbo.Facturas ADD Motivo_Anulacion VARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.Facturas', 'Usuario_Anulacion') IS NULL
    ALTER TABLE dbo.Facturas ADD Usuario_Anulacion VARCHAR(60) NULL;
GO
IF COL_LENGTH('dbo.Facturas', 'Fecha_Anulacion') IS NULL
    ALTER TABLE dbo.Facturas ADD Fecha_Anulacion DATETIME NULL;
GO

-- ------------------------------------------------------------
-- 2. Proc_Anular_Factura con @Motivo (logica original sin cambios)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[Proc_Anular_Factura]
@NumeroFactura INT,
@Usuario VARCHAR(60),
@IdSucursal INT,
@Motivo VARCHAR(500) = NULL
AS

    DECLARE @IdTipo INT, @Cantidad INT, @IdEquipo varchar(50), @Tipo INT, @IdAccesorio INT, @LiteralFt char(1), @LiteralCompleta varchar(15);

    SET @LiteralFt = (SELECT literal FROM Facturas WHERE Numero_Factura = @NumeroFactura AND ID_Sucursal = @IdSucursal)
    SET @LiteralCompleta = @LiteralFt + CONVERT(VARCHAR(10), @NumeroFactura)

    -- ============================================================
    -- Reestablecer secuencia del comprobante si es el último asignado
    -- ============================================================
    DECLARE @NfcTipo INT, @NfcSecuencia BIGINT
    SELECT @NfcTipo = NFCTipoNumero, @NfcSecuencia = NFCFISCAL
    FROM Facturas
    WHERE Numero_Factura = @NumeroFactura AND ID_Sucursal = @IdSucursal AND Literal = @LiteralFt

    --IF @NfcTipo IS NOT NULL AND @NfcTipo <> 0 AND @NfcSecuencia IS NOT NULL AND @NfcSecuencia > 0
    --BEGIN
    --    -- Solo decrementar si esta factura tiene la última secuencia asignada
    --    IF @NfcSecuencia = (SELECT NFCTipoSecuencia FROM TIPO_NFC WHERE NFCTipoNumero = @NfcTipo)
    --    BEGIN
    --        UPDATE TIPO_NFC
    --        SET NFCTipoSecuencia = NFCTipoSecuencia - 1
    --        WHERE NFCTipoNumero = @NfcTipo
    --    END
    --END
    -- ============================================================

DECLARE detalle_cursor CURSOR FOR
    SELECT df.ID_Tipo, df.Cantidad, df.ID_Equipo
    FROM Detalle_Facturas df
    INNER JOIN Facturas ft ON df.Numero_Factura = ft.Numero_Factura AND df.LiteralF = @LiteralCompleta
    WHERE ft.Numero_Factura = @NumeroFactura AND ID_Sucursal = @IdSucursal;

OPEN detalle_cursor;
FETCH NEXT FROM detalle_cursor INTO @IdTipo, @Cantidad, @IdEquipo;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF @IdTipo = 1
    BEGIN
       INSERT INTO [dbo].[Equipos_Historico]
           ([Imei],[ID_Modelo],[ID_Tecnologia],[ID_Garantia],[ID_Condicion],[ID_Estado_Bloqueo],[ID_Almacen],[ID_Suplidor],[Numero_de_Factura],[Precio_Por_Mayor],[Precio_Detalle],[Costo_Equipo],[Desbloqueado],[Disponible],[Nota_Adicional],Nota_Historico,[Usuario],[Fecha_registro],[Numero_Invetario],[Numero_Registro],Usuario_Historico,Fecha_registro_Historico)
       SELECT [Imei],[ID_Modelo],[ID_Tecnologia],[ID_Garantia],[ID_Condicion],[ID_Estado_Bloqueo],[ID_Almacen],[ID_Suplidor],[Numero_de_Factura],[Precio_Por_Mayor],[Precio_Detalle],[Costo_Equipo],[Desbloqueado],[Disponible],[Nota_Adicional],'Se anuló la Factura # '+CONVERT(varchar(50),@NumeroFactura)+' Por El Usuario: '+@Usuario,[Usuario],[Fecha_registro],[Numero_Invetario],[Numero_Registro],@Usuario,(dateadd(hour,(-4),getdate()))
       FROM Equipos WITH(NoLock) WHERE Imei = @IdEquipo

        UPDATE dbo.Equipos
        SET Disponible = 'S',
            Nota_Adicional = 'Factura # '+CONVERT(varchar(50),@NumeroFactura)+' Anulada Por El Usuario: '+@Usuario+' En Fecha: '+convert(varchar(50),(dateadd(hour,(-4),getdate()))),
            Usuario = @Usuario
        WHERE Imei = @IdEquipo

        UPDATE dbo.Facturas
        SET Estado = 'X', Usuario = @Usuario,
            Nota = 'Factura anulada por el usuario: ' + @Usuario + ' en fecha: ' + CONVERT(varchar(19), DATEADD(hour, -4, GETDATE()), 120),
            Fecha_registro = (dateadd(hour,(-4),getdate()))
        WHERE Numero_Factura = @NumeroFactura AND ID_Sucursal = @IdSucursal AND literal = @LiteralFt

        UPDATE dbo.Detalle_Facturas
        SET Estado = 'X', Usuario = @Usuario, Fecha_registro = (dateadd(hour,(-4),getdate()))
        WHERE Numero_Factura = @NumeroFactura AND ID_Equipo = @IdEquipo AND literalF = @LiteralCompleta
    END
    ELSE IF @IdTipo = 2
    BEGIN
        INSERT INTO [dbo].[Accesorios_Historico]
            ([ID_Accesorio],[Imei_Equipo],[ID_Modelo],[ID_Almacen],[Numero_de_Factura],[Descripcion_Accesorio],[Cantidad],[Can_Max],[Can_Min],[Precio_Por_Mayor],[Precio_Detalle],[Costo_Equipo],[Disponible],[Disponible_Detalle],[Estado],[Usuario],[Fecha_registro],[Numero_Invetario],[Codigo_Barra],[Estado_Historico],[Usuario_Historico],[Fecha_registro_Historico])
        SELECT [ID_Accesorio],[Imei_Equipo],[ID_Modelo],[ID_Almacen],[Numero_de_Factura],[Descripcion_Accesorio],[Cantidad],[Can_Max],[Can_Min],[Precio_Por_Mayor],[Precio_Detalle],[Costo_Equipo],[Disponible],[Disponible_Detalle],[Estado],[Usuario],[Fecha_registro],[Numero_Invetario],[Codigo_Barra],'A',@Usuario,dateadd(hour,-4,getdate())
        FROM [dbo].[Accesorios] WHERE id_accesorio = @IdEquipo

        UPDATE dbo.Accesorios
        SET Cantidad = (SELECT Cantidad FROM Accesorios WHERE ID_Accesorio = @IdEquipo) + @Cantidad,
            Usuario = @Usuario, Fecha_registro = (dateadd(hour,(-4),getdate()))
        WHERE ID_Accesorio = @IdEquipo

        UPDATE dbo.Facturas
        SET Estado = 'X', Usuario = @Usuario,
            Nota = 'Factura anulada por el usuario: ' + @Usuario + ' en fecha: ' + CONVERT(varchar(19), DATEADD(hour, -4, GETDATE()), 120),
            Fecha_Registro = DATEADD(hour, -4, GETDATE())
        WHERE Numero_Factura = @NumeroFactura AND ID_Sucursal = @IdSucursal AND Literal = @LiteralFt;

        UPDATE dbo.Detalle_Facturas
        SET Estado = 'X', Usuario = @Usuario, Fecha_registro = (dateadd(hour,(-4),getdate()))
        WHERE Numero_Factura = @NumeroFactura AND ID_Equipo = @IdEquipo AND literalF = @LiteralCompleta
    END
    ELSE IF @IdTipo = 4
    BEGIN
        DECLARE @id_repa int = (SELECT ID_Reparacion FROM dbo.Reparaciones WHERE Numero_Factura = @NumeroFactura)

        UPDATE dbo.Reparaciones
        SET ID_Estado_Taller = 3, estado = 'X', Usuario = @Usuario, Fecha_registro = (dateadd(hour,(-4),getdate()))
        WHERE ID_Reparacion = @id_repa

        UPDATE dbo.Facturas
        SET Estado = 'X', Usuario = @Usuario,
            Nota = 'Factura anulada por el usuario: ' + @Usuario + ' en fecha: ' + CONVERT(varchar(19), DATEADD(hour, -4, GETDATE()), 120),
            Fecha_registro = (dateadd(hour,(-4),getdate()))
        WHERE Numero_Factura = @NumeroFactura AND ID_Sucursal = @IdSucursal AND literal = @LiteralFt

        UPDATE dbo.Detalle_Facturas
        SET Estado = 'X', Usuario = @Usuario, Fecha_registro = (dateadd(hour,(-4),getdate()))
        WHERE Numero_Factura = @NumeroFactura AND literalF = @LiteralCompleta
    END

    FETCH NEXT FROM detalle_cursor INTO @IdTipo, @Cantidad, @IdEquipo;
END

CLOSE detalle_cursor;
DEALLOCATE detalle_cursor;

-- ============================================================
-- NUEVO: motivo, usuario y fecha de la anulacion (fuera del cursor).
-- Solo si la factura quedo anulada (Estado = 'X').
-- ============================================================
UPDATE dbo.Facturas
SET Motivo_Anulacion = NULLIF(LTRIM(RTRIM(@Motivo)), ''),
    Usuario_Anulacion = @Usuario,
    Fecha_Anulacion = DATEADD(hour, -4, GETDATE())
WHERE Numero_Factura = @NumeroFactura AND ID_Sucursal = @IdSucursal AND Literal = @LiteralFt AND Estado = 'X'
GO
