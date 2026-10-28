-- ============================================================
-- Feature: Módulo de Laptops - Fase 4, Tarea 12
-- Propósito: SP Equipo_Cambio_Pieza_Recibir. Confirma que la pieza retirada
--            que estaba en custodia del técnico fue recibida:
--            - Solo desde Estado_Custodia = 'PENDIENTE'.
--            - La recibe un usuario DISTINTO al técnico (R9).
--            - Suma 1 al accesorio retirado en el almacén del cambio.
--            - Marca el cambio como 'RECIBIDA' con usuario, fecha y nota.
--            Transaccional (R13).
-- Nota: Accesorios tiene una fila por (accesorio, almacén). Si el accesorio retirado
--       pertenece a otro almacén, se busca el equivalente en el almacén del cambio por
--       código de barras o por modelo + descripción.
-- Requiere: CREATE_Equipo_Cambio_Pieza.sql
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Recibir
    @ID_Cambio INT,
    @Usuario   VARCHAR(60),
    @Nota      VARCHAR(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50100, 'El usuario que recibe es requerido.', 1;

        BEGIN TRAN;

        DECLARE @Estado VARCHAR(20), @Tecnico VARCHAR(60), @ID_Almacen INT, @ID_Accesorio_Retirado BIGINT;

        SELECT @Estado = C.Estado_Custodia,
               @Tecnico = C.Tecnico,
               @ID_Almacen = C.ID_Almacen,
               @ID_Accesorio_Retirado = C.ID_Accesorio_Retirado
        FROM dbo.Equipo_Cambio_Pieza C WITH (UPDLOCK, ROWLOCK)
        WHERE C.ID_Cambio = @ID_Cambio;

        IF @Estado IS NULL
            THROW 50101, 'El cambio de pieza no existe.', 1;

        IF @Estado <> 'PENDIENTE'
            THROW 50102, 'Solo se puede recibir una pieza en estado PENDIENTE.', 1;

        -- R9: la recepcion la confirma un usuario distinto al tecnico
        IF LTRIM(RTRIM(@Usuario)) = LTRIM(RTRIM(@Tecnico))
            THROW 50103, 'La recepcion debe confirmarla un usuario distinto al tecnico que retiro la pieza.', 1;

        IF @ID_Accesorio_Retirado IS NULL
            THROW 50104, 'El cambio no tiene pieza retirada.', 1;

        -- --------------------------------------------------------
        -- Fila del accesorio retirado en el almacen del cambio
        -- --------------------------------------------------------
        DECLARE @ID_Accesorio_Destino BIGINT;

        SELECT @ID_Accesorio_Destino = A.ID_Accesorio
        FROM dbo.Accesorios A
        WHERE A.ID_Accesorio = @ID_Accesorio_Retirado
          AND A.ID_Almacen = @ID_Almacen;

        IF @ID_Accesorio_Destino IS NULL
        BEGIN
            -- El accesorio retirado es de otro almacen: buscar el equivalente en el almacen del cambio
            SELECT TOP 1 @ID_Accesorio_Destino = D.ID_Accesorio
            FROM dbo.Accesorios O
            INNER JOIN dbo.Accesorios D
                ON D.ID_Almacen = @ID_Almacen
               AND ((O.Codigo_Barra IS NOT NULL AND O.Codigo_Barra <> 0 AND D.Codigo_Barra = O.Codigo_Barra)
                    OR (D.ID_Modelo = O.ID_Modelo AND D.Descripcion_Accesorio = O.Descripcion_Accesorio))
            WHERE O.ID_Accesorio = @ID_Accesorio_Retirado
            ORDER BY D.ID_Accesorio;
        END

        IF @ID_Accesorio_Destino IS NULL
            THROW 50105, 'El accesorio retirado no esta registrado en el almacen del cambio. Registrelo en ese almacen antes de recibirlo.', 1;

        -- --------------------------------------------------------
        -- Sumar 1 al inventario y marcar como recibida
        -- --------------------------------------------------------
        UPDATE dbo.Accesorios
           SET Cantidad = ISNULL(Cantidad, 0) + 1
         WHERE ID_Accesorio = @ID_Accesorio_Destino;

        UPDATE dbo.Equipo_Cambio_Pieza
           SET Estado_Custodia = 'RECIBIDA',
               Usuario_Recepcion = @Usuario,
               Fecha_Recepcion = GETDATE(),
               Nota_Recepcion = @Nota
         WHERE ID_Cambio = @ID_Cambio
           AND Estado_Custodia = 'PENDIENTE';

        IF @@ROWCOUNT = 0
            THROW 50106, 'La pieza ya fue procesada por otro usuario.', 1;

        COMMIT TRAN;

        SELECT C.*, Mensaje = 'Pieza recibida satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO
