-- ============================================================
-- Feature: Tareas pendientes - Cancelar una cotizacion
-- Proposito:
--   1. Columnas de auditoria en Cotizacion: Usuario_Cancelacion y Fecha_Cancelacion
--      (opcionales; las cotizaciones existentes quedan en NULL). No se pide motivo.
--   2. SP Cotizacion_Cancelar:
--        - Solo cancela cotizaciones activas (Estado = 'A') que NO estan facturadas (facturado = 'N').
--        - Deja la cotizacion en Estado = 'X' (no se borra: se puede consultar y reimprimir).
--        - No toca el detalle ni el inventario (las cotizaciones no mueven inventario).
--   3. Cotizacion_Facturar_Consulta (cotizaciones disponibles para facturar) ya no lista las
--      canceladas: se agrega AND c.Estado = 'A'. Todo lo demas queda identico.
--   La lista de cotizaciones oculta las canceladas porque el front envia @Estado = 'A'
--   (o 'X' con "Ver canceladas"); Cotizacion_Consulta no cambia.
--   Script idempotente.
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================

-- ------------------------------------------------------------
-- 1. Columnas de auditoria de la cancelacion
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.Cotizacion', 'Usuario_Cancelacion') IS NULL
    ALTER TABLE dbo.Cotizacion ADD Usuario_Cancelacion VARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.Cotizacion', 'Fecha_Cancelacion') IS NULL
    ALTER TABLE dbo.Cotizacion ADD Fecha_Cancelacion DATETIME NULL;
GO

-- ------------------------------------------------------------
-- 2. SP Cotizacion_Cancelar
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Cotizacion_Cancelar
    @Codigo  INT,
    @Usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50600, 'El usuario es requerido.', 1;

        BEGIN TRAN;

        DECLARE @Estado CHAR(1), @Facturado CHAR(1);

        SELECT @Estado = C.Estado,
               @Facturado = C.facturado
        FROM dbo.Cotizacion C WITH (UPDLOCK, ROWLOCK)
        WHERE C.Codigo = @Codigo;

        IF @Estado IS NULL
            THROW 50601, 'La cotizacion no existe.', 1;

        IF @Estado = 'X'
            THROW 50602, 'La cotizacion ya esta cancelada.', 1;

        IF @Estado <> 'A'
            THROW 50603, 'La cotizacion no esta activa y no se puede cancelar.', 1;

        IF @Facturado <> 'N'
            THROW 50604, 'La cotizacion ya fue facturada y no se puede cancelar.', 1;

        UPDATE dbo.Cotizacion
           SET Estado = 'X',
               Usuario_Cancelacion = @Usuario,
               Fecha_Cancelacion = DATEADD(HOUR, -4, GETDATE())
         WHERE Codigo = @Codigo
           AND Estado = 'A'
           AND facturado = 'N';

        IF @@ROWCOUNT = 0
            THROW 50605, 'La cotizacion fue modificada por otro usuario. Actualice la lista e intente de nuevo.', 1;

        COMMIT TRAN;

        SELECT C.Codigo,
               C.Numero_Factura AS NumeroFactura,
               C.Estado,
               C.Usuario_Cancelacion AS UsuarioCancelacion,
               C.Fecha_Cancelacion AS FechaCancelacion,
               Mensaje = 'Cotizacion cancelada satisfactoriamente'
          FROM dbo.Cotizacion C
         WHERE C.Codigo = @Codigo;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- 3. Cotizacion_Facturar_Consulta: no ofrecer cotizaciones canceladas
--    (unico cambio: AND c.Estado = 'A')
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[Cotizacion_Facturar_Consulta]--'8293808751'
@Numero_Telefono varchar(15)
as
select c.Numero_Factura,
Cliente = co.Nombres +' '+co.Apellidos,
c.SubTotal,
c.Descuento,
c.Total,
c.Itbis,
c.Abono
from Cotizacion c
inner join Clientes cl on cl.ID_Cliente = c.ID_Cliente
inner join Telefonos t on t.ID_Contacto = cl.ID_Contacto
inner join Contactos co on co.ID_Contacto = cl.ID_Contacto
where t.Numero_Telefono = @Numero_Telefono
and c.facturado = 'N'
and c.Estado = 'A'
GO
