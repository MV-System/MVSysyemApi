-- ============================================================
-- Feature: Tareas pendientes - Cancelar una reparación abierta
-- Propósito:
--   1. Estado de taller 'Cancelado' en Estados_Taller_Cata (ID 4).
--   2. Columnas de auditoría en Reparaciones: Motivo_Cancelacion, Usuario_Cancelacion,
--      Fecha_Cancelacion (opcionales; las reparaciones existentes quedan en NULL).
--   3. SP Reparacion_Cancelar:
--        - Solo cancela reparaciones activas (Estado = 'A') en estado Pendiente (1) o En Proceso (2).
--        - No cancela si ya tiene factura (Numero_Factura) ni si está en una prefactura pendiente.
--        - El motivo es obligatorio (mínimo 5 caracteres).
--        - Cambia ID_Estado_Taller a 'Cancelado' y guarda motivo, usuario y fecha.
--        - No borra la reparación: sigue en la lista con estatus 'Cancelado'.
--        - Una reparación cancelada deja de salir para facturar porque
--          SP_Reparaciones_consulta_facturacion solo lista ID_Estado_Taller = 1.
--   Transaccional. Script idempotente.
-- Orden: ejecutar este script antes de UPDATE_Reparaciones_Consulta_Cancelacion.sql.
-- Autor: MVSystem
-- Fecha: 2026-10-28
-- ============================================================

-- ------------------------------------------------------------
-- 1. Estado 'Cancelado'
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Estados_Taller_Cata WHERE Descripcion_Estado_Taller = 'Cancelado')
BEGIN
    DECLARE @NuevoID INT = (SELECT ISNULL(MAX(ID_Estado_Taller), 0) + 1 FROM dbo.Estados_Taller_Cata);

    IF OBJECTPROPERTY(OBJECT_ID('dbo.Estados_Taller_Cata'), 'TableHasIdentity') = 1
    BEGIN
        SET IDENTITY_INSERT dbo.Estados_Taller_Cata ON;

        INSERT INTO dbo.Estados_Taller_Cata
            (ID_Estado_Taller, Descripcion_Estado_Taller, Detalle_Estado_Taller, Estado, Usuario, Fecha_registro)
        VALUES
            (@NuevoID, 'Cancelado', 'La reparacion fue cancelada antes de completarse', 'A', 'MVSystem', GETDATE());

        SET IDENTITY_INSERT dbo.Estados_Taller_Cata OFF;
    END
    ELSE
        INSERT INTO dbo.Estados_Taller_Cata
            (ID_Estado_Taller, Descripcion_Estado_Taller, Detalle_Estado_Taller, Estado, Usuario, Fecha_registro)
        VALUES
            (@NuevoID, 'Cancelado', 'La reparacion fue cancelada antes de completarse', 'A', 'MVSystem', GETDATE());
END
GO

-- ------------------------------------------------------------
-- 2. Columnas de auditoría de la cancelación
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Reparaciones', 'Motivo_Cancelacion') IS NULL
    ALTER TABLE dbo.Reparaciones ADD Motivo_Cancelacion VARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.Reparaciones', 'Usuario_Cancelacion') IS NULL
    ALTER TABLE dbo.Reparaciones ADD Usuario_Cancelacion VARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.Reparaciones', 'Fecha_Cancelacion') IS NULL
    ALTER TABLE dbo.Reparaciones ADD Fecha_Cancelacion DATETIME NULL;
GO

-- ------------------------------------------------------------
-- 3. SP Reparacion_Cancelar
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Reparacion_Cancelar
    @ID_Reparacion BIGINT,
    @Motivo        VARCHAR(500),
    @Usuario       VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50500, 'El usuario es requerido.', 1;

        IF @Motivo IS NULL OR LEN(LTRIM(RTRIM(@Motivo))) < 5
            THROW 50501, 'Debe indicar el motivo de la cancelacion (minimo 5 caracteres).', 1;

        DECLARE @ID_Estado_Cancelado INT =
            (SELECT TOP 1 ID_Estado_Taller FROM dbo.Estados_Taller_Cata WHERE Descripcion_Estado_Taller = 'Cancelado');

        IF @ID_Estado_Cancelado IS NULL
            THROW 50508, 'Falta el estado Cancelado en Estados_Taller_Cata. Ejecute CREATE_Reparacion_Cancelar.sql.', 1;

        BEGIN TRAN;

        DECLARE @Estado CHAR(1), @ID_Estado_Taller INT, @Numero_Factura INT;

        SELECT @Estado = R.Estado,
               @ID_Estado_Taller = R.ID_Estado_Taller,
               @Numero_Factura = R.Numero_Factura
        FROM dbo.Reparaciones R WITH (UPDLOCK, ROWLOCK)
        WHERE R.ID_Reparacion = @ID_Reparacion;

        IF @Estado IS NULL
            THROW 50502, 'La reparacion no existe.', 1;

        IF @Estado <> 'A'
            THROW 50503, 'La reparacion esta inactiva y no se puede cancelar.', 1;

        IF @ID_Estado_Taller = @ID_Estado_Cancelado
            THROW 50504, 'La reparacion ya esta cancelada.', 1;

        IF @ID_Estado_Taller NOT IN (1, 2)
            THROW 50505, 'Solo se pueden cancelar reparaciones en estado Pendiente o En Proceso.', 1;

        IF ISNULL(@Numero_Factura, 0) > 0
            THROW 50506, 'La reparacion ya esta facturada y no se puede cancelar.', 1;

        -- Si esta en una prefactura pendiente, primero hay que procesarla o anularla
        DECLARE @PreFactura_Pendiente INT = (
            SELECT TOP 1 pf.Numero_PreFactura
              FROM dbo.PreDetalle_Facturas pf
              INNER JOIN dbo.PreFacturas p ON p.Numero_PreFactura = pf.Numero_PreFactura
             WHERE pf.ID_Equipo = CONVERT(VARCHAR(30), @ID_Reparacion)
               AND pf.ID_Tipo IN (3, 4) AND pf.Estado = 'P' AND p.Estado = 'P'
             ORDER BY pf.Numero_PreFactura DESC);

        IF @PreFactura_Pendiente IS NOT NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La reparacion esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de cancelar la reparacion.');
            THROW 50507, @MsgPre, 1;
        END

        UPDATE dbo.Reparaciones
           SET ID_Estado_Taller   = @ID_Estado_Cancelado,
               Motivo_Cancelacion = LTRIM(RTRIM(@Motivo)),
               Usuario_Cancelacion = @Usuario,
               Fecha_Cancelacion  = DATEADD(HOUR, -4, GETDATE())
         WHERE ID_Reparacion = @ID_Reparacion
           AND ID_Estado_Taller IN (1, 2);

        IF @@ROWCOUNT = 0
            THROW 50509, 'La reparacion fue modificada por otro usuario. Actualice la lista e intente de nuevo.', 1;

        COMMIT TRAN;

        SELECT R.ID_Reparacion,
               Estatus = ET.Descripcion_Estado_Taller,
               R.Motivo_Cancelacion,
               R.Usuario_Cancelacion,
               R.Fecha_Cancelacion,
               Mensaje = 'Reparacion cancelada satisfactoriamente'
          FROM dbo.Reparaciones R
          INNER JOIN dbo.Estados_Taller_Cata ET ON ET.ID_Estado_Taller = R.ID_Estado_Taller
         WHERE R.ID_Reparacion = @ID_Reparacion;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO
