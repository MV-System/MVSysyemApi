-- =====================================================================
-- RETENCION POR TARJETA - SCRIPT COMPLETO (Partes 0 a 5 + garantia de prefactura)
-- Idempotente: se puede correr mas de una vez.
-- Primero en MV_SYSTEM. Al desplegar: principal y espejo de cada empresa, fuera de horario
-- (ALTER TABLE sobre Facturas e Historio_Pagos).
-- Al final: configurar el % de la tarjeta (seccion 8).
-- =====================================================================


-- #####################################################################
-- 1. UPDATE_PreFacturas_Garantia.sql
-- #####################################################################
-- =====================================================================
-- Garantia en la PreFactura (cabecera)
-- La impresion de la factura toma la garantia de Facturas.ID_Garantia (SP_Reporte_Factura).
-- Al procesar una prefactura, Procesar_PreFactura no copiaba la garantia a la cabecera de la factura.
--   1. PreFacturas.ID_Garantia (nuevo campo)
--   2. PreFacturas_Insert recibe @ID_Garantia (opcional)
--   3. Procesar_PreFactura copia la garantia a Facturas.ID_Garantia
--      (si la prefactura no la tiene, la toma de sus lineas; si tampoco, 7 = No Aplica)
-- Idempotente. Ejecutar en cada base (principal y espejo). API y front se publican junto con este script.
-- =====================================================================

IF COL_LENGTH('dbo.PreFacturas', 'ID_Garantia') IS NULL
    ALTER TABLE dbo.PreFacturas ADD ID_Garantia INT NULL;
GO

CREATE OR ALTER PROCEDURE [dbo].[PreFacturas_Insert]
@Numero_PreFactura INT OUTPUT,
@ID_Cliente INT,
@ID_Tipo_Factura INT,
@ID_Vendedor INT = NULL,
@ID_Tipo_Pagos INT = NULL,
@ID_Sucursal INT,
@SubTotal MONEY = NULL,
@Descuento MONEY = NULL,
@Itbis MONEY = NULL,
@NFCTipoNumero VARCHAR(50) = NULL,
@NFCFISCAL VARCHAR(50) = NULL,
@NCF_NUMERO VARCHAR(50) = NULL,
@RNC VARCHAR(11) = NULL,
@Abono MONEY = NULL,
@Tiempo_Credito DATETIME = NULL,
@Total MONEY = NULL,
@Cantidad_Articulos INT,
@Nota VARCHAR(500) = NULL,
@Estado CHAR(1) = 'P',
@Usuario VARCHAR(50),
@Fecha_Registro DATETIME = NULL,
@Fecha_Procesamiento DATETIME = NULL,
@Usuario_Procesamiento VARCHAR(50) = NULL,
@Literal CHAR(1) = 'X',
@Cotizacion_Numero INT = 0,
@ID_Garantia INT = NULL
AS
BEGIN
    IF @Numero_PreFactura IS NULL
    BEGIN
        SET @Numero_PreFactura = ISNULL((SELECT MAX(Numero_PreFactura) FROM PreFacturas), 0) + 1
    END

    IF @Fecha_Registro IS NULL
    BEGIN
        SET @Fecha_Registro = dateadd (hour,-4,getdate())
    END

	 set @Literal = (select literal from Almacen_Cata where ID_Almacen = (select ID_Almacen from sucursales_cata where ID_Sucursal = @ID_Sucursal))

  Set @Numero_PreFactura = (Select IsNull(Max(Numero_PreFactura),1)
                              From PreFacturas
                              Where Literal = @Literal)

  Set @Numero_PreFactura = @Numero_PreFactura+1 -- Incrementa la Factura

    INSERT INTO PreFacturas (
        Numero_PreFactura, ID_Cliente, ID_Tipo_Factura, ID_Vendedor, ID_Tipo_Pagos, ID_Sucursal,
        SubTotal, Descuento, Itbis, NFCTipoNumero, NFCFISCAL, NCF_NUMERO, RNC, Abono, Tiempo_Credito, Total,
        Cantidad_Articulos, Nota, Estado, Usuario, Fecha_Registro, Fecha_Procesamiento, Usuario_Procesamiento, Literal, Cotizacion_Numero,
        ID_Garantia
    )
    VALUES (
        @Numero_PreFactura, @ID_Cliente, @ID_Tipo_Factura, @ID_Vendedor, @ID_Tipo_Pagos, @ID_Sucursal,
        @SubTotal, @Descuento, @Itbis, @NFCTipoNumero, @NFCFISCAL, @NCF_NUMERO, @RNC, @Abono, @Tiempo_Credito, @Total,
        @Cantidad_Articulos, @Nota, @Estado, @Usuario, @Fecha_Registro, @Fecha_Procesamiento, @Usuario_Procesamiento, @Literal, @Cotizacion_Numero,
        @ID_Garantia
    )

    -- Retornar el registro recién insertado
    SELECT TOP 1 *
    FROM PreFacturas
    WHERE Numero_PreFactura = @Numero_PreFactura
    ORDER BY Fecha_Registro DESC
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Procesar_PreFactura]
    @Numero_PreFactura INT,
    @Usuario_Procesamiento VARCHAR(50),
    @Nuevo_Numero_Factura INT OUTPUT,
    @Mensaje NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RowsAffected INT = 0;
    DECLARE @TipoPago INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TipoPago = ID_Tipo_Pagos
        FROM PreFacturas
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        IF @TipoPago IS NULL
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'PreFactura no encontrada o no esta en estado Pendiente (P)';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        SELECT @Nuevo_Numero_Factura =
            ISNULL(MAX(Numero_Factura), 0) + 1
        FROM Facturas;

        INSERT INTO Facturas
        (
            Numero_Factura, ID_Cliente, ID_Tipo_Factura, ID_Vendedor,
            ID_Tipo_Pagos, ID_Sucursal, SubTotal, Descuento, Itbis,
            NFCTipoNumero, RNC, Abono, Tiempo_Credito, Total,
            Cantidad_Articulos, Nota, Estado, Usuario, Fecha_Registro,
            Literal, Cotizacion_Numero, ID_Garantia
        )
        SELECT
            @Nuevo_Numero_Factura,
            ID_Cliente,
            ID_Tipo_Factura,
            ID_Vendedor,
            ID_Tipo_Pagos,
            ID_Sucursal,
            SubTotal,
            Descuento,
            Itbis,
            NFCTipoNumero,
            RNC,
            Abono,
            Tiempo_Credito,
            Total,
            Cantidad_Articulos,
            Nota,
            'A',
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE()),
            Literal,
            Cotizacion_Numero,
            -- Garantia: la de la prefactura; si no la tiene (prefacturas anteriores), la de sus lineas; si no, 7 = No Aplica
            COALESCE(ID_Garantia,
                     (SELECT MAX(d.ID_Garantia) FROM PreDetalle_Facturas d
                       WHERE d.Numero_PreFactura = @Numero_PreFactura AND d.Estado = 'P'),
                     7)
        FROM PreFacturas
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se pudo insertar la cabecera de la factura';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        INSERT INTO Detalle_Facturas
        (
            Numero_Factura, ID_Equipo, Descripcion, ID_Tipo,
            ID_Garantia, cantidad, precio, SubTotal, Descuento,
            LiteralF, Itbis, Total, Estado, Usuario,
            Fecha_registro, Costo_Equipo
        )
        SELECT
            @Nuevo_Numero_Factura,
            pf.ID_Equipo,
            pf.Descripcion,
            pf.ID_Tipo,
            pf.ID_Garantia,
            pf.cantidad,
            pf.precio,
            pf.SubTotal,
            pf.Descuento,
            CONCAT(p.Literal, @Nuevo_Numero_Factura),
            pf.Itbis,
            pf.Total,
            'A',
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE()),
            pf.Costo_Equipo
        FROM PreDetalle_Facturas pf
        INNER JOIN PreFacturas p
            ON pf.Numero_PreFactura = p.Numero_PreFactura
        WHERE p.Numero_PreFactura = @Numero_PreFactura
          AND pf.Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se encontraron detalles para procesar';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        -- Copiar pagos mixtos P -> F sin modificar la prefactura
        IF @TipoPago = 6
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM DetallesPagos_Factura
                WHERE Numero_Factura = @Numero_PreFactura
                  AND Tipo_Documento = 'P'
                  AND Estado = 'A'
            )
            BEGIN
                SET @Nuevo_Numero_Factura = 0;
                SET @Mensaje = 'Error: La prefactura Mixta no tiene detalles de pago';
                ROLLBACK TRANSACTION;
                SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
                RETURN;
            END;

            EXEC dbo.CopiarDetallesPagoPrefacturaAFactura
                @Numero_PreFactura = @Numero_PreFactura,
                @Numero_Factura = @Nuevo_Numero_Factura;
        END;

        INSERT INTO Equipos_Historico
        (
            Imei, ID_Modelo, ID_Tecnologia, ID_Garantia,
            ID_Condicion, ID_Estado_Bloqueo, ID_Almacen,
            ID_Suplidor, Numero_de_Factura, Precio_Por_Mayor,
            Precio_Detalle, Costo_Equipo, Desbloqueado,
            Disponible, Nota_Adicional, Nota_Historico,
            Usuario, Fecha_registro, Numero_Invetario,
            Numero_Registro, Usuario_Historico,
            Fecha_registro_Historico
        )
        SELECT
            Imei,
            ID_Modelo,
            ID_Tecnologia,
            ID_Garantia,
            ID_Condicion,
            ID_Estado_Bloqueo,
            ID_Almacen,
            ID_Suplidor,
            Numero_de_Factura,
            Precio_Por_Mayor,
            Precio_Detalle,
            Costo_Equipo,
            Desbloqueado,
            Disponible,
            Nota_Adicional,
            'Vendido en factura #' + CONVERT(VARCHAR(50), @Nuevo_Numero_Factura),
            Usuario,
            Fecha_registro,
            Numero_Invetario,
            Numero_Registro,
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE())
        FROM Equipos WITH (NOLOCK)
        WHERE Imei IN
        (
            SELECT DISTINCT ID_Equipo
            FROM PreDetalle_Facturas
            WHERE Numero_PreFactura = @Numero_PreFactura
              AND ID_Tipo = 1
              AND Estado = 'P'
        );

        UPDATE Equipos
        SET Disponible = 'N',
            Nota_Adicional = 'Vendido en factura #' +
                CONVERT(VARCHAR(50), @Nuevo_Numero_Factura),
            Numero_de_Factura = @Nuevo_Numero_Factura
        WHERE Imei IN
        (
            SELECT DISTINCT ID_Equipo
            FROM PreDetalle_Facturas
            WHERE Numero_PreFactura = @Numero_PreFactura
              AND ID_Tipo = 1
              AND Estado = 'P'
        );

        UPDATE a
        SET a.Cantidad = a.Cantidad - pf.cantidad
        FROM Accesorios a
        INNER JOIN PreDetalle_Facturas pf
            ON a.ID_Accesorio = pf.ID_Equipo
        WHERE pf.Numero_PreFactura = @Numero_PreFactura
          AND pf.ID_Tipo = 2
          AND pf.Estado = 'P';

        UPDATE Reparaciones
        SET ID_Estado_Taller = 3,
            SubTotal = pf.SubTotal,
            Descuento = pf.Descuento,
            Itbis = pf.Itbis,
            Numero_Factura = @Nuevo_Numero_Factura
        FROM PreDetalle_Facturas pf
        WHERE CONVERT(INT, pf.ID_Equipo) = Reparaciones.ID_Reparacion
          AND pf.Numero_PreFactura = @Numero_PreFactura
          AND pf.ID_Tipo IN (3, 4)
          AND pf.Estado = 'P';

        UPDATE PreFacturas
        SET Estado = 'C',
            Fecha_Procesamiento = DATEADD(HOUR, -4, GETDATE()),
            Usuario_Procesamiento = @Usuario_Procesamiento
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se pudo actualizar el estado de la PreFactura';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        -- El detalle de la prefactura tambien pasa a 'C'
        UPDATE PreDetalle_Facturas
           SET Estado = 'C'
         WHERE Numero_PreFactura = @Numero_PreFactura
           AND Estado = 'P';

        COMMIT TRANSACTION;

        SET @Mensaje =
            'Factura creada satisfactoriamente. Factura #' +
            CONVERT(VARCHAR(50), @Nuevo_Numero_Factura);
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Nuevo_Numero_Factura = 0;
        SET @Mensaje = 'ERROR: ' + ERROR_MESSAGE();
    END CATCH;

    SELECT
        @Nuevo_Numero_Factura AS Cod_Factura,
        @Mensaje AS Mensaje;
END;
GO

-- #####################################################################
-- 2. CREATE_RETENCION_TARJETA.sql
-- #####################################################################
-- =====================================================================
-- Retencion por pagos con tarjeta
--   Tipos_Pagos_Cata.Porcentaje_Retencion        (% que retiene el procesador; > 0 = es tarjeta)
--   Facturas / Historio_Pagos: Porcentaje_Retencion_Tarjeta y Monto_Retencion_Tarjeta
--   SP Factura_Calcular_Retencion_Tarjeta y Abono_Calcular_Retencion_Tarjeta
-- Idempotente. Ejecutar en cada base (principal y espejo).
-- Despues, configurar el % con CONFIGURAR_RETENCION_TARJETA.sql.
-- =====================================================================

IF COL_LENGTH('dbo.Tipos_Pagos_Cata', 'Porcentaje_Retencion') IS NULL
    ALTER TABLE dbo.Tipos_Pagos_Cata ADD Porcentaje_Retencion DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Tipos_Pagos_Cata_Porcentaje_Retencion DEFAULT 0;
GO

IF COL_LENGTH('dbo.Facturas', 'Porcentaje_Retencion_Tarjeta') IS NULL
    ALTER TABLE dbo.Facturas ADD Porcentaje_Retencion_Tarjeta DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Facturas_Porcentaje_Retencion_Tarjeta DEFAULT 0;
GO

IF COL_LENGTH('dbo.Facturas', 'Monto_Retencion_Tarjeta') IS NULL
    ALTER TABLE dbo.Facturas ADD Monto_Retencion_Tarjeta DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Facturas_Monto_Retencion_Tarjeta DEFAULT 0;
GO

IF COL_LENGTH('dbo.Historio_Pagos', 'Porcentaje_Retencion_Tarjeta') IS NULL
    ALTER TABLE dbo.Historio_Pagos ADD Porcentaje_Retencion_Tarjeta DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Historio_Pagos_Porcentaje_Retencion_Tarjeta DEFAULT 0;
GO

IF COL_LENGTH('dbo.Historio_Pagos', 'Monto_Retencion_Tarjeta') IS NULL
    ALTER TABLE dbo.Historio_Pagos ADD Monto_Retencion_Tarjeta DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Historio_Pagos_Monto_Retencion_Tarjeta DEFAULT 0;
GO

-- Calcula y guarda la retencion de una factura. Sin transaccion propia: usa la del llamador.
-- Forma de pago tarjeta: Total de la factura. Mixto (6): suma de las lineas de pago con tarjeta. Otras: 0.
CREATE OR ALTER PROCEDURE dbo.Factura_Calcular_Retencion_Tarjeta
    @Numero_Factura INT,
    @ID_Sucursal INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TipoPago INT, @Total DECIMAL(18,2), @Pct DECIMAL(5,2) = 0, @MontoTarjeta DECIMAL(18,2) = 0;

    SELECT TOP (1) @TipoPago = ID_Tipo_Pagos, @Total = CONVERT(DECIMAL(18,2), ISNULL(Total, 0))
      FROM dbo.Facturas
     WHERE Numero_Factura = @Numero_Factura AND ID_Sucursal = @ID_Sucursal;

    IF @TipoPago IS NULL RETURN;

    IF @TipoPago = 6
        SELECT @MontoTarjeta = ISNULL(SUM(CONVERT(DECIMAL(18,2), dp.Monto_Pago)), 0),
               @Pct = ISNULL(MAX(tp.Porcentaje_Retencion), 0)
          FROM dbo.DetallesPagos_Factura dp
          JOIN dbo.Tipos_Pagos_Cata tp ON tp.ID_Tipo_Pagos = dp.ID_Tipo_Pago
         WHERE dp.Numero_Factura = @Numero_Factura
           AND dp.Tipo_Documento = 'F'
           AND dp.Estado = 'A'
           AND tp.Porcentaje_Retencion > 0;
    ELSE
        SELECT @Pct = Porcentaje_Retencion,
               @MontoTarjeta = CASE WHEN Porcentaje_Retencion > 0 THEN @Total ELSE 0 END
          FROM dbo.Tipos_Pagos_Cata
         WHERE ID_Tipo_Pagos = @TipoPago;

    IF @MontoTarjeta > @Total SET @MontoTarjeta = @Total;
    IF @MontoTarjeta <= 0 SELECT @MontoTarjeta = 0, @Pct = 0;

    UPDATE dbo.Facturas
       SET Porcentaje_Retencion_Tarjeta = @Pct,
           Monto_Retencion_Tarjeta = ROUND(@MontoTarjeta * @Pct / 100, 2)
     WHERE Numero_Factura = @Numero_Factura AND ID_Sucursal = @ID_Sucursal;
END;
GO

-- Calcula y guarda la retencion de un abono (todas las filas del lote), segun el tipo de pago del abono.
CREATE OR ALTER PROCEDURE dbo.Abono_Calcular_Retencion_Tarjeta
    @IdLoteAbono UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE hp
       SET Porcentaje_Retencion_Tarjeta = tp.Porcentaje_Retencion,
           Monto_Retencion_Tarjeta = ROUND(CONVERT(DECIMAL(18,2), hp.Total_Abono) * tp.Porcentaje_Retencion / 100, 2)
      FROM dbo.Historio_Pagos hp
      JOIN dbo.Tipos_Pagos_Cata tp ON tp.ID_Tipo_Pagos = hp.ID_Tipo_Pagos
     WHERE hp.Id_Lote_Abono = @IdLoteAbono
       AND tp.Porcentaje_Retencion > 0
       AND hp.Total_Abono > 0;
END;
GO

-- Verificacion: las 5 columnas y los 2 SP deben aparecer con valor (no NULL)
SELECT
    COL_LENGTH('dbo.Tipos_Pagos_Cata', 'Porcentaje_Retencion')      AS Tipos_Pagos_Porcentaje,
    COL_LENGTH('dbo.Facturas', 'Porcentaje_Retencion_Tarjeta')      AS Facturas_Porcentaje,
    COL_LENGTH('dbo.Facturas', 'Monto_Retencion_Tarjeta')           AS Facturas_Monto,
    COL_LENGTH('dbo.Historio_Pagos', 'Porcentaje_Retencion_Tarjeta') AS Pagos_Porcentaje,
    COL_LENGTH('dbo.Historio_Pagos', 'Monto_Retencion_Tarjeta')     AS Pagos_Monto,
    OBJECT_ID('dbo.Factura_Calcular_Retencion_Tarjeta', 'P')        AS SP_Factura,
    OBJECT_ID('dbo.Abono_Calcular_Retencion_Tarjeta', 'P')          AS SP_Abono;
GO

-- #####################################################################
-- 3. UPDATE_Procesar_PreFactura_Retencion.sql
-- #####################################################################
-- =====================================================================
-- Retencion de tarjeta al procesar una prefactura
-- Procesar_PreFactura calcula la retencion de la factura generada (Factura_Calcular_Retencion_Tarjeta)
-- despues de copiar los pagos y dentro de la misma transaccion.
-- Es la copia integra de Procesar_PreFactura de UPDATE_PreFacturas_Garantia.sql mas ese cambio.
-- Ejecutar DESPUES de UPDATE_PreFacturas_Garantia.sql y CREATE_RETENCION_TARJETA.sql.
-- Idempotente. Ejecutar en cada base (principal y espejo).
-- =====================================================================

CREATE OR ALTER PROCEDURE [dbo].[Procesar_PreFactura]
    @Numero_PreFactura INT,
    @Usuario_Procesamiento VARCHAR(50),
    @Nuevo_Numero_Factura INT OUTPUT,
    @Mensaje NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RowsAffected INT = 0;
    DECLARE @TipoPago INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TipoPago = ID_Tipo_Pagos
        FROM PreFacturas
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        IF @TipoPago IS NULL
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'PreFactura no encontrada o no esta en estado Pendiente (P)';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        SELECT @Nuevo_Numero_Factura =
            ISNULL(MAX(Numero_Factura), 0) + 1
        FROM Facturas;

        INSERT INTO Facturas
        (
            Numero_Factura, ID_Cliente, ID_Tipo_Factura, ID_Vendedor,
            ID_Tipo_Pagos, ID_Sucursal, SubTotal, Descuento, Itbis,
            NFCTipoNumero, RNC, Abono, Tiempo_Credito, Total,
            Cantidad_Articulos, Nota, Estado, Usuario, Fecha_Registro,
            Literal, Cotizacion_Numero, ID_Garantia
        )
        SELECT
            @Nuevo_Numero_Factura,
            ID_Cliente,
            ID_Tipo_Factura,
            ID_Vendedor,
            ID_Tipo_Pagos,
            ID_Sucursal,
            SubTotal,
            Descuento,
            Itbis,
            NFCTipoNumero,
            RNC,
            Abono,
            Tiempo_Credito,
            Total,
            Cantidad_Articulos,
            Nota,
            'A',
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE()),
            Literal,
            Cotizacion_Numero,
            -- Garantia: la de la prefactura; si no la tiene (prefacturas anteriores), la de sus lineas; si no, 7 = No Aplica
            COALESCE(ID_Garantia,
                     (SELECT MAX(d.ID_Garantia) FROM PreDetalle_Facturas d
                       WHERE d.Numero_PreFactura = @Numero_PreFactura AND d.Estado = 'P'),
                     7)
        FROM PreFacturas
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se pudo insertar la cabecera de la factura';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        INSERT INTO Detalle_Facturas
        (
            Numero_Factura, ID_Equipo, Descripcion, ID_Tipo,
            ID_Garantia, cantidad, precio, SubTotal, Descuento,
            LiteralF, Itbis, Total, Estado, Usuario,
            Fecha_registro, Costo_Equipo
        )
        SELECT
            @Nuevo_Numero_Factura,
            pf.ID_Equipo,
            pf.Descripcion,
            pf.ID_Tipo,
            pf.ID_Garantia,
            pf.cantidad,
            pf.precio,
            pf.SubTotal,
            pf.Descuento,
            CONCAT(p.Literal, @Nuevo_Numero_Factura),
            pf.Itbis,
            pf.Total,
            'A',
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE()),
            pf.Costo_Equipo
        FROM PreDetalle_Facturas pf
        INNER JOIN PreFacturas p
            ON pf.Numero_PreFactura = p.Numero_PreFactura
        WHERE p.Numero_PreFactura = @Numero_PreFactura
          AND pf.Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se encontraron detalles para procesar';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        -- Copiar pagos mixtos P -> F sin modificar la prefactura
        IF @TipoPago = 6
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM DetallesPagos_Factura
                WHERE Numero_Factura = @Numero_PreFactura
                  AND Tipo_Documento = 'P'
                  AND Estado = 'A'
            )
            BEGIN
                SET @Nuevo_Numero_Factura = 0;
                SET @Mensaje = 'Error: La prefactura Mixta no tiene detalles de pago';
                ROLLBACK TRANSACTION;
                SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
                RETURN;
            END;

            EXEC dbo.CopiarDetallesPagoPrefacturaAFactura
                @Numero_PreFactura = @Numero_PreFactura,
                @Numero_Factura = @Nuevo_Numero_Factura;
        END;

        -- Retencion de tarjeta: % y monto de la factura recien creada (ya tiene sus pagos)
        DECLARE @SucursalFactura INT =
            (SELECT TOP (1) ID_Sucursal FROM Facturas WHERE Numero_Factura = @Nuevo_Numero_Factura);

        EXEC dbo.Factura_Calcular_Retencion_Tarjeta
            @Numero_Factura = @Nuevo_Numero_Factura,
            @ID_Sucursal = @SucursalFactura;

        INSERT INTO Equipos_Historico
        (
            Imei, ID_Modelo, ID_Tecnologia, ID_Garantia,
            ID_Condicion, ID_Estado_Bloqueo, ID_Almacen,
            ID_Suplidor, Numero_de_Factura, Precio_Por_Mayor,
            Precio_Detalle, Costo_Equipo, Desbloqueado,
            Disponible, Nota_Adicional, Nota_Historico,
            Usuario, Fecha_registro, Numero_Invetario,
            Numero_Registro, Usuario_Historico,
            Fecha_registro_Historico
        )
        SELECT
            Imei,
            ID_Modelo,
            ID_Tecnologia,
            ID_Garantia,
            ID_Condicion,
            ID_Estado_Bloqueo,
            ID_Almacen,
            ID_Suplidor,
            Numero_de_Factura,
            Precio_Por_Mayor,
            Precio_Detalle,
            Costo_Equipo,
            Desbloqueado,
            Disponible,
            Nota_Adicional,
            'Vendido en factura #' + CONVERT(VARCHAR(50), @Nuevo_Numero_Factura),
            Usuario,
            Fecha_registro,
            Numero_Invetario,
            Numero_Registro,
            @Usuario_Procesamiento,
            DATEADD(HOUR, -4, GETDATE())
        FROM Equipos WITH (NOLOCK)
        WHERE Imei IN
        (
            SELECT DISTINCT ID_Equipo
            FROM PreDetalle_Facturas
            WHERE Numero_PreFactura = @Numero_PreFactura
              AND ID_Tipo = 1
              AND Estado = 'P'
        );

        UPDATE Equipos
        SET Disponible = 'N',
            Nota_Adicional = 'Vendido en factura #' +
                CONVERT(VARCHAR(50), @Nuevo_Numero_Factura),
            Numero_de_Factura = @Nuevo_Numero_Factura
        WHERE Imei IN
        (
            SELECT DISTINCT ID_Equipo
            FROM PreDetalle_Facturas
            WHERE Numero_PreFactura = @Numero_PreFactura
              AND ID_Tipo = 1
              AND Estado = 'P'
        );

        UPDATE a
        SET a.Cantidad = a.Cantidad - pf.cantidad
        FROM Accesorios a
        INNER JOIN PreDetalle_Facturas pf
            ON a.ID_Accesorio = pf.ID_Equipo
        WHERE pf.Numero_PreFactura = @Numero_PreFactura
          AND pf.ID_Tipo = 2
          AND pf.Estado = 'P';

        UPDATE Reparaciones
        SET ID_Estado_Taller = 3,
            SubTotal = pf.SubTotal,
            Descuento = pf.Descuento,
            Itbis = pf.Itbis,
            Numero_Factura = @Nuevo_Numero_Factura
        FROM PreDetalle_Facturas pf
        WHERE CONVERT(INT, pf.ID_Equipo) = Reparaciones.ID_Reparacion
          AND pf.Numero_PreFactura = @Numero_PreFactura
          AND pf.ID_Tipo IN (3, 4)
          AND pf.Estado = 'P';

        UPDATE PreFacturas
        SET Estado = 'C',
            Fecha_Procesamiento = DATEADD(HOUR, -4, GETDATE()),
            Usuario_Procesamiento = @Usuario_Procesamiento
        WHERE Numero_PreFactura = @Numero_PreFactura
          AND Estado = 'P';

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            SET @Nuevo_Numero_Factura = 0;
            SET @Mensaje = 'Error: No se pudo actualizar el estado de la PreFactura';
            ROLLBACK TRANSACTION;
            SELECT @Nuevo_Numero_Factura AS Cod_Factura, @Mensaje AS Mensaje;
            RETURN;
        END;

        -- El detalle de la prefactura tambien pasa a 'C'
        UPDATE PreDetalle_Facturas
           SET Estado = 'C'
         WHERE Numero_PreFactura = @Numero_PreFactura
           AND Estado = 'P';

        COMMIT TRANSACTION;

        SET @Mensaje =
            'Factura creada satisfactoriamente. Factura #' +
            CONVERT(VARCHAR(50), @Nuevo_Numero_Factura);
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Nuevo_Numero_Factura = 0;
        SET @Mensaje = 'ERROR: ' + ERROR_MESSAGE();
    END CATCH;

    SELECT
        @Nuevo_Numero_Factura AS Cod_Factura,
        @Mensaje AS Mensaje;
END;
GO

-- #####################################################################
-- 4. UPDATE_Proc_Facturas_Consulta_Retencion.sql
-- #####################################################################
-- ============================================================
-- Retencion de tarjeta en la Consulta de facturas
-- Proc_Facturas_Consulta devuelve ademas MontoRetencionTarjeta (por factura) y TotalRetencionTarjeta
-- (suma con el mismo filtro que TotalItbis). Parametros, filtros y paginacion no cambian.
-- Requiere: CREATE_RETENCION_TARJETA.sql.
-- Idempotente. Ejecutar en cada base (principal y espejo).
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[Proc_Facturas_Consulta]
    @PageIndex INT = 1,
    @PageSize INT = 10,
    @Criterio VARCHAR(100) = NULL,
    @Almacen INT = 0,
    @Vendedor INT = 0,
    @Comprobante INT = 1,
    @Fecha_Inicio DATETIME = NULL,
    @Fecha_Final DATETIME = NULL,
    @EstadoFactura CHAR(1) = NULL,
    @IdTipoPago INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @Almacen = 4 SET @Almacen = 0;
    IF @PageIndex < 1 SET @PageIndex = 1;
    IF @Fecha_Inicio IS NULL OR @Fecha_Inicio = '' SET @Fecha_Inicio = CAST(GETDATE() AS DATE);
    IF @Fecha_Final IS NULL OR @Fecha_Final = '' SET @Fecha_Final = CAST(GETDATE() AS DATE);

    DECLARE @BusquedaGlobal BIT = CASE
        WHEN NULLIF(LTRIM(RTRIM(@Criterio)), '') IS NULL THEN 0
        ELSE 1
    END;
    SET @Criterio = NULLIF(LTRIM(RTRIM(@Criterio)), '');

    -- Calcular totales agregados
    DECLARE @TotalFacturado MONEY;
    DECLARE @TotalItbis MONEY;
    DECLARE @TotalSinItbis MONEY;
    DECLARE @TotalRetencionTarjeta MONEY;

    SELECT @TotalFacturado = SUM(F.Total),
           @TotalItbis = SUM(F.Itbis),
           @TotalSinItbis = SUM(F.SubTotal),
           @TotalRetencionTarjeta = SUM(F.Monto_Retencion_Tarjeta)
    FROM Facturas F
    INNER JOIN Clientes C ON F.ID_Cliente = C.ID_Cliente
    INNER JOIN Contactos Co ON C.ID_Contacto = Co.ID_Contacto
    INNER JOIN Telefonos te ON te.ID_Contacto = C.ID_Contacto
    INNER JOIN Tipos_Facturas_Cata TF ON F.ID_Tipo_Factura = TF.ID_Tipo_Factura
    INNER JOIN Tipos_Pagos_Cata TP ON F.ID_Tipo_Pagos = TP.ID_Tipo_Pagos
    INNER JOIN Sucursales_cata SC ON SC.id_sucursal = F.id_sucursal
    LEFT JOIN TIPO_NFC TN ON TN.NFCTipoNumero = F.NFCTipoNumero
    LEFT JOIN Vendedores v ON v.ID_Vendedor = F.ID_Vendedor
    LEFT JOIN Contactos CS ON CS.ID_Contacto = v.ID_Contacto
    WHERE
        (@Criterio IS NULL OR (Co.Nombres + ' ' + Co.Apellidos LIKE '%' + @Criterio + '%')
            OR F.Numero_Factura LIKE @Criterio
            OR te.Numero_Telefono LIKE @Criterio
            OR TF.Descripcion_Factura_Tipo = @Criterio)
        AND (@BusquedaGlobal = 1 OR @Almacen = 0 OR F.id_sucursal = @Almacen)
        AND (@Vendedor = 0 OR F.ID_Vendedor = @Vendedor)
        AND (@IdTipoPago = 0 OR F.ID_Tipo_Pagos = @IdTipoPago)
        AND (@BusquedaGlobal = 1 OR (f.Fecha_Registro >= @Fecha_Inicio
            AND f.Fecha_Registro < DATEADD(DAY, 1, @Fecha_Final)))
        AND (@EstadoFactura IS NULL OR F.Estado = @EstadoFactura)
        AND (@Comprobante <> 2 OR F.NFCTipoNumero <> 0);

    ;WITH FacturasCTE AS (
        SELECT
            F.Numero_Factura AS [Numero de Factura],
            Co.Nombres,
            Co.Apellidos,
            F.ID_Cliente,
            te.Numero_Telefono AS Telefono,
            TF.Descripcion_Factura_Tipo AS [Tipo De Factura],
            ISNULL(CS.Nombres, '') AS [Vendedor],
            TP.Descripcion_Tipo_Pagos AS [Tipo de Pago],
            SC.Sucursal_Descripcion AS Almacen,
            F.ID_Sucursal AS IdSucursal,
            F.Abono,
            F.SubTotal,
            F.Descuento,
            F.NCF_NUMERO AS NCF,
            F.NFCTipoNumero,
            TN.NFCTipoDescripcion AS TipoComprobante,
            F.RNC,
            F.Total,
            F.Itbis,
            F.Monto_Retencion_Tarjeta AS MontoRetencionTarjeta,
            F.Nota,
            F.Motivo_Anulacion AS MotivoAnulacion,
            F.Usuario_Anulacion AS UsuarioAnulacion,
            F.Fecha_Anulacion AS FechaAnulacion,
            CAST(F.Fecha_registro AS DATE) AS [Fecha de la Factura],
            F.Estado AS EstadoFactura,
            @TotalFacturado AS TotalFacturado,
            @TotalItbis AS TotalItbis,
            @TotalSinItbis AS TotalSinItbis,
            @TotalRetencionTarjeta AS TotalRetencionTarjeta,
            ROW_NUMBER() OVER (ORDER BY F.Fecha_registro DESC) AS Linea,
            COUNT(*) OVER() AS Cantidad_Registros
        FROM Facturas F
        INNER JOIN Clientes C ON F.ID_Cliente = C.ID_Cliente
        INNER JOIN Contactos Co ON C.ID_Contacto = Co.ID_Contacto
        INNER JOIN Telefonos te ON te.ID_Contacto = C.ID_Contacto
        INNER JOIN Tipos_Facturas_Cata TF ON F.ID_Tipo_Factura = TF.ID_Tipo_Factura
        INNER JOIN Tipos_Pagos_Cata TP ON F.ID_Tipo_Pagos = TP.ID_Tipo_Pagos
        INNER JOIN Sucursales_cata SC ON SC.id_sucursal = F.id_sucursal
        LEFT JOIN TIPO_NFC TN ON TN.NFCTipoNumero = F.NFCTipoNumero
        LEFT JOIN Vendedores v ON v.ID_Vendedor = F.ID_Vendedor
        LEFT JOIN Contactos CS ON CS.ID_Contacto = v.ID_Contacto
        WHERE
            (@Criterio IS NULL OR (Co.Nombres + ' ' + Co.Apellidos LIKE '%' + @Criterio + '%')
                OR F.Numero_Factura LIKE @Criterio
                OR te.Numero_Telefono LIKE @Criterio
                OR TF.Descripcion_Factura_Tipo = @Criterio)
            AND (@BusquedaGlobal = 1 OR @Almacen = 0 OR F.id_sucursal = @Almacen)
            AND (@Vendedor = 0 OR F.ID_Vendedor = @Vendedor)
            AND (@IdTipoPago = 0 OR F.ID_Tipo_Pagos = @IdTipoPago)
            AND (@BusquedaGlobal = 1 OR (f.Fecha_Registro >= @Fecha_Inicio
                AND f.Fecha_Registro < DATEADD(DAY, 1, @Fecha_Final)))
            AND (@EstadoFactura IS NULL OR F.Estado = @EstadoFactura)
            AND (@Comprobante <> 2 OR F.NFCTipoNumero <> 0)
    )
    SELECT *,
        CASE
            WHEN Cantidad_Registros < (@PageIndex * @PageSize)
                THEN Cantidad_Registros
            ELSE (@PageIndex * @PageSize)
        END AS Ultima_Linea
    FROM FacturasCTE
    WHERE Linea BETWEEN ((@PageIndex - 1) * @PageSize) + 1
                    AND (@PageIndex * @PageSize)
    ORDER BY Linea
    OPTION (RECOMPILE);

END
GO

-- #####################################################################
-- 5. UPDATE_Ver_Utilidades_Precio_Detalle_Retencion.sql
-- #####################################################################
-- ============================================================
-- Utilidades (Ver_Utilidades_Precio_Detalle): retencion de tarjeta + cuadre de numeros
-- Cambios:
--  * Retencion_Tarjeta: suma de Facturas.Monto_Retencion_Tarjeta de las facturas de venta del periodo.
--  * Fecha: todos los bloques filtran por Facturas.Fecha_registro (rango sin cast sobre la columna),
--    igual que la retencion; antes usaban Detalle_Facturas.fecha_registro.
--  * Equipos: se quita el join con Equipos y el filtro Disponible = 'N'; la venta la definen la linea y la
--    factura activas. Costo e ITBIS con coalesce para que Ganancia = Venta - Costo - ITBIS.
--  * Accesorios: costo igual que antes (costo del accesorio * cantidad); coalesce en ITBIS/costo.
--  * Reparaciones: sin filas duplicadas (una reparacion por linea) y ganancia = Venta - Costo - ITBIS.
--    Se tratan las columnas como se comportan hoy (Reparaciones.Costo es el costo); sin renombrar.
--  * Desbloqueos: logica sin cambios (pendiente); solo cambia el filtro de fecha.
-- Requiere: CREATE_RETENCION_TARJETA.sql.
-- Idempotente. Ejecutar en cada base (principal y espejo).
-- ============================================================

-- Comprobacion previa: si faltan las columnas, no se crea el SP (SET NOEXEC ON hasta el GO del SP).
IF COL_LENGTH('dbo.Reparaciones', 'Costo') IS NULL
   OR COL_LENGTH('dbo.Detalle_Facturas', 'Costo_Equipo') IS NULL
BEGIN
    RAISERROR('Ver_Utilidades_Precio_Detalle NO se creo: falta la columna Reparaciones.Costo o Detalle_Facturas.Costo_Equipo en esta base. Revisar el esquema.', 16, 1);
    SET NOEXEC ON;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Ver_Utilidades_Precio_Detalle]
@Sucursal int = null,
@Fecha_Inicio datetime,
@Fecha_Final datetime
as
begin
Set DateFormat MDY
	select * from
(
    --Ganancia en Equipo
    select
        coalesce(sum(coalesce(df.costo_equipo, 0)), 0) as Costo_Equipos,
        coalesce(sum(df.total), 0) as Venta_Equipos,
        coalesce(sum(coalesce(df.Itbis, 0)), 0) as Itbis_Equipos,
        coalesce(sum(df.total - coalesce(df.costo_equipo, 0) - coalesce(df.Itbis, 0)), 0) as Ganancia_Equipos
    from  Detalle_Facturas df
    inner join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura
                              and df.literalF = ft.literal + convert(varchar(10), ft.Numero_Factura)
    where
        ft.estado = 'A'
        and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal)
		and ft.ID_Tipo_Pagos <> 2
		and df.id_tipo in (1)
		and ft.ID_Tipo_Factura <> 3
        and ft.Fecha_registro >= convert(date, @Fecha_Inicio) and ft.Fecha_registro < dateadd(day, 1, convert(date, @Fecha_Final))
        and df.Estado = 'A'

) as EQUIPOS
	, (
		--Ganancia en Accesorio
		select coalesce(sum(coalesce(ac.costo_equipo, 0) * df.cantidad), 0)                                         as Costo_Accesorios
			 , coalesce(sum(df.total), 0)                                                                            as Venta_Accesorios
			 , coalesce(sum(coalesce(df.Itbis, 0)), 0)                                                               as Itbis_Accesorios
			 , coalesce(sum(df.total - (coalesce(ac.costo_equipo, 0) * df.cantidad) - coalesce(df.Itbis, 0)), 0)     as Ganancia_Accesorios
		  from Detalle_Facturas df
		 inner join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura
		 inner join Accesorios ac on df.id_equipo = ac.id_accesorio
		 where df.estado = 'A'
		     and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal)
		   and ft.ID_Tipo_Pagos <> 2
		   and df.id_tipo = 2
		   and ft.ID_Tipo_Factura <> 3
		   and df.literalF = ft.literal + convert(varchar(10), ft.numero_factura)
		   and ft.Fecha_registro >= convert(date, @Fecha_Inicio) and ft.Fecha_registro < dateadd(day, 1, convert(date, @Fecha_Final))
		   and df.Estado = 'A'
		   and ft.Estado = 'A') as ACCESORIOS
	, (
		--Ganancia en reparaciones (una reparacion por linea: por ID_Reparacion o por Imei)
		select coalesce(sum(coalesce(rp.Costo, 0) * coalesce(df.cantidad, 1)), 0)                                              as Costo_Reparaciones
			 , coalesce(sum(df.Total), 0)                                                                                      as Venta_Reparaciones
			 , coalesce(sum(coalesce(df.Itbis, 0)), 0)                                                                         as Itbis_Reparaciones
			 , coalesce(sum(df.Total - coalesce(rp.Costo, 0) * coalesce(df.cantidad, 1) - coalesce(df.Itbis, 0)), 0)           as Ganancia_Reparaciones
		  from Detalle_Facturas df
		 inner join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura and df.literalF = ft.literal + convert(varchar(10), ft.Numero_Factura)
		 cross apply (select top (1) r.Costo
		                from dbo.Reparaciones r
		               where r.Numero_Factura = ft.Numero_Factura
		                 and r.Estado = 'A'
		                 and (df.ID_Equipo = convert(varchar(30), r.ID_Reparacion) or df.ID_Equipo = convert(varchar(30), r.Imei))
		               order by case when df.ID_Equipo = convert(varchar(30), r.ID_Reparacion) then 0 else 1 end, r.ID_Reparacion) rp
		 where df.Estado = 'A'
		   and ft.Estado = 'A'
		   and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal)
		   and ft.ID_Tipo_Pagos <> 2
		   and df.ID_Tipo in (3, 4)
		   and ft.ID_Tipo_Factura = 4
		   and ft.Fecha_registro >= convert(date, @Fecha_Inicio) and ft.Fecha_registro < dateadd(day, 1, convert(date, @Fecha_Final))) as REPARACIONES
	, (
		--Ganancia en desbloqueo (logica sin cambios; pendiente de revisar)
		select coalesce(sum(d.SubTotal * df.cantidad), 0)                       as Costo_Desbloqueos
			 , coalesce(sum(df.total), 0)                                       as Venta_Desbloqueos
			 , coalesce(sum(df.Itbis), 0)                                       as Itbis_Desbloqueos
			 , coalesce(sum(df.total - (d.SubTotal * df.cantidad) - df.Itbis), 0) as Ganancia_Desbloqueos
		  from Detalle_Facturas df
		 inner join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura
		 inner join dbo.Desbloqueos d on df.id_equipo = d.ID_Desbloqueos and d.numero_factura = ft.numero_factura
		 where df.estado = 'A'
		     and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal)
		   and ft.ID_Tipo_Pagos <> 2
		   and df.id_tipo = 5
		   and ft.ID_Tipo_Factura <> 3
		   and df.literalF = ft.literal + convert(varchar(10), ft.numero_factura)
		   and ft.Fecha_registro >= convert(date, @Fecha_Inicio) and ft.Fecha_registro < dateadd(day, 1, convert(date, @Fecha_Final))
		   and df.Estado = 'A' And d.Estado = 'A') as DESBLOQUEOS
	, (
		-- Abonos recibidos durante el período en facturas a crédito.
		-- Se presentan por separado; no se suman las ventas completas ni se atribuyen a una categoría.
		select coalesce(sum(try_convert(decimal(18, 2), hp.Total_Abono)), 0) as Abonos_Credito
		  from dbo.Historio_Pagos hp
		 inner join dbo.Facturas ft
		    on ft.Numero_Factura = try_convert(int, hp.Numero_factura)
		   and ft.ID_Sucursal = hp.Id_Sucursal
		   and ft.ID_Cliente = hp.id_Cliente_Suplidor
		 where ft.Estado = 'A'
		   and ft.ID_Tipo_Pagos = 2
		   and ft.ID_Tipo_Factura <> 3
		   --and hp.Tipo_Factura = 1
		   and hp.Id_Sucursal = coalesce(@Sucursal, hp.Id_Sucursal)
		   and hp.Total_Abono > 0
		   and hp.Fecha_registro >= convert(date, @Fecha_Inicio) and hp.Fecha_registro < dateadd(day, 1, convert(date, @Fecha_Final))
	) as ABONOS_CREDITO
	, (
		-- Retencion de tarjeta de las facturas de venta del periodo (a nivel de factura, sin prorrateo)
		select coalesce(sum(ft.Monto_Retencion_Tarjeta), 0) as Retencion_Tarjeta
		  from dbo.Facturas ft
		 where ft.Estado = 'A'
		   and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal)
		   and ft.ID_Tipo_Pagos <> 2
		   and ft.ID_Tipo_Factura <> 3
		   and ft.Fecha_registro >= convert(date, @Fecha_Inicio) and ft.Fecha_registro < dateadd(day, 1, convert(date, @Fecha_Final))
	) as RETENCION
end
GO

-- Restaura la ejecucion normal (si la comprobacion previa la desactivo, las secciones siguientes corren igual).
SET NOEXEC OFF;
GO

-- Verificacion (comentada): por categoria, Venta - Costo - ITBIS - Ganancia debe dar 0.
-- Cambiar las fechas; la sucursal NULL = todas.
-- DECLARE @FI datetime = '2026-11-01', @FF datetime = '2026-11-30';
-- CREATE TABLE #u (Costo_Equipos decimal(18,2), Venta_Equipos decimal(18,2), Itbis_Equipos decimal(18,2), Ganancia_Equipos decimal(18,2),
--                  Costo_Accesorios decimal(18,2), Venta_Accesorios decimal(18,2), Itbis_Accesorios decimal(18,2), Ganancia_Accesorios decimal(18,2),
--                  Costo_Reparaciones decimal(18,2), Venta_Reparaciones decimal(18,2), Itbis_Reparaciones decimal(18,2), Ganancia_Reparaciones decimal(18,2),
--                  Costo_Desbloqueos decimal(18,2), Venta_Desbloqueos decimal(18,2), Itbis_Desbloqueos decimal(18,2), Ganancia_Desbloqueos decimal(18,2),
--                  Abonos_Credito decimal(18,2), Retencion_Tarjeta decimal(18,2));
-- INSERT INTO #u EXEC dbo.Ver_Utilidades_Precio_Detalle @Sucursal = NULL, @Fecha_Inicio = @FI, @Fecha_Final = @FF;
-- SELECT 'Equipos' AS Categoria, Venta_Equipos - Costo_Equipos - Itbis_Equipos - Ganancia_Equipos AS Diferencia FROM #u
-- UNION ALL SELECT 'Accesorios', Venta_Accesorios - Costo_Accesorios - Itbis_Accesorios - Ganancia_Accesorios FROM #u
-- UNION ALL SELECT 'Reparaciones', Venta_Reparaciones - Costo_Reparaciones - Itbis_Reparaciones - Ganancia_Reparaciones FROM #u
-- UNION ALL SELECT 'Desbloqueos', Venta_Desbloqueos - Costo_Desbloqueos - Itbis_Desbloqueos - Ganancia_Desbloqueos FROM #u;
-- DROP TABLE #u;

-- #####################################################################
-- 6. UPDATE_Ver_Utilidades_Abonos_Credito_Detalle_Retencion.sql
-- #####################################################################
-- ============================================================
-- Retencion de tarjeta en el detalle de abonos de Utilidades
-- Ver_Utilidades_Abonos_Credito_Detalle agrega RetencionAbono (Historio_Pagos.Monto_Retencion_Tarjeta)
-- y RetencionFactura (en el abono que salda: retencion de todos los abonos de la factura).
-- GananciaAbono no cambia; el front le resta RetencionFactura.
-- Requiere: CREATE_RETENCION_TARJETA.sql.
-- Idempotente. Ejecutar en cada base (principal y espejo).
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Ver_Utilidades_Abonos_Credito_Detalle
    @Sucursal INT = NULL,
    @Fecha_Inicio DATETIME,
    @Fecha_Final DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        hp.Id,
        hp.id_Cliente_Suplidor AS IdCliente,
        COALESCE(
            NULLIF(LTRIM(RTRIM(CONCAT(COALESCE(co.Nombres, ''), ' ', COALESCE(co.Apellidos, '')))), ''),
            CONCAT('Cliente #', hp.id_Cliente_Suplidor)
        ) AS Cliente,
        ft.Numero_Factura AS NumeroFactura,
        hp.Fecha_registro AS FechaAbono,
        TRY_CONVERT(DECIMAL(18, 2), hp.Total_Abono) AS MontoAbono,
        CONVERT(DECIMAL(18, 2), hp.Monto_Retencion_Tarjeta) AS RetencionAbono,
        -- Abono que salda: retencion de todos los abonos activos de la factura hasta este (no es ganancia)
        CONVERT(DECIMAL(18, 2), CASE WHEN hp.Pendiente <= 0 THEN retFactura.Retencion ELSE 0 END) AS RetencionFactura,
        CASE WHEN COALESCE(saldoActual.Pendiente, hp.Pendiente, 0) <= 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS FacturaSaldada,
        CASE WHEN hp.Pendiente <= 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS EsAbonoQueSaldaFactura,
        costo.CostoFactura,
        CONVERT(DECIMAL(18, 2), ft.Total) AS TotalFactura,
        CONVERT(DECIMAL(18, 2), CASE
            WHEN hp.Pendiente <= 0 THEN ft.Total - ft.Itbis - costo.CostoFactura
            ELSE 0
        END) AS GananciaAbono,
        hp.Usuario,
        hp.Nota
    FROM dbo.Historio_Pagos hp
    INNER JOIN dbo.Facturas ft
        ON ft.Numero_Factura = TRY_CONVERT(INT, hp.Numero_factura)
       AND ft.ID_Sucursal = hp.Id_Sucursal
       AND ft.ID_Cliente = hp.id_Cliente_Suplidor
    LEFT JOIN dbo.Clientes cl
        ON cl.ID_Cliente = ft.ID_Cliente
    LEFT JOIN dbo.Contactos co
        ON co.ID_Contacto = cl.ID_Contacto
    OUTER APPLY (
        SELECT COALESCE(SUM(COALESCE(df.Costo_Equipo, 0)), 0) AS CostoFactura
        FROM dbo.Detalle_Facturas df
        WHERE df.Numero_Factura = ft.Numero_Factura
          AND df.LiteralF = ft.literal + CONVERT(VARCHAR(10), ft.Numero_Factura)
          AND df.Estado = 'A'
    ) costo
        OUTER APPLY (
                SELECT TOP (1) hpActual.Pendiente
                FROM dbo.Historio_Pagos hpActual
                WHERE TRY_CONVERT(INT, hpActual.Numero_factura) = ft.Numero_Factura
                    AND hpActual.Id_Sucursal = ft.ID_Sucursal
                    AND hpActual.id_Cliente_Suplidor = ft.ID_Cliente
                    AND hpActual.Estado = 'A'
                ORDER BY hpActual.Fecha_registro DESC, hpActual.Id DESC
        ) saldoActual
    OUTER APPLY (
        SELECT COALESCE(SUM(COALESCE(hpR.Monto_Retencion_Tarjeta, 0)), 0) AS Retencion
        FROM dbo.Historio_Pagos hpR
        WHERE TRY_CONVERT(INT, hpR.Numero_factura) = ft.Numero_Factura
          AND hpR.Id_Sucursal = ft.ID_Sucursal
          AND hpR.id_Cliente_Suplidor = ft.ID_Cliente
          AND hpR.Estado = 'A'
          AND hpR.Id <= hp.Id
    ) retFactura
    WHERE ft.Estado = 'A'
      AND ft.ID_Tipo_Pagos = 2
      AND ft.ID_Tipo_Factura <> 3
      AND hp.Id_Sucursal = COALESCE(@Sucursal, hp.Id_Sucursal)
      AND hp.Total_Abono > 0
      AND hp.Fecha_registro >= CONVERT(DATE, @Fecha_Inicio)
      AND hp.Fecha_registro < DATEADD(DAY, 1, CONVERT(DATE, @Fecha_Final))
    ORDER BY hp.Fecha_registro DESC, hp.Id DESC;
END;
GO

-- #####################################################################
-- 7. UPDATE_Proc_Detalle_Factura_Elimina_Equipo.sql
-- #####################################################################
-- =====================================================================
-- Rebaja de equipo en factura (Proc_Detalle_Factura_Elimina_Equipo)
-- Plan Retencion tarjeta, Parte 0.
--   1. Ajusta la cabecera de la factura con cualquier forma de pago (antes solo contado y credito).
--   2. Pago mixto: baja el monto rebajado de DetallesPagos_Factura, primero el efectivo (ID_Tipo_Pago = 1)
--      y despues el otro pago. Ningun pago queda negativo; la suma de pagos sigue igual al total.
--   3. ITBIS con centavos (antes DECIMAL sin decimales lo redondeaba a entero).
--   4. Si el equipo ya no esta activo en la factura (doble rebaja), avisa y no toca nada.
--   5. Todo en una transaccion.
-- Idempotente (CREATE OR ALTER). Primero en MV_SYSTEM; al desplegar, principal y espejo.
-- =====================================================================

CREATE OR ALTER PROCEDURE [dbo].[Proc_Detalle_Factura_Elimina_Equipo]
    @imei VARCHAR(30),
    @numero_Factura INT,
    @IdSucursal INT,
    @Usuario varchar(60)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Obtener literal de la factura
    DECLARE @literal CHAR(1) = (
        SELECT literal
        FROM facturas
        WHERE id_sucursal = @IdSucursal AND numero_factura = @numero_Factura
    );

    DECLARE @literalF VARCHAR(10) = @literal + CONVERT(VARCHAR(10), @numero_Factura);

    -- Obtener datos del detalle de factura
    DECLARE @total MONEY,
            @sub_total MONEY,
            @costo_equipo MONEY,
            @descuento MONEY,
            @itbis MONEY,
            @tipo_Pago INT;

    SELECT TOP 1
        @total = ISNULL(Total, 0),
        @sub_total = ISNULL(SubTotal, 0),
        @descuento = ISNULL(Descuento, 0),
        @itbis = ISNULL(Itbis, 0),
        @costo_equipo = Costo_Equipo
    FROM Detalle_Facturas
    WHERE Id_Equipo = @imei
      AND LiteralF = @literalF
      AND Estado = 'A';

    -- Doble rebaja o equipo que no esta en la factura: no se toca nada
    IF @total IS NULL
    BEGIN
        SELECT 'El equipo con IMEI ' + @imei + ' ya no está activo en la factura #' + CONVERT(VARCHAR(10), @numero_Factura) + '; no se hizo ningún cambio.' AS mensaje;
        RETURN;
    END

    -- Obtener tipo de pago
    SELECT @tipo_Pago = ID_Tipo_Pagos
    FROM Facturas
    WHERE Numero_Factura = @numero_Factura
      AND ID_Sucursal = @IdSucursal;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Eliminar el detalle de factura
        UPDATE Detalle_Facturas
        SET Estado = 'X'
        WHERE Id_Equipo = @imei
          AND LiteralF = @literalF
          AND Estado = 'A';

        -- Insertar en histórico del equipo
        INSERT INTO [dbo].[Equipos_Historico] (
            [Imei], [ID_Modelo], [ID_Tecnologia], [ID_Garantia],
            [ID_Condicion], [ID_Estado_Bloqueo], [ID_Almacen],
            [ID_Suplidor], [Numero_de_Factura], [Precio_Por_Mayor],
            [Precio_Detalle], [Costo_Equipo], [Desbloqueado],
            [Disponible], [Nota_Adicional], Nota_Historico,
            [Usuario], [Fecha_registro], [Numero_Invetario],
            [Numero_Registro], Usuario_Historico, Fecha_registro_Historico
        )
        SELECT
            [Imei], [ID_Modelo], [ID_Tecnologia], [ID_Garantia],
            [ID_Condicion], [ID_Estado_Bloqueo], [ID_Almacen],
            [ID_Suplidor], [Numero_de_Factura], [Precio_Por_Mayor],
            [Precio_Detalle], [Costo_Equipo], [Desbloqueado],
            [Disponible], [Nota_Adicional],
            'Devuelto de la FT # ' + CONVERT(VARCHAR(8), @numero_Factura),
            [Usuario], [Fecha_registro], [Numero_Invetario],
            [Numero_Registro], @Usuario, DATEADD(HOUR, -4, GETDATE())
        FROM Equipos WITH (NOLOCK)
        WHERE Imei = @imei;

        -- Devolver equipo al inventario
        UPDATE Equipos
        SET Disponible = 'S',
            Nota_Adicional = 'Devuelto de la FT # ' + CONVERT(VARCHAR(8), @numero_Factura),
            Usuario = @Usuario
        WHERE Imei = @imei
          AND Disponible = 'N';

        -- Ajustar totales de la factura (cualquier forma de pago)
        UPDATE Facturas
        SET SubTotal = ISNULL(SubTotal, 0) - @sub_total,
            Descuento = ISNULL(Descuento, 0) - @descuento,
            Itbis = ISNULL(Itbis, 0) - @itbis,
            Total = ISNULL(Total, 0) - @total,
            Usuario = @Usuario
        WHERE Numero_Factura = @numero_Factura
          AND Estado = 'A'
          AND ID_Sucursal = @IdSucursal;

        -- Pago mixto: bajar el monto de los pagos, primero el efectivo y despues el otro pago
        IF @tipo_Pago = 6
        BEGIN
            DECLARE @restante MONEY = @total,
                    @idPago INT,
                    @montoPago MONEY,
                    @baja MONEY;

            WHILE @restante > 0
            BEGIN
                SET @idPago = NULL;

                SELECT TOP 1
                    @idPago = ID_Detalle_Pago,
                    @montoPago = Monto_Pago
                FROM DetallesPagos_Factura
                WHERE Numero_Factura = @numero_Factura
                  AND Tipo_Documento = 'F'
                  AND Estado = 'A'
                  AND Monto_Pago > 0
                ORDER BY CASE WHEN ID_Tipo_Pago = 1 THEN 0 ELSE 1 END, ID_Detalle_Pago DESC;

                IF @idPago IS NULL BREAK;

                SET @baja = CASE WHEN @montoPago < @restante THEN @montoPago ELSE @restante END;

                UPDATE DetallesPagos_Factura
                SET Monto_Pago = Monto_Pago - @baja
                WHERE ID_Detalle_Pago = @idPago;

                SET @restante = @restante - @baja;
            END
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    -- Retornar mensaje de éxito
    SELECT 'El equipo con IMEI ' + @imei + ' fue rebajado correctamente de la factura #' + CONVERT(VARCHAR(10), @numero_Factura) AS mensaje;
END;
GO

-- #####################################################################
-- 8. CONFIGURAR_RETENCION_TARJETA.sql
-- #####################################################################
-- =====================================================================
-- Configurar el % de retencion de la tarjeta (un solo % por tienda).
-- Ejecutar en CADA base: principal y espejo (si no, la espejo calcula 0).
-- 1. Ver los tipos de pago y buscar el ID de la tarjeta (en MV_SYSTEM es 7; revisar en cada base).
-- 2. Quitar los comentarios del UPDATE y poner el ID y el % reales.
-- Solo afecta facturas y abonos nuevos; los anteriores no se recalculan.
-- =====================================================================

SELECT * FROM dbo.Tipos_Pagos_Cata;

--UPDATE dbo.Tipos_Pagos_Cata
--   SET Porcentaje_Retencion = 4.50
-- WHERE ID_Tipo_Pagos = <id tarjeta>;
