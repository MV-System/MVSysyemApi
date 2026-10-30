-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 2 (modulos por laptop)
-- Proposito: guardar la configuracion de una laptop POR MODULO (1x16 vs 2x8 de RAM, SSD + HDD).
--            1. dbo.Equipo_Configuracion_Modulo: un registro por cada memoria / disco de la laptop.
--            2. dbo.Equipo_Configuracion_Correccion: log de correcciones manuales de modulos.
--            3. SP Equipo_Configuracion_Recalcular: deja Equipo_Configuracion como RESUMEN de los modulos
--               (Ram_GB = suma de memorias activas, Almacenamiento_GB = suma de discos activos).
--            4. SP Equipo_Configuracion_Modulos_Consultar / Equipo_Configuracion_Correcciones_Consultar /
--               Componente_Modelos_Consultar (catalogo de modelos de componentes para las pantallas).
--            5. SP Equipo_Configuracion_Modulo_Corregir: corrige modelo y/o capacidad de un modulo activo
--               (sin mover inventario ni costo) y deja registro.
--            6. Migracion idempotente: cada laptop con configuracion y sin modulos recibe 1 modulo de RAM
--               y 1 de almacenamiento. Un SSD SATA antiguo se migra como 'SSD SATA 2.5' (el registro viejo
--               no distinguia 2.5 de M.2; se corrige con Equipo_Configuracion_Modulo_Corregir).
-- Requiere: CREATE_Componentes_Especificacion.sql (Tarea 1) ejecutado ANTES.
-- Orden: 1) este script  2) UPDATE_fn_Equipo_Configuracion_Texto_Modulos.sql
--        3) UPDATE_Equipo_Configuracion_Guardar_Modulos.sql
-- Ejecutar en TODAS las bases (principal y espejo de cada empresa).
-- Script idempotente: solo agrega objetos y datos; no borra ni modifica la configuracion existente.
--
-- IDs de los modulos: deben ser iguales en principal y espejo. Los modulos NUEVOS se replican con su ID
-- (lo hace el API). Los modulos MIGRADOS se numeran en cada base por orden de ID_Configuracion: son iguales
-- solo si ambas bases tienen las mismas configuraciones. Despues de migrar, compare los resultados de la
-- verificacion final (o use DbCompare) en principal y espejo.
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================

-- ------------------------------------------------------------
-- 1. Tablas
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Equipo_Configuracion_Modulo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipo_Configuracion_Modulo
    (
        ID_Modulo            INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Equipo_Configuracion_Modulo PRIMARY KEY,
        Imei                 VARCHAR(30)  NOT NULL,
        Tipo_Pieza           VARCHAR(20)  NOT NULL
            CONSTRAINT CK_Equipo_Configuracion_Modulo_Pieza CHECK (Tipo_Pieza IN ('RAM', 'ALMACENAMIENTO')),
        Capacidad_GB         INT          NOT NULL
            CONSTRAINT CK_Equipo_Configuracion_Modulo_Capacidad CHECK (Capacidad_GB > 0),
        ID_Modelo_Componente INT          NOT NULL
            CONSTRAINT FK_Equipo_Configuracion_Modulo_Modelos FOREIGN KEY REFERENCES dbo.Modelos (ID_Modelo),
        Tipo                 VARCHAR(30)  NOT NULL,           -- se toma de Componente_Modelo_Tipo segun el modelo
        ID_Accesorio         BIGINT       NULL,               -- si entro por un cambio de pieza
        Estado               CHAR(1)      NOT NULL
            CONSTRAINT DF_Equipo_Configuracion_Modulo_Estado DEFAULT 'A'
            CONSTRAINT CK_Equipo_Configuracion_Modulo_Estado CHECK (Estado IN ('A', 'R')),   -- A instalado | R retirado
        ID_Cambio_Alta       INT          NULL,
        ID_Cambio_Baja       INT          NULL,
        Usuario              VARCHAR(60)  NOT NULL,
        Fecha_Registro       DATETIME     NOT NULL
            CONSTRAINT DF_Equipo_Configuracion_Modulo_Fecha DEFAULT GETDATE()
    );

    CREATE INDEX IX_Equipo_Configuracion_Modulo_Imei_Estado
        ON dbo.Equipo_Configuracion_Modulo (Imei, Estado);
END
GO

IF OBJECT_ID('dbo.Equipo_Configuracion_Correccion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipo_Configuracion_Correccion
    (
        ID_Correccion INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Equipo_Configuracion_Correccion PRIMARY KEY,
        Imei          VARCHAR(30)  NOT NULL,
        ID_Modulo     INT          NOT NULL,
        Antes         VARCHAR(100) NOT NULL,
        Despues       VARCHAR(100) NOT NULL,
        Motivo        VARCHAR(250) NOT NULL,
        Usuario       VARCHAR(60)  NOT NULL,
        Fecha         DATETIME     NOT NULL
            CONSTRAINT DF_Equipo_Configuracion_Correccion_Fecha DEFAULT GETDATE()
    );

    CREATE INDEX IX_Equipo_Configuracion_Correccion_Imei
        ON dbo.Equipo_Configuracion_Correccion (Imei);
END
GO

-- ------------------------------------------------------------
-- 2. Recalcular el resumen (Equipo_Configuracion) desde los modulos activos
--    Tipos = el del primer modulo activo (por ID_Modulo). Sin modulos de un tipo -> NULL.
--    Si la laptop no tiene fila en Equipo_Configuracion no hace nada.
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Recalcular
    @Imei    VARCHAR(30),
    @Usuario VARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE C
       SET Ram_GB = (SELECT SUM(M.Capacidad_GB)
                       FROM dbo.Equipo_Configuracion_Modulo M
                      WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM'),
           Ram_Tipo = (SELECT TOP 1 M.Tipo
                         FROM dbo.Equipo_Configuracion_Modulo M
                        WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM'
                        ORDER BY M.ID_Modulo),
           Almacenamiento_GB = (SELECT SUM(M.Capacidad_GB)
                                  FROM dbo.Equipo_Configuracion_Modulo M
                                 WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'ALMACENAMIENTO'),
           Almacenamiento_Tipo = (SELECT TOP 1 M.Tipo
                                    FROM dbo.Equipo_Configuracion_Modulo M
                                   WHERE M.Imei = C.Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'ALMACENAMIENTO'
                                   ORDER BY M.ID_Modulo),
           Usuario_Modifica = ISNULL(@Usuario, C.Usuario_Modifica),
           Fecha_Modifica   = GETDATE()
      FROM dbo.Equipo_Configuracion C
     WHERE C.Imei = @Imei;
END
GO

-- ------------------------------------------------------------
-- 3. Consultas
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Modulos_Consultar
    @Imei               VARCHAR(30),
    @Incluir_Retirados  BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT M.ID_Modulo, M.Imei, M.Tipo_Pieza, M.Capacidad_GB,
           M.ID_Modelo_Componente, Modelo_Componente = Mo.Descripcion,
           M.Tipo, M.ID_Accesorio, M.Estado, M.ID_Cambio_Alta, M.ID_Cambio_Baja, M.Usuario, M.Fecha_Registro
      FROM dbo.Equipo_Configuracion_Modulo M
     INNER JOIN dbo.Modelos Mo ON Mo.ID_Modelo = M.ID_Modelo_Componente
     WHERE M.Imei = @Imei
       AND (@Incluir_Retirados = 1 OR M.Estado = 'A')
     ORDER BY CASE WHEN M.Tipo_Pieza = 'RAM' THEN 0 ELSE 1 END, M.ID_Modulo;
END
GO

CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Correcciones_Consultar
    @Imei VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID_Correccion, Imei, ID_Modulo, Antes, Despues, Motivo, Usuario, Fecha
      FROM dbo.Equipo_Configuracion_Correccion
     WHERE Imei = @Imei
     ORDER BY Fecha DESC, ID_Correccion DESC;
END
GO

-- Catalogo de los modelos de componentes (para elegir el modelo de cada modulo)
CREATE OR ALTER PROCEDURE dbo.Componente_Modelos_Consultar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT T.ID_Modelo, Descripcion = Mo.Descripcion, T.Tipo_Pieza, T.Tipo
      FROM dbo.Componente_Modelo_Tipo T
     INNER JOIN dbo.Modelos Mo ON Mo.ID_Modelo = T.ID_Modelo
     ORDER BY CASE WHEN T.Tipo_Pieza = 'RAM' THEN 0 ELSE 1 END, Mo.Descripcion;
END
GO

-- ------------------------------------------------------------
-- 4. Corregir un modulo (errores de registro o de migracion, ej. un SSD SATA 2.5 que en realidad es M.2)
--    Solo laptop disponible y modulo instalado. No mueve inventario ni costo.
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Modulo_Corregir
    @ID_Modulo            INT,
    @ID_Modelo_Componente INT,
    @Capacidad_GB         INT,
    @Motivo               VARCHAR(250),
    @Usuario              VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50500, 'El usuario es requerido.', 1;

        IF @Motivo IS NULL OR LTRIM(RTRIM(@Motivo)) = ''
            THROW 50501, 'El motivo de la correccion es requerido.', 1;

        IF @Capacidad_GB IS NULL OR @Capacidad_GB <= 0 OR @Capacidad_GB > 16000
            THROW 50502, 'La capacidad debe estar entre 1 y 16000 GB.', 1;

        DECLARE @Imei VARCHAR(30), @Tipo_Pieza VARCHAR(20), @Estado CHAR(1),
                @Cap_Ant INT, @Modelo_Ant INT, @Tipo_Ant VARCHAR(30);

        SELECT @Imei = M.Imei, @Tipo_Pieza = M.Tipo_Pieza, @Estado = M.Estado,
               @Cap_Ant = M.Capacidad_GB, @Modelo_Ant = M.ID_Modelo_Componente, @Tipo_Ant = M.Tipo
          FROM dbo.Equipo_Configuracion_Modulo M WITH (UPDLOCK, ROWLOCK)
         WHERE M.ID_Modulo = @ID_Modulo;

        IF @Imei IS NULL
            THROW 50503, 'El modulo no existe.', 1;

        IF @Estado <> 'A'
            THROW 50504, 'Solo se puede corregir un modulo instalado.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Equipos WHERE Imei = @Imei AND Disponible = 'S')
            THROW 50505, 'La laptop no esta disponible (vendida o no disponible). No se puede corregir su configuracion.', 1;

        DECLARE @Tipo_Nuevo VARCHAR(30), @Pieza_Nueva VARCHAR(20);

        SELECT @Tipo_Nuevo = T.Tipo, @Pieza_Nueva = T.Tipo_Pieza
          FROM dbo.Componente_Modelo_Tipo T
         WHERE T.ID_Modelo = @ID_Modelo_Componente;

        IF @Tipo_Nuevo IS NULL
            THROW 50506, 'El modelo indicado no es un modelo de componente.', 1;

        IF @Pieza_Nueva <> @Tipo_Pieza
            THROW 50507, 'El modelo indicado no corresponde al tipo de pieza del modulo (RAM / almacenamiento).', 1;

        IF @Tipo_Pieza = 'RAM'
           AND EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo X
                        WHERE X.Imei = @Imei AND X.Estado = 'A' AND X.Tipo_Pieza = 'RAM'
                          AND X.ID_Modulo <> @ID_Modulo AND X.Tipo <> @Tipo_Nuevo)
            THROW 50508, 'No se pueden mezclar tipos de RAM (DDR3 / DDR4 / DDR5) en una misma laptop.', 1;

        IF @Modelo_Ant = @ID_Modelo_Componente AND @Cap_Ant = @Capacidad_GB
            THROW 50509, 'No hay cambios: el modulo ya tiene ese modelo y esa capacidad.', 1;

        DECLARE @Antes VARCHAR(100) =
            CONCAT(@Cap_Ant, 'GB ', (SELECT Descripcion FROM dbo.Modelos WHERE ID_Modelo = @Modelo_Ant));
        DECLARE @Despues VARCHAR(100) =
            CONCAT(@Capacidad_GB, 'GB ', (SELECT Descripcion FROM dbo.Modelos WHERE ID_Modelo = @ID_Modelo_Componente));

        BEGIN TRAN;

        UPDATE dbo.Equipo_Configuracion_Modulo
           SET ID_Modelo_Componente = @ID_Modelo_Componente,
               Tipo                 = @Tipo_Nuevo,
               Capacidad_GB         = @Capacidad_GB
         WHERE ID_Modulo = @ID_Modulo;

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;

        INSERT INTO dbo.Equipo_Configuracion_Correccion (Imei, ID_Modulo, Antes, Despues, Motivo, Usuario, Fecha)
        VALUES (@Imei, @ID_Modulo, @Antes, @Despues, LTRIM(RTRIM(@Motivo)), @Usuario, GETDATE());

        COMMIT TRAN;

        SELECT @ID_Modulo AS ID_Modulo, @Imei AS Imei,
               Configuracion = dbo.fn_Equipo_Configuracion_Texto(@Imei),
               Mensaje = 'Modulo corregido satisfactoriamente';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

-- ------------------------------------------------------------
-- 5. Migracion de las laptops que ya tienen configuracion y aun no tienen modulos
--    Solo despues de la Tarea 1: si falta un modelo de componente, se detiene sin migrar nada.
-- ------------------------------------------------------------
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Faltan NVARCHAR(600) = N'';
SELECT @Faltan += N' [' + x.Descripcion + N']'
  FROM (VALUES ('RAM DDR3 SODIMM'), ('RAM DDR4 SODIMM'), ('RAM DDR5 SODIMM'), ('SSD SATA 2.5'),
               ('SSD M.2 NVMe'), ('HDD 2.5')) AS x(Descripcion)
 WHERE NOT EXISTS (SELECT 1
                     FROM dbo.Modelos m
                    INNER JOIN dbo.Componente_Modelo_Tipo t ON t.ID_Modelo = m.ID_Modelo
                    WHERE m.Descripcion = x.Descripcion);

IF LEN(@Faltan) > 0
BEGIN
    DECLARE @Msg NVARCHAR(900) = N'No se migro nada. Falta ejecutar CREATE_Componentes_Especificacion.sql (modelos sin registrar en Componente_Modelo_Tipo):' + @Faltan;
    THROW 50510, @Msg, 1;
END

DECLARE @Mapa TABLE (Tipo VARCHAR(30) PRIMARY KEY, Tipo_Pieza VARCHAR(20) NOT NULL, Modelo VARCHAR(100) NOT NULL);
INSERT INTO @Mapa (Tipo, Tipo_Pieza, Modelo) VALUES
    ('DDR3',     'RAM',            'RAM DDR3 SODIMM'),
    ('DDR4',     'RAM',            'RAM DDR4 SODIMM'),
    ('DDR5',     'RAM',            'RAM DDR5 SODIMM'),
    ('SSD SATA', 'ALMACENAMIENTO', 'SSD SATA 2.5'),
    ('SSD NVMe', 'ALMACENAMIENTO', 'SSD M.2 NVMe'),
    ('HDD',      'ALMACENAMIENTO', 'HDD 2.5');

DECLARE @Pendientes TABLE (Imei VARCHAR(30) PRIMARY KEY);
INSERT INTO @Pendientes (Imei)
SELECT C.Imei
  FROM dbo.Equipo_Configuracion C
 WHERE NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo M WHERE M.Imei = C.Imei);

