-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 6 (anular un cambio de pieza)
-- Proposito: revertir un cambio de pieza registrado por error.
--   1. Esquema (solo se AGREGA): Equipo_Cambio_Pieza recibe Precio_Detalle_Anterior, Precio_Por_Mayor_Anterior,
--      Usuario_Anula, Fecha_Anula y Motivo_Anula (todas NULL); la restriccion de Estado_Custodia admite 'ANULADO'.
--   2. SP Equipo_Cambio_Pieza_Anular @ID_Cambio, @Usuario, @Motivo (transaccional):
--        - Solo el ULTIMO cambio de esa laptop (que no este anulado), laptop disponible y sin prefactura pendiente.
--        - Solo si la pieza retirada esta PENDIENTE o NO_APLICA. Si ya fue recibida o entregada al cliente ese
--          movimiento ya afecto el inventario / el costo: mensaje claro, no se anula.
--        - Revierte: +1 al accesorio instalado, Costo_Equipo = Costo_Equipo_Anterior (y los precios, si el cambio
--          los guardo), modulos (el instalado pasa a 'R', el retirado vuelve a 'A'), Equipo_Configuracion
--          (Equipo_Configuracion_Recalcular) y deja una foto en Equipos_Historico.
--        - Marca el cambio como 'ANULADO' con usuario, fecha y motivo.
--        - El accesorio de la pieza retirada que el cambio haya creado en el catalogo se conserva (no afecta inventario).
--   3. SP Equipo_Cambio_Pieza_Historial: agrega Puede_Anular, Es_Ultimo, Laptop_Disponible y los datos de anulacion.
-- Requiere: scripts de las Tareas 2 y 3 (modulos) ejecutados ANTES.
-- Despues ejecutar UPDATE_Equipo_Cambio_Pieza_Registrar_Anulable.sql (guarda los precios anteriores al registrar).
-- Ejecutar en TODAS las bases. Script idempotente.
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================

-- ------------------------------------------------------------
-- 1. Esquema
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Equipo_Cambio_Pieza', 'Precio_Detalle_Anterior') IS NULL
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD Precio_Detalle_Anterior DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.Equipo_Cambio_Pieza', 'Precio_Por_Mayor_Anterior') IS NULL
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD Precio_Por_Mayor_Anterior DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.Equipo_Cambio_Pieza', 'Usuario_Anula') IS NULL
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD Usuario_Anula VARCHAR(60) NULL;
GO
IF COL_LENGTH('dbo.Equipo_Cambio_Pieza', 'Fecha_Anula') IS NULL
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD Fecha_Anula DATETIME NULL;
GO
IF COL_LENGTH('dbo.Equipo_Cambio_Pieza', 'Motivo_Anula') IS NULL
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD Motivo_Anula VARCHAR(250) NULL;
GO

-- La restriccion de Estado_Custodia pasa a admitir 'ANULADO'
IF EXISTS (SELECT 1 FROM sys.check_constraints
            WHERE name = 'CK_Equipo_Cambio_Pieza_Estado'
              AND parent_object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza')
              AND definition NOT LIKE '%ANULADO%')
BEGIN
    ALTER TABLE dbo.Equipo_Cambio_Pieza DROP CONSTRAINT CK_Equipo_Cambio_Pieza_Estado;
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD CONSTRAINT CK_Equipo_Cambio_Pieza_Estado
        CHECK (Estado_Custodia IN ('PENDIENTE', 'RECIBIDA', 'ENTREGADA_CLIENTE', 'NO_APLICA', 'ANULADO'));
END
GO

