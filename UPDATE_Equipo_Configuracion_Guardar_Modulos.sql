-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 2 (modulos por laptop)
-- Proposito: Equipo_Configuracion_Guardar acepta la lista de modulos de la laptop en el parametro NUEVO y
--            OPCIONAL @Modulos (JSON):
--              [{"idModulo":null,"tipoPieza":"RAM","capacidadGB":8,"idModeloComponente":12}, ...]
--            - idModulo: solo lo envia el API al replicar en la espejo (mismo ID que en la base de trabajo).
--            - Sin @Modulos, los parametros actuales (@Ram_GB, @Ram_Tipo, @Almacenamiento_GB,
--              @Almacenamiento_Tipo) siguen funcionando: se crea 1 modulo de RAM y 1 de almacenamiento.
--            - Reemplaza TODOS los modulos de la laptop. Se rechaza si la laptop ya tiene cambios de pieza
--              (para corregir una laptop con historial se usa Equipo_Configuracion_Modulo_Corregir).
--            - Equipo_Configuracion queda como RESUMEN (Equipo_Configuracion_Recalcular).
-- Requiere: CREATE_Componentes_Especificacion.sql y CREATE_Equipo_Configuracion_Modulo.sql ejecutados ANTES.
--           Usa OPENJSON: la base debe tener nivel de compatibilidad 130 (SQL Server 2016) o superior.
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER).
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================
IF (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) < 130
    PRINT 'ATENCION: la base tiene nivel de compatibilidad < 130; OPENJSON no funcionara. Suba el nivel de compatibilidad.';
GO

CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Guardar
    @Imei                VARCHAR(30),
    @Ram_GB              INT = NULL,
    @Ram_Tipo            VARCHAR(30) = NULL,
    @Almacenamiento_GB   INT = NULL,
    @Almacenamiento_Tipo VARCHAR(30) = NULL,
    @Notas               VARCHAR(250) = NULL,
    @Usuario             VARCHAR(60),
    @Modulos             NVARCHAR(MAX) = NULL      -- NUEVO: lista de modulos en JSON
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- R2: se obtiene el ID del grupo por descripcion una sola vez y se compara por ID.
    DECLARE @ID_Grupo_Laptops INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');
    DECLARE @ID_Modelo INT;

    IF @ID_Grupo_Laptops IS NULL
    BEGIN
        RAISERROR('No existe el grupo Laptops. Ejecute CREATE_Laptops_Fase1_Grupos.sql', 16, 1);
        RETURN;
    END

    SELECT TOP 1 @ID_Modelo = ID_Modelo FROM dbo.Equipos WHERE Imei = @Imei;

    IF @ID_Modelo IS NULL
    BEGIN
        RAISERROR('El equipo con serial/IMEI %s no existe.', 16, 1, @Imei);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Modelos WHERE ID_Modelo = @ID_Modelo AND ID_Grupo = @ID_Grupo_Laptops)
    BEGIN
        RAISERROR('El equipo %s no es una laptop (su modelo no pertenece al grupo Laptops).', 16, 1, @Imei);
        RETURN;
    END

    -- --------------------------------------------------------
    -- Lista de modulos (JSON, o armada con los parametros actuales)
    -- --------------------------------------------------------
    DECLARE @Mod TABLE
    (
        Orden        INT IDENTITY(1, 1) PRIMARY KEY,
        ID_Modulo    INT NULL,
        Tipo_Pieza   VARCHAR(20) NOT NULL,
        Capacidad_GB INT NOT NULL,
        ID_Modelo    INT NOT NULL
    );

    IF @Modulos IS NOT NULL
    BEGIN
        IF ISJSON(@Modulos) <> 1
            THROW 50520, 'El formato de los modulos no es un JSON valido.', 1;

        INSERT INTO @Mod (ID_Modulo, Tipo_Pieza, Capacidad_GB, ID_Modelo)
        SELECT j.idModulo, UPPER(j.tipoPieza), j.capacidadGB, j.idModeloComponente
          FROM OPENJSON(@Modulos)
               WITH (idModulo           INT         '$.idModulo',
                     tipoPieza          VARCHAR(20) '$.tipoPieza',
                     capacidadGB        INT         '$.capacidadGB',
                     idModeloComponente INT         '$.idModeloComponente') AS j;

        IF NOT EXISTS (SELECT 1 FROM @Mod)
            THROW 50521, 'Debe indicar al menos un modulo (memoria o disco).', 1;
    END
    ELSE
    BEGIN
        -- Compatibilidad: un modulo por RAM y uno por almacenamiento. El modelo sale del tipo
        -- (SSD SATA -> 'SSD SATA 2.5' por defecto).
        IF @Ram_GB IS NOT NULL
        BEGIN
            DECLARE @ModeloRam INT;
            SELECT TOP 1 @ModeloRam = T.ID_Modelo
              FROM dbo.Componente_Modelo_Tipo T
             WHERE T.Tipo_Pieza = 'RAM' AND T.Tipo = @Ram_Tipo
             ORDER BY T.ID_Modelo;

            IF @ModeloRam IS NULL
                THROW 50522, 'El tipo de RAM es requerido y debe ser DDR3, DDR4 o DDR5.', 1;

            INSERT INTO @Mod (ID_Modulo, Tipo_Pieza, Capacidad_GB, ID_Modelo) VALUES (NULL, 'RAM', @Ram_GB, @ModeloRam);
        END

        IF @Almacenamiento_GB IS NOT NULL
        BEGIN
            DECLARE @ModeloDisco INT;
            SELECT TOP 1 @ModeloDisco = T.ID_Modelo
              FROM dbo.Componente_Modelo_Tipo T
              INNER JOIN dbo.Modelos Mo ON Mo.ID_Modelo = T.ID_Modelo
             WHERE T.Tipo_Pieza = 'ALMACENAMIENTO' AND T.Tipo = @Almacenamiento_Tipo
             ORDER BY CASE WHEN Mo.Descripcion = 'SSD SATA 2.5' THEN 0 ELSE 1 END, T.ID_Modelo;

            IF @ModeloDisco IS NULL
                THROW 50523, 'El tipo de almacenamiento es requerido y debe ser SSD SATA, SSD NVMe o HDD.', 1;

            INSERT INTO @Mod (ID_Modulo, Tipo_Pieza, Capacidad_GB, ID_Modelo) VALUES (NULL, 'ALMACENAMIENTO', @Almacenamiento_GB, @ModeloDisco);
        END
    END

    -- --------------------------------------------------------
    -- Validaciones de los modulos
    -- --------------------------------------------------------
    IF EXISTS (SELECT 1 FROM @Mod)
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza WHERE Imei = @Imei)
            THROW 50524, 'La laptop ya tiene cambios de pieza: su configuracion no se puede reemplazar. Use la correccion de modulos.', 1;

        IF EXISTS (SELECT 1 FROM @Mod WHERE Tipo_Pieza NOT IN ('RAM', 'ALMACENAMIENTO'))
            THROW 50525, 'El tipo de pieza de un modulo debe ser RAM o ALMACENAMIENTO.', 1;

        IF EXISTS (SELECT 1 FROM @Mod WHERE Capacidad_GB IS NULL OR Capacidad_GB <= 0 OR Capacidad_GB > 16000)
            THROW 50526, 'La capacidad de cada modulo debe estar entre 1 y 16000 GB.', 1;

        IF EXISTS (SELECT 1
                     FROM @Mod X
                     LEFT JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo AND T.Tipo_Pieza = X.Tipo_Pieza
                    WHERE T.ID_Modelo IS NULL)
            THROW 50527, 'Un modulo tiene un modelo de componente invalido o que no corresponde a su tipo de pieza.', 1;

        IF (SELECT COUNT(DISTINCT T.Tipo)
              FROM @Mod X
              INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo
             WHERE X.Tipo_Pieza = 'RAM') > 1
            THROW 50528, 'No se pueden mezclar tipos de RAM (DDR3 / DDR4 / DDR5) en una misma laptop.', 1;

        IF EXISTS (SELECT ID_Modulo FROM @Mod WHERE ID_Modulo IS NOT NULL GROUP BY ID_Modulo HAVING COUNT(*) > 1)
            THROW 50529, 'La lista de modulos repite un ID de modulo.', 1;

        IF EXISTS (SELECT 1
                     FROM @Mod X
                    INNER JOIN dbo.Equipo_Configuracion_Modulo M ON M.ID_Modulo = X.ID_Modulo
                    WHERE M.Imei <> @Imei)
            THROW 50530, 'Error: un ID de modulo ya lo usa otra laptop.', 1;
    END

    -- --------------------------------------------------------
    -- Guardar
    -- --------------------------------------------------------
    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion WHERE Imei = @Imei)
    BEGIN
        UPDATE dbo.Equipo_Configuracion
        SET Ram_GB              = @Ram_GB,
            Ram_Tipo            = @Ram_Tipo,
            Almacenamiento_GB   = @Almacenamiento_GB,
            Almacenamiento_Tipo = @Almacenamiento_Tipo,
            Notas               = @Notas,
            Usuario_Modifica    = @Usuario,
            Fecha_Modifica      = GETDATE()
        WHERE Imei = @Imei;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Equipo_Configuracion
            (Imei, Ram_GB, Ram_Tipo, Almacenamiento_GB, Almacenamiento_Tipo, Notas, Usuario, Fecha_Registro)
        VALUES
            (@Imei, @Ram_GB, @Ram_Tipo, @Almacenamiento_GB, @Almacenamiento_Tipo, @Notas, @Usuario, GETDATE());
    END

    IF EXISTS (SELECT 1 FROM @Mod)
    BEGIN
        DELETE FROM dbo.Equipo_Configuracion_Modulo WHERE Imei = @Imei;

        -- Modulos nuevos: ID automatico, en el orden recibido
        INSERT INTO dbo.Equipo_Configuracion_Modulo
            (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
        SELECT @Imei, X.Tipo_Pieza, X.Capacidad_GB, X.ID_Modelo, T.Tipo, 'A', @Usuario, GETDATE()
          FROM @Mod X
         INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo
         WHERE X.ID_Modulo IS NULL
         ORDER BY X.Orden;

        -- Replicacion: mismo ID que en la base de trabajo
        IF EXISTS (SELECT 1 FROM @Mod WHERE ID_Modulo IS NOT NULL)
        BEGIN
            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo ON;

            INSERT INTO dbo.Equipo_Configuracion_Modulo
                (ID_Modulo, Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
            SELECT X.ID_Modulo, @Imei, X.Tipo_Pieza, X.Capacidad_GB, X.ID_Modelo, T.Tipo, 'A', @Usuario, GETDATE()
              FROM @Mod X
             INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = X.ID_Modelo
             WHERE X.ID_Modulo IS NOT NULL
             ORDER BY X.Orden;

            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo OFF;
        END

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;
    END

    COMMIT TRAN;

    SELECT C.ID_Configuracion, C.Imei, C.Ram_GB, C.Ram_Tipo, C.Almacenamiento_GB, C.Almacenamiento_Tipo,
           C.Notas, dbo.fn_Equipo_Configuracion_Texto(C.Imei) AS Configuracion,
           Mensaje = 'Configuracion guardada satisfactoriamente'
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;
END
GO