BEGIN TRAN;

INSERT INTO dbo.Equipo_Configuracion_Modulo
    (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
SELECT C.Imei, 'RAM', C.Ram_GB, Mo.ID_Modelo, Mp.Tipo, 'A', 'migracion', GETDATE()
  FROM dbo.Equipo_Configuracion C
 INNER JOIN @Pendientes P ON P.Imei = C.Imei
 INNER JOIN @Mapa Mp ON Mp.Tipo = C.Ram_Tipo AND Mp.Tipo_Pieza = 'RAM'
 INNER JOIN dbo.Modelos Mo ON Mo.Descripcion = Mp.Modelo
 WHERE C.Ram_GB > 0
 ORDER BY C.ID_Configuracion;

INSERT INTO dbo.Equipo_Configuracion_Modulo
    (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
SELECT C.Imei, 'ALMACENAMIENTO', C.Almacenamiento_GB, Mo.ID_Modelo, Mp.Tipo, 'A', 'migracion', GETDATE()
  FROM dbo.Equipo_Configuracion C
 INNER JOIN @Pendientes P ON P.Imei = C.Imei
 INNER JOIN @Mapa Mp ON Mp.Tipo = C.Almacenamiento_Tipo AND Mp.Tipo_Pieza = 'ALMACENAMIENTO'
 INNER JOIN dbo.Modelos Mo ON Mo.Descripcion = Mp.Modelo
 WHERE C.Almacenamiento_GB > 0
 ORDER BY C.ID_Configuracion;

COMMIT TRAN;

-- Configuraciones que NO se pudieron migrar (tipo vacio o desconocido). Quedan con el texto anterior.
SELECT 'NO MIGRADA' AS Estado, C.Imei, C.Ram_GB, C.Ram_Tipo, C.Almacenamiento_GB, C.Almacenamiento_Tipo
  FROM dbo.Equipo_Configuracion C
 INNER JOIN @Pendientes P ON P.Imei = C.Imei
 WHERE (C.Ram_GB > 0 AND NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo M WHERE M.Imei = C.Imei AND M.Tipo_Pieza = 'RAM'))
    OR (C.Almacenamiento_GB > 0 AND NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo M WHERE M.Imei = C.Imei AND M.Tipo_Pieza = 'ALMACENAMIENTO'));
GO

-- ------------------------------------------------------------
-- 6. Verificacion (comparar principal y espejo)
-- ------------------------------------------------------------
SELECT M.ID_Modulo, M.Imei, M.Tipo_Pieza, M.Capacidad_GB, Modelo = Mo.Descripcion, M.Tipo, M.Estado, M.Usuario
  FROM dbo.Equipo_Configuracion_Modulo M
 INNER JOIN dbo.Modelos Mo ON Mo.ID_Modelo = M.ID_Modelo_Componente
 ORDER BY M.ID_Modulo;
GO