-- ------------------------------------------------------------
-- 2. Anular
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Anular
    @ID_Cambio INT,
    @Usuario   VARCHAR(60),
    @Motivo    VARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50600, 'El usuario que anula es requerido.', 1;

        IF @Motivo IS NULL OR LTRIM(RTRIM(@Motivo)) = ''
            THROW 50601, 'El motivo de la anulacion es requerido.', 1;

        BEGIN TRAN;

        DECLARE @Imei VARCHAR(30), @Estado VARCHAR(20), @ID_Almacen INT, @ID_Accesorio_Instalado BIGINT,
                @Mod_Ret INT, @Mod_Inst INT, @Costo_Ant DECIMAL(18,2),
                @PrecioDet_Ant DECIMAL(18,2), @PrecioMay_Ant DECIMAL(18,2);

        SELECT @Imei = C.Imei, @Estado = C.Estado_Custodia, @ID_Almacen = C.ID_Almacen,
               @ID_Accesorio_Instalado = C.ID_Accesorio_Instalado,
               @Mod_Ret = C.ID_Modulo_Retirado, @Mod_Inst = C.ID_Modulo_Instalado,
               @Costo_Ant = C.Costo_Equipo_Anterior,
               @PrecioDet_Ant = C.Precio_Detalle_Anterior, @PrecioMay_Ant = C.Precio_Por_Mayor_Anterior
          FROM dbo.Equipo_Cambio_Pieza C WITH (UPDLOCK, ROWLOCK)
         WHERE C.ID_Cambio = @ID_Cambio;

        IF @Imei IS NULL
            THROW 50602, 'El cambio de pieza no existe.', 1;

        IF @Estado = 'ANULADO'
            THROW 50603, 'El cambio de pieza ya fue anulado.', 1;

        IF @Estado NOT IN ('PENDIENTE', 'NO_APLICA')
            THROW 50604, 'No se puede anular: la pieza retirada ya fue recibida o entregada al cliente, y ese movimiento ya afecto el inventario o el costo.', 1;

        IF EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza X
                    WHERE X.Imei = @Imei AND X.ID_Cambio > @ID_Cambio AND X.Estado_Custodia <> 'ANULADO')
            THROW 50605, 'Solo se puede anular el ultimo cambio de pieza de la laptop.', 1;

        DECLARE @Disponible CHAR(1);
        SELECT TOP 1 @Disponible = E.Disponible
          FROM dbo.Equipos E WITH (UPDLOCK, ROWLOCK)
         WHERE E.Imei = @Imei;

        IF @Disponible IS NULL OR @Disponible <> 'S'
            THROW 50606, 'La laptop no esta disponible (vendida o no disponible). No se puede anular el cambio.', 1;

        DECLARE @PreFactura_Pendiente INT = (
            SELECT TOP 1 pf.Numero_PreFactura
              FROM dbo.PreDetalle_Facturas pf
              INNER JOIN dbo.PreFacturas p ON p.Numero_PreFactura = pf.Numero_PreFactura
             WHERE pf.ID_Equipo = @Imei AND pf.ID_Tipo = 1 AND pf.Estado = 'P' AND p.Estado = 'P'
             ORDER BY pf.Numero_PreFactura DESC);

        IF @PreFactura_Pendiente IS NOT NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La laptop esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de anular el cambio de pieza.');
            THROW 50607, @MsgPre, 1;
        END

        -- Foto de la fila actual en el historico de equipos (antes de revertir costo / precios)
        INSERT INTO dbo.Equipos_Historico
            (Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
             Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
             Nota_Historico, Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
             Usuario_Historico, Fecha_registro_Historico, Comision_Detalle, Comision_Por_Mayor)
        SELECT Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
               Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
               'Anulacion de cambio de pieza de laptop: se restablece costo y/o precios', Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
               @Usuario, DATEADD(HOUR, -4, GETDATE()), Comision_Detalle, Comision_Por_Mayor
          FROM dbo.Equipos
         WHERE Imei = @Imei;

        -- Costo (y precios, si el cambio los guardo) vuelven a como estaban
        UPDATE dbo.Equipos
           SET Costo_Equipo     = @Costo_Ant,
               Precio_Detalle   = ISNULL(@PrecioDet_Ant, Precio_Detalle),
               Precio_Por_Mayor = ISNULL(@PrecioMay_Ant, Precio_Por_Mayor)
         WHERE Imei = @Imei;

        -- La pieza instalada vuelve al inventario del almacen del cambio
        UPDATE dbo.Accesorios
           SET Cantidad = ISNULL(Cantidad, 0) + 1
         WHERE ID_Accesorio = @ID_Accesorio_Instalado
           AND ID_Almacen = @ID_Almacen;

        -- Modulos: el instalado pasa a retirado y el que salio vuelve a estar instalado
        IF @Mod_Inst IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET Estado = 'R' WHERE ID_Modulo = @Mod_Inst;

        IF @Mod_Ret IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET Estado = 'A', ID_Cambio_Baja = NULL WHERE ID_Modulo = @Mod_Ret;

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;

        UPDATE dbo.Equipo_Cambio_Pieza
           SET Estado_Custodia = 'ANULADO',
               Usuario_Anula   = @Usuario,
               Fecha_Anula     = GETDATE(),
               Motivo_Anula    = LTRIM(RTRIM(@Motivo))
         WHERE ID_Cambio = @ID_Cambio;

        COMMIT TRAN;

        SELECT C.*, Configuracion = dbo.fn_Equipo_Configuracion_Texto(C.Imei),
               Mensaje = 'Cambio de pieza anulado satisfactoriamente'
          FROM dbo.Equipo_Cambio_Pieza C
         WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- 3. Historial (agrega los datos de anulacion y si se puede anular)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Historial
    @Imei VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Cambio,
           C.Imei,
           C.Tipo_Pieza,
           C.ID_Accesorio_Instalado,
           Pieza_Instalada = AI.Descripcion_Accesorio,
           C.Costo_Instalado,
           C.ID_Accesorio_Retirado,
           Pieza_Retirada = AR.Descripcion_Accesorio,
           C.Costo_Retirado,
           C.Config_Anterior,
           C.Config_Nueva,
           C.Costo_Equipo_Anterior,
           C.Costo_Equipo_Nuevo,
           C.ID_Almacen,
           C.ID_PreFactura,
           C.ID_Reparacion,
           C.Tecnico,
           C.Estado_Custodia,
           C.Usuario_Registro,
           C.Fecha_Registro,
           C.Usuario_Recepcion,
           C.Fecha_Recepcion,
           C.Nota_Recepcion,
           -- NUEVO
           Es_Ultimo = CAST(CASE WHEN C.Estado_Custodia <> 'ANULADO'
                                  AND NOT EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza X
                                                   WHERE X.Imei = C.Imei AND X.ID_Cambio > C.ID_Cambio AND X.Estado_Custodia <> 'ANULADO')
                                 THEN 1 ELSE 0 END AS BIT),
           Laptop_Disponible = E.Disponible,
           Puede_Anular = CAST(CASE WHEN C.Estado_Custodia IN ('PENDIENTE', 'NO_APLICA') AND E.Disponible = 'S'
                                     AND NOT EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza X
                                                      WHERE X.Imei = C.Imei AND X.ID_Cambio > C.ID_Cambio AND X.Estado_Custodia <> 'ANULADO')
                                    THEN 1 ELSE 0 END AS BIT),
           C.Usuario_Anula,
           C.Fecha_Anula,
           C.Motivo_Anula
    FROM dbo.Equipo_Cambio_Pieza C
    LEFT JOIN dbo.Accesorios AI ON AI.ID_Accesorio = C.ID_Accesorio_Instalado
    LEFT JOIN dbo.Accesorios AR ON AR.ID_Accesorio = C.ID_Accesorio_Retirado
    LEFT JOIN dbo.Equipos E ON E.Imei = C.Imei
    WHERE C.Imei = @Imei
    ORDER BY C.Fecha_Registro DESC, C.ID_Cambio DESC;
END
GO
