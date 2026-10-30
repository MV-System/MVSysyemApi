-- ============================================================
-- MODULO DE LAPTOPS - SCRIPT COMPLETO (version vigente de cada objeto, una sola copia)
-- Fecha: 2026-10-30
--
-- Incluye: grupos y tablas base, especificacion de componentes (Tarea 1), modulos por laptop (Tarea 2),
-- cambio de pieza por modulos, pieza usada, anulacion, custodia, kardex y comprobante (Tareas 3 a 8),
-- fechas en hora local (Tarea 9) y los procedimientos de consulta que muestran la configuracion.
--
-- Ejecutar en TODAS las bases (principal y espejo de cada empresa), de una sola vez y de arriba hacia abajo.
-- Es idempotente: se puede repetir sin duplicar nada.
-- Todas las fechas se guardan con DATEADD(HOUR, -4, GETDATE()) (hora local).
--
-- Los UPDATE de datos (estados de prefacturas) estan APARTE en SCRIPT_MODULO_LAPTOPS_DATOS.sql:
-- ejecutarlo DESPUES de este y una sola vez por base.
-- Los permisos (BD SEGURIDAD) ya se ejecutaron y aplican a todas las bases: no forman parte de este script.
-- Requiere que ya existan CopiarDetallesPagoPrefacturaAFactura y las tablas generales del sistema.
-- ============================================================


-- ############################################################
-- 1. TABLAS BASE: configuracion de la laptop, grupos Laptops / Componentes y cambios de pieza
-- ############################################################

IF OBJECT_ID('dbo.Equipo_Configuracion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipo_Configuracion (
        ID_Configuracion    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Imei                VARCHAR(30)  NOT NULL,
        Ram_GB              INT          NULL,
        Ram_Tipo            VARCHAR(30)  NULL,
        Almacenamiento_GB   INT          NULL,
        Almacenamiento_Tipo VARCHAR(30)  NULL,
        Notas               VARCHAR(250) NULL,
        Usuario             VARCHAR(60)  NOT NULL,
        Fecha_Registro      DATETIME     NOT NULL DEFAULT (DATEADD(HOUR, -4, GETDATE())),
        Usuario_Modifica    VARCHAR(60)  NULL,
        Fecha_Modifica      DATETIME     NULL,
        CONSTRAINT UQ_Equipo_Configuracion_Imei UNIQUE (Imei)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops')
BEGIN
    INSERT INTO dbo.Grupos_Cata (Descripcion, Estado, Usuario, Fecha_Registro)
    VALUES ('Laptops', 'A', 'sistema', DATEADD(HOUR, -4, GETDATE()));
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes')
BEGIN
    INSERT INTO dbo.Grupos_Cata (Descripcion, Estado, Usuario, Fecha_Registro)
    VALUES ('Componentes', 'A', 'sistema', DATEADD(HOUR, -4, GETDATE()));
END
GO

-- Verificación
SELECT ID_Grupo, Descripcion, Estado, Usuario, Fecha_Registro
FROM dbo.Grupos_Cata
WHERE Descripcion IN ('Laptops', 'Componentes');
GO

IF OBJECT_ID('dbo.Equipo_Cambio_Pieza', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipo_Cambio_Pieza (
        ID_Cambio               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Imei                    VARCHAR(30)   NOT NULL,
        Tipo_Pieza              VARCHAR(20)   NOT NULL,   -- 'RAM' | 'ALMACENAMIENTO'
        ID_Accesorio_Instalado  BIGINT        NULL,
        Costo_Instalado         DECIMAL(18,2) NOT NULL DEFAULT 0,
        ID_Accesorio_Retirado   BIGINT        NULL,
        Costo_Retirado          DECIMAL(18,2) NOT NULL DEFAULT 0,
        Config_Anterior         VARCHAR(100)  NULL,       -- ej. '8GB / 256GB SSD SATA'
        Config_Nueva            VARCHAR(100)  NULL,
        Costo_Equipo_Anterior   DECIMAL(18,2) NOT NULL,
        Costo_Equipo_Nuevo      DECIMAL(18,2) NOT NULL,
        ID_Almacen              INT           NOT NULL,
        ID_PreFactura           INT           NULL,
        ID_Reparacion           INT           NULL,
        Tecnico                 VARCHAR(60)   NOT NULL,
        Estado_Custodia         VARCHAR(20)   NOT NULL,
        Usuario_Registro        VARCHAR(60)   NOT NULL,
        Fecha_Registro          DATETIME      NOT NULL DEFAULT (DATEADD(HOUR, -4, GETDATE())),
        Usuario_Recepcion       VARCHAR(60)   NULL,
        Fecha_Recepcion         DATETIME      NULL,
        Nota_Recepcion          VARCHAR(250)  NULL,
        CONSTRAINT CK_Equipo_Cambio_Pieza_Tipo
            CHECK (Tipo_Pieza IN ('RAM', 'ALMACENAMIENTO')),
        CONSTRAINT CK_Equipo_Cambio_Pieza_Estado
            CHECK (Estado_Custodia IN ('PENDIENTE', 'RECIBIDA', 'ENTREGADA_CLIENTE', 'NO_APLICA'))
    );
END
GO

-- Índices
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipo_Cambio_Pieza_Imei' AND object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza'))
    CREATE INDEX IX_Equipo_Cambio_Pieza_Imei ON dbo.Equipo_Cambio_Pieza (Imei);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipo_Cambio_Pieza_Custodia_Tecnico' AND object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza'))
    CREATE INDEX IX_Equipo_Cambio_Pieza_Custodia_Tecnico ON dbo.Equipo_Cambio_Pieza (Estado_Custodia, Tecnico);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipo_Cambio_Pieza_Fecha_Registro' AND object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza'))
    CREATE INDEX IX_Equipo_Cambio_Pieza_Fecha_Registro ON dbo.Equipo_Cambio_Pieza (Fecha_Registro);
GO

-- Verificación
SELECT name AS Indice FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Equipo_Cambio_Pieza') AND name IS NOT NULL;
GO

-- Columnas agregadas al historial de cambios de pieza (modulos y anulacion)
IF COL_LENGTH('dbo.Equipo_Cambio_Pieza', 'ID_Modulo_Retirado') IS NULL
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD ID_Modulo_Retirado INT NULL;
GO
IF COL_LENGTH('dbo.Equipo_Cambio_Pieza', 'ID_Modulo_Instalado') IS NULL
    ALTER TABLE dbo.Equipo_Cambio_Pieza ADD ID_Modulo_Instalado INT NULL;
GO

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


-- ############################################################
-- 2. ESPECIFICACION DE COMPONENTES (marca y modelos Componentes, tipo por modelo, capacidad leida de la descripcion)
-- ############################################################

-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 1 (especificacion de cada componente)
-- Proposito: que el sistema sepa la capacidad y el tipo de cada pieza (RAM / almacenamiento)
--            sin que el usuario los escriba a mano. Crea:
--            1. Los 7 modelos de componentes (marca y grupo 'Componentes') si faltan.
--            2. dbo.Componente_Modelo_Tipo: tipo de pieza y tipo de cada modelo de componente.
--            3. dbo.fn_Componente_Capacidad_GB: capacidad en GB leida de la descripcion del accesorio.
--            4. dbo.Componente_Especificacion_Consultar: Tipo_Pieza, Tipo y Capacidad_GB de un accesorio.
-- Ejecutar en TODAS las bases (principal y espejo de cada empresa).
-- Script idempotente: solo agrega lo que falta; no modifica ni borra datos existentes.
--
-- IDs en la espejo: si en alguna base hay que CREAR marca o modelos, deben quedar con el MISMO ID
-- en principal y espejo (el API envia el ID del modelo al replicar). Orden recomendado:
--   1) Ejecutar en la PRINCIPAL. El resultado final lista los IDs de la marca y de cada modelo.
--   2) Si en la ESPEJO tambien faltan, copiar esos IDs en las variables "ID forzado" del PASO 2
--      (ID_Marca_Forzado y la columna ID_Forzado de cada modelo) y ejecutar.
--   Si los modelos ya existen en ambas bases, no hay que tocar nada.
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================

-- ------------------------------------------------------------
-- PASO 1. Tabla dbo.Componente_Modelo_Tipo
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Componente_Modelo_Tipo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Componente_Modelo_Tipo
    (
        ID_Modelo  INT         NOT NULL
            CONSTRAINT PK_Componente_Modelo_Tipo PRIMARY KEY,
        Tipo_Pieza VARCHAR(20) NOT NULL
            CONSTRAINT CK_Componente_Modelo_Tipo_Pieza CHECK (Tipo_Pieza IN ('RAM', 'ALMACENAMIENTO')),
        Tipo       VARCHAR(30) NOT NULL,
        CONSTRAINT FK_Componente_Modelo_Tipo_Modelos FOREIGN KEY (ID_Modelo) REFERENCES dbo.Modelos (ID_Modelo)
    );
END
GO

-- ------------------------------------------------------------
-- PASO 2. Asegurar marca, grupo y los 7 modelos de componentes, y sembrar la tabla
-- ------------------------------------------------------------
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Usuario           VARCHAR(50) = 'sistema';
DECLARE @ID_Marca_Forzado  INT = NULL;      -- solo para la espejo si hay que crear la marca (ver encabezado)

DECLARE @Modelos TABLE
(
    Orden       INT         NOT NULL PRIMARY KEY,
    Descripcion VARCHAR(100) NOT NULL,
    Tipo_Pieza  VARCHAR(20)  NOT NULL,
    Tipo        VARCHAR(30)  NOT NULL,
    ID_Forzado  INT          NULL           -- solo para la espejo si hay que crear el modelo (ver encabezado)
);

INSERT INTO @Modelos (Orden, Descripcion, Tipo_Pieza, Tipo, ID_Forzado) VALUES
    (1, 'RAM DDR3 SODIMM', 'RAM',            'DDR3',     NULL),
    (2, 'RAM DDR4 SODIMM', 'RAM',            'DDR4',     NULL),
    (3, 'RAM DDR5 SODIMM', 'RAM',            'DDR5',     NULL),
    (4, 'SSD SATA 2.5',    'ALMACENAMIENTO', 'SSD SATA', NULL),
    (5, 'SSD M.2 SATA',    'ALMACENAMIENTO', 'SSD SATA', NULL),
    (6, 'SSD M.2 NVMe',    'ALMACENAMIENTO', 'SSD NVMe', NULL),
    (7, 'HDD 2.5',         'ALMACENAMIENTO', 'HDD',      NULL);

DECLARE @ID_Grupo INT, @ID_Marca INT;
DECLARE @Problemas NVARCHAR(1500) = N'';

BEGIN TRAN;

-- Grupo 'Componentes' (lo crea CREATE_Laptops_Fase1_Grupos.sql; aqui se asegura por si falta)
SELECT TOP 1 @ID_Grupo = ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes';

IF @ID_Grupo IS NULL
BEGIN
    INSERT INTO dbo.Grupos_Cata (Descripcion, Estado, Usuario, Fecha_Registro)
    VALUES ('Componentes', 'A', 'sistema', DATEADD(HOUR, -4, GETDATE()));
    SET @ID_Grupo = SCOPE_IDENTITY();
END

-- Marca 'Componentes'
SELECT TOP 1 @ID_Marca = ID_Marca FROM dbo.Marcas WHERE Descripcion = 'Componentes';

IF @ID_Marca IS NULL
BEGIN
    IF @ID_Marca_Forzado IS NULL
    BEGIN
        INSERT INTO dbo.Marcas (Descripcion, Estado, Usuario, Fecha_registro)
        VALUES ('Componentes', 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));
        SET @ID_Marca = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Marcas WHERE ID_Marca = @ID_Marca_Forzado)
            THROW 50401, 'El ID forzado de la marca Componentes ya lo usa otra marca. Revise ID_Marca_Forzado.', 1;

        SET IDENTITY_INSERT dbo.Marcas ON;
        INSERT INTO dbo.Marcas (ID_Marca, Descripcion, Estado, Usuario, Fecha_registro)
        VALUES (@ID_Marca_Forzado, 'Componentes', 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));
        SET IDENTITY_INSERT dbo.Marcas OFF;
        SET @ID_Marca = @ID_Marca_Forzado;
    END
END

-- Modelos de componentes
DECLARE @Orden INT = 1, @Max INT = (SELECT MAX(Orden) FROM @Modelos);
DECLARE @Desc VARCHAR(100), @IdForzado INT, @IdGrupoActual INT;

WHILE @Orden <= @Max
BEGIN
    SELECT @Desc = Descripcion, @IdForzado = ID_Forzado FROM @Modelos WHERE Orden = @Orden;
    SET @IdGrupoActual = NULL;

    -- La descripcion del modelo es unica en todo el catalogo (igual que en Modelos_Insert)
    SELECT TOP 1 @IdGrupoActual = ID_Grupo FROM dbo.Modelos WHERE Descripcion = @Desc;

    IF @@ROWCOUNT = 0
    BEGIN
        IF @IdForzado IS NULL
        BEGIN
            INSERT INTO dbo.Modelos (Descripcion, ID_Marca, Estado, Usuario, Fecha_registro, ID_Grupo)
            VALUES (@Desc, @ID_Marca, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()), @ID_Grupo);
        END
        ELSE
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.Modelos WHERE ID_Modelo = @IdForzado)
                SET @Problemas += N' [' + @Desc + N': el ID forzado ' + CONVERT(NVARCHAR(20), @IdForzado) + N' ya lo usa otro modelo]';
            ELSE
            BEGIN
                SET IDENTITY_INSERT dbo.Modelos ON;
                INSERT INTO dbo.Modelos (ID_Modelo, Descripcion, ID_Marca, Estado, Usuario, Fecha_registro, ID_Grupo)
                VALUES (@IdForzado, @Desc, @ID_Marca, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()), @ID_Grupo);
                SET IDENTITY_INSERT dbo.Modelos OFF;
            END
        END
    END
    ELSE IF ISNULL(@IdGrupoActual, -1) <> @ID_Grupo
    BEGIN
        SET @Problemas += N' [' + @Desc + N': ya existe pero no esta en el grupo Componentes]';
    END

    SET @Orden += 1;
END

IF LEN(@Problemas) > 0
BEGIN
    DECLARE @Msg NVARCHAR(2000) = N'No se pudo asegurar los modelos de componentes:' + @Problemas;
    THROW 50402, @Msg, 1;
END

-- Sembrar dbo.Componente_Modelo_Tipo
INSERT INTO dbo.Componente_Modelo_Tipo (ID_Modelo, Tipo_Pieza, Tipo)
SELECT m.ID_Modelo, x.Tipo_Pieza, x.Tipo
FROM @Modelos x
INNER JOIN dbo.Modelos m ON m.Descripcion = x.Descripcion AND m.ID_Grupo = @ID_Grupo
WHERE NOT EXISTS (SELECT 1 FROM dbo.Componente_Modelo_Tipo t WHERE t.ID_Modelo = m.ID_Modelo);

COMMIT TRAN;

-- IDs resultantes (copiarlos a la espejo si hubo que crear marca o modelos)
SELECT 'Marca' AS Elemento, ID_Marca AS ID, Descripcion FROM dbo.Marcas WHERE ID_Marca = @ID_Marca
UNION ALL
SELECT 'Modelo', m.ID_Modelo, m.Descripcion
FROM @Modelos x INNER JOIN dbo.Modelos m ON m.Descripcion = x.Descripcion
ORDER BY 1 DESC, 2;
GO

-- ------------------------------------------------------------
-- PASO 3. Capacidad en GB leida de la descripcion del accesorio
--         Primer numero seguido de GB o TB (TB x 1024). Sin coincidencia -> NULL.
--         Acepta '8GB', '8 GB', '8gb 3200', '1TB', '1 TB', '1.5TB', 'DDR4 16GB'.
-- ------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.fn_Componente_Capacidad_GB (@Descripcion VARCHAR(200))
RETURNS INT
AS
BEGIN
    DECLARE @s VARCHAR(200) = UPPER(ISNULL(@Descripcion, ''));
    DECLARE @len INT = LEN(@s);
    DECLARE @pos INT = 1, @ini INT, @fin INT, @j INT;
    DECLARE @num VARCHAR(30), @unidad CHAR(2);
    DECLARE @valor DECIMAL(18, 4);

    WHILE @pos <= @len
    BEGIN
        -- siguiente digito
        SET @ini = PATINDEX('%[0-9]%', SUBSTRING(@s, @pos, 200));
        IF @ini = 0 BREAK;
        SET @ini = @pos + @ini - 1;

        -- leer el numero (con decimal opcional: 1.5 / 1,5)
        SET @fin = @ini;
        WHILE @fin <= @len AND SUBSTRING(@s, @fin, 1) LIKE '[0-9]'
            SET @fin += 1;

        IF @fin < @len AND SUBSTRING(@s, @fin, 1) IN ('.', ',') AND SUBSTRING(@s, @fin + 1, 1) LIKE '[0-9]'
        BEGIN
            SET @fin += 1;
            WHILE @fin <= @len AND SUBSTRING(@s, @fin, 1) LIKE '[0-9]'
                SET @fin += 1;
        END

        SET @num = REPLACE(SUBSTRING(@s, @ini, @fin - @ini), ',', '.');

        -- espacios opcionales y unidad
        SET @j = @fin;
        WHILE @j <= @len AND SUBSTRING(@s, @j, 1) = ' '
            SET @j += 1;
        SET @unidad = SUBSTRING(@s, @j, 2);

        IF @unidad IN ('GB', 'TB')
        BEGIN
            SET @valor = TRY_CAST(@num AS DECIMAL(18, 4));
            IF @valor IS NULL OR @valor <= 0 OR @valor > 100000
                RETURN NULL;
            RETURN CAST(ROUND(@valor * CASE WHEN @unidad = 'TB' THEN 1024 ELSE 1 END, 0) AS INT);
        END

        SET @pos = @fin;
    END

    RETURN NULL;
END
GO

-- ------------------------------------------------------------
-- PASO 4. Especificacion de un accesorio: tipo de pieza, tipo y capacidad
--         (vacio si el accesorio no es de uno de los 7 modelos de componentes)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Componente_Especificacion_Consultar
    @ID_Accesorio BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
           t.Tipo_Pieza,
           t.Tipo,
           Capacidad_GB = dbo.fn_Componente_Capacidad_GB(a.Descripcion_Accesorio)
      FROM dbo.Accesorios a
     INNER JOIN dbo.Componente_Modelo_Tipo t ON t.ID_Modelo = a.ID_Modelo
     WHERE a.ID_Accesorio = @ID_Accesorio;
END
GO

-- ------------------------------------------------------------
-- PASO 5. Verificacion (Aceptacion de la Tarea 1)
-- ------------------------------------------------------------
SELECT Descripcion_Prueba = x.Texto,
       Esperado           = x.Esperado,
       Obtenido           = dbo.fn_Componente_Capacidad_GB(x.Texto),
       Resultado          = CASE WHEN ISNULL(dbo.fn_Componente_Capacidad_GB(x.Texto), -1) = ISNULL(x.Esperado, -1)
                                 THEN 'OK' ELSE 'FALLA' END
  FROM (VALUES ('8GB 3200', 8), ('8 GB', 8), ('8gb 3200', 8), ('16 GB', 16), ('1TB', 1024), ('1 TB', 1024),
               ('1.5TB', 1536), ('DDR4 16GB 3200', 16), ('256GB', 256), ('Memoria', NULL), ('3200', NULL), ('', NULL))
       AS x(Texto, Esperado);

SELECT t.ID_Modelo, m.Descripcion AS Modelo, t.Tipo_Pieza, t.Tipo
  FROM dbo.Componente_Modelo_Tipo t
 INNER JOIN dbo.Modelos m ON m.ID_Modelo = t.ID_Modelo
 ORDER BY t.Tipo_Pieza, m.Descripcion;
GO


-- ############################################################
-- 3. MODULOS POR LAPTOP (memorias y discos): tablas, recalcular, consultas, corregir y migracion
-- ############################################################

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
            CONSTRAINT DF_Equipo_Configuracion_Modulo_Fecha DEFAULT (DATEADD(HOUR, -4, GETDATE()))
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
            CONSTRAINT DF_Equipo_Configuracion_Correccion_Fecha DEFAULT (DATEADD(HOUR, -4, GETDATE()))
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
           Fecha_Modifica   = DATEADD(HOUR, -4, GETDATE())
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
        VALUES (@Imei, @ID_Modulo, @Antes, @Despues, LTRIM(RTRIM(@Motivo)), @Usuario, DATEADD(HOUR, -4, GETDATE()));

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
SELECT C.Imei, 'RAM', C.Ram_GB, Mo.ID_Modelo, Mp.Tipo, 'A', 'migracion', DATEADD(HOUR, -4, GETDATE())
  FROM dbo.Equipo_Configuracion C
 INNER JOIN @Pendientes P ON P.Imei = C.Imei
 INNER JOIN @Mapa Mp ON Mp.Tipo = C.Ram_Tipo AND Mp.Tipo_Pieza = 'RAM'
 INNER JOIN dbo.Modelos Mo ON Mo.Descripcion = Mp.Modelo
 WHERE C.Ram_GB > 0
 ORDER BY C.ID_Configuracion;

INSERT INTO dbo.Equipo_Configuracion_Modulo
    (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
SELECT C.Imei, 'ALMACENAMIENTO', C.Almacenamiento_GB, Mo.ID_Modelo, Mp.Tipo, 'A', 'migracion', DATEADD(HOUR, -4, GETDATE())
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


-- ############################################################
-- 3b. Texto de la configuracion (desde los modulos), guardar y consultar configuracion
-- ############################################################

CREATE OR ALTER FUNCTION dbo.fn_Equipo_Configuracion_Texto (@Imei VARCHAR(30))
RETURNS VARCHAR(100)
AS
BEGIN
    DECLARE @Texto VARCHAR(100);

    -- Con modulos: se arma desde los modulos activos
    IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo WHERE Imei = @Imei AND Estado = 'A')
    BEGIN
        DECLARE @Ram INT, @RamTipo VARCHAR(30), @Discos VARCHAR(100);

        SELECT @Ram = SUM(M.Capacidad_GB)
          FROM dbo.Equipo_Configuracion_Modulo M
         WHERE M.Imei = @Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM';

        SELECT TOP 1 @RamTipo = M.Tipo
          FROM dbo.Equipo_Configuracion_Modulo M
         WHERE M.Imei = @Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM'
         ORDER BY M.ID_Modulo;

        SET @Discos = STUFF((
            SELECT ' + '
                   + CASE WHEN M.Capacidad_GB >= 1024 AND M.Capacidad_GB % 1024 = 0
                          THEN CAST(M.Capacidad_GB / 1024 AS VARCHAR(10)) + 'TB'
                          ELSE CAST(M.Capacidad_GB AS VARCHAR(10)) + 'GB' END
                   + ' ' + M.Tipo
              FROM dbo.Equipo_Configuracion_Modulo M
             WHERE M.Imei = @Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'ALMACENAMIENTO'
             ORDER BY M.ID_Modulo
               FOR XML PATH('')), 1, 3, '');

        SET @Texto = NULLIF(LTRIM(RTRIM(
                ISNULL(CASE WHEN @Ram IS NOT NULL THEN CAST(@Ram AS VARCHAR(10)) + 'GB ' + @RamTipo END, '')
                + CASE WHEN @Ram IS NOT NULL AND @Discos IS NOT NULL THEN ' / ' ELSE '' END
                + ISNULL(@Discos, ''))), '');

        RETURN @Texto;
    END

    -- Sin modulos: comportamiento anterior
    SELECT @Texto =
        NULLIF(
            LTRIM(RTRIM(
                ISNULL(CASE WHEN C.Ram_GB IS NOT NULL
                            THEN CAST(C.Ram_GB AS VARCHAR(10)) + 'GB' + ISNULL(' ' + C.Ram_Tipo, '')
                       END, '')
                + CASE WHEN C.Ram_GB IS NOT NULL AND C.Almacenamiento_GB IS NOT NULL THEN ' / ' ELSE '' END
                + ISNULL(CASE WHEN C.Almacenamiento_GB IS NOT NULL
                              THEN CAST(C.Almacenamiento_GB AS VARCHAR(10)) + 'GB' + ISNULL(' ' + C.Almacenamiento_Tipo, '')
                         END, '')
            )), '')
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;

    RETURN @Texto;
END
GO

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
            Fecha_Modifica      = DATEADD(HOUR, -4, GETDATE())
        WHERE Imei = @Imei;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Equipo_Configuracion
            (Imei, Ram_GB, Ram_Tipo, Almacenamiento_GB, Almacenamiento_Tipo, Notas, Usuario, Fecha_Registro)
        VALUES
            (@Imei, @Ram_GB, @Ram_Tipo, @Almacenamiento_GB, @Almacenamiento_Tipo, @Notas, @Usuario, DATEADD(HOUR, -4, GETDATE()));
    END

    IF EXISTS (SELECT 1 FROM @Mod)
    BEGIN
        DELETE FROM dbo.Equipo_Configuracion_Modulo WHERE Imei = @Imei;

        -- Modulos nuevos: ID automatico, en el orden recibido
        INSERT INTO dbo.Equipo_Configuracion_Modulo
            (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, Estado, Usuario, Fecha_Registro)
        SELECT @Imei, X.Tipo_Pieza, X.Capacidad_GB, X.ID_Modelo, T.Tipo, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE())
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
            SELECT X.ID_Modulo, @Imei, X.Tipo_Pieza, X.Capacidad_GB, X.ID_Modelo, T.Tipo, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE())
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

CREATE OR ALTER PROCEDURE dbo.Equipo_Configuracion_Consultar
    @Imei VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Configuracion, C.Imei, C.Ram_GB, C.Ram_Tipo, C.Almacenamiento_GB, C.Almacenamiento_Tipo,
           C.Notas, dbo.fn_Equipo_Configuracion_Texto(C.Imei) AS Configuracion,
           C.Usuario, C.Fecha_Registro, C.Usuario_Modifica, C.Fecha_Modifica
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;
END
GO


-- ############################################################
-- 4. ACCESORIOS DE COMPONENTES: alta automatica de la pieza retirada (nueva / usada)
-- ############################################################

CREATE OR ALTER PROCEDURE dbo.Accesorio_Componente_Asegurar
    @ID_Modelo               INT,
    @Capacidad_GB            INT,
    @ID_Almacen              INT,
    @Usuario                 VARCHAR(60),
    @Ids_Explicitos          NVARCHAR(MAX) = NULL,
    @ID_Accesorio            BIGINT OUTPUT,
    @Creados                 NVARCHAR(MAX) = NULL OUTPUT,
    @Usado                   BIT = 0,
    @Porcentaje_Costo_Usado  DECIMAL(5,2) = 50
AS
BEGIN
    SET NOCOUNT ON;

    SET @ID_Accesorio = NULL;
    SET @Creados = NULL;
    DECLARE @UsuarioAcc VARCHAR(50) = 'Sistema';     -- creado automaticamente por el sistema

    -- Ya existe en el almacen (del mismo tipo: nueva o usada; se prefiere el activo)
    SELECT TOP 1 @ID_Accesorio = A.ID_Accesorio
      FROM dbo.Accesorios A
     WHERE A.ID_Modelo = @ID_Modelo
       AND A.ID_Almacen = @ID_Almacen
       AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB
       AND (CASE WHEN A.Descripcion_Accesorio LIKE '%(Usado)' THEN 1 ELSE 0 END) = @Usado
     ORDER BY CASE WHEN A.Estado = 'A' THEN 0 ELSE 1 END, A.ID_Accesorio;

    -- Referencia: el mismo modelo, capacidad y tipo (nueva / usada) en cualquier almacen
    DECLARE @Desc VARCHAR(100), @Costo MONEY = 0, @Mayor MONEY = 0, @Detalle MONEY = 0,
            @CanMax INT = 2, @CanMin INT = 1, @EstadoNuevo CHAR(1) = 'I';

    SELECT TOP 1 @Desc = A.Descripcion_Accesorio, @Costo = A.Costo_Equipo, @Mayor = A.Precio_Por_Mayor,
                 @Detalle = A.Precio_Detalle, @CanMax = ISNULL(A.Can_Max, 2), @CanMin = ISNULL(A.Can_Min, 1),
                 @EstadoNuevo = A.Estado
      FROM dbo.Accesorios A
     WHERE A.ID_Modelo = @ID_Modelo
       AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB
       AND (CASE WHEN A.Descripcion_Accesorio LIKE '%(Usado)' THEN 1 ELSE 0 END) = @Usado
     ORDER BY CASE WHEN A.Estado = 'A' THEN 0 ELSE 1 END,
              CASE WHEN A.Precio_Detalle > 0 THEN 0 ELSE 1 END, A.ID_Accesorio;

    IF @Desc IS NULL AND @Usado = 1
    BEGIN
        -- No hay variante usada: se deriva de la pieza nueva (costo = % del costo de la nueva, precios 0, inactiva)
        DECLARE @DescNueva VARCHAR(100), @CostoNueva MONEY = 0;

        SELECT TOP 1 @DescNueva = A.Descripcion_Accesorio, @CostoNueva = A.Costo_Equipo,
                     @CanMax = ISNULL(A.Can_Max, 2), @CanMin = ISNULL(A.Can_Min, 1)
          FROM dbo.Accesorios A
         WHERE A.ID_Modelo = @ID_Modelo
           AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB
           AND A.Descripcion_Accesorio NOT LIKE '%(Usado)'
         ORDER BY CASE WHEN A.Estado = 'A' THEN 0 ELSE 1 END,
                  CASE WHEN A.Costo_Equipo > 0 THEN 0 ELSE 1 END, A.ID_Accesorio;

        SET @Desc = @DescNueva;
        SET @Costo = ROUND(ISNULL(@CostoNueva, 0) * @Porcentaje_Costo_Usado / 100, 2);
        SET @Mayor = 0;
        SET @Detalle = 0;
        SET @EstadoNuevo = 'I';
    END

    IF @Desc IS NULL
        SET @Desc = CASE WHEN @Capacidad_GB >= 1024 AND @Capacidad_GB % 1024 = 0
                         THEN CAST(@Capacidad_GB / 1024 AS VARCHAR(10)) + 'TB'
                         ELSE CAST(@Capacidad_GB AS VARCHAR(10)) + 'GB' END;

    IF @Usado = 1 AND @Desc NOT LIKE '%(Usado)'
        SET @Desc = LEFT(@Desc, 100 - LEN(' (Usado)')) + ' (Usado)';

    -- Almacenes que aun no tienen el accesorio (del mismo tipo: nueva / usada)
    DECLARE @Faltan TABLE (Orden INT IDENTITY(1, 1) PRIMARY KEY, ID_Almacen INT NOT NULL, ID_Explicito BIGINT NULL);

    INSERT INTO @Faltan (ID_Almacen)
    SELECT AC.ID_Almacen
      FROM dbo.Almacen_Cata AC
     WHERE NOT EXISTS (SELECT 1 FROM dbo.Accesorios A
                        WHERE A.ID_Modelo = @ID_Modelo AND A.ID_Almacen = AC.ID_Almacen
                          AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB
                          AND (CASE WHEN A.Descripcion_Accesorio LIKE '%(Usado)' THEN 1 ELSE 0 END) = @Usado)
     ORDER BY AC.ID_Almacen;

    IF @Ids_Explicitos IS NOT NULL AND ISJSON(@Ids_Explicitos) = 1
        UPDATE F SET ID_Explicito = J.idAccesorio
          FROM @Faltan F
         INNER JOIN OPENJSON(@Ids_Explicitos) WITH (idAlmacen INT '$.idAlmacen', idAccesorio BIGINT '$.idAccesorio') J
                 ON J.idAlmacen = F.ID_Almacen;

    DECLARE @Orden INT = 1, @Max INT = (SELECT ISNULL(MAX(Orden), 0) FROM @Faltan);
    DECLARE @Almacen INT, @IdExp BIGINT, @IdNuevo BIGINT, @Json NVARCHAR(MAX) = N'';

    WHILE @Orden <= @Max
    BEGIN
        SELECT @Almacen = ID_Almacen, @IdExp = ID_Explicito FROM @Faltan WHERE Orden = @Orden;

        IF @IdExp IS NULL
        BEGIN
            INSERT INTO dbo.Accesorios
                (Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura, Descripcion_Accesorio, Cantidad, Can_Max, Can_Min,
                 Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado, Usuario)
            VALUES
                (NULL, @ID_Modelo, @Almacen, NULL, @Desc, 0, @CanMax, @CanMin,
                 @Mayor, @Detalle, @Costo, 'S', NULL, @EstadoNuevo, @UsuarioAcc);
            SET @IdNuevo = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            SET IDENTITY_INSERT dbo.Accesorios ON;
            INSERT INTO dbo.Accesorios
                (ID_Accesorio, Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura, Descripcion_Accesorio, Cantidad, Can_Max, Can_Min,
                 Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado, Usuario)
            VALUES
                (@IdExp, NULL, @ID_Modelo, @Almacen, NULL, @Desc, 0, @CanMax, @CanMin,
                 @Mayor, @Detalle, @Costo, 'S', NULL, @EstadoNuevo, @UsuarioAcc);
            SET IDENTITY_INSERT dbo.Accesorios OFF;
            SET @IdNuevo = @IdExp;
        END

        UPDATE dbo.Accesorios SET Codigo_Barra = 1000000000 + @IdNuevo WHERE ID_Accesorio = @IdNuevo;

        SET @Json += CASE WHEN LEN(@Json) > 0 THEN N',' ELSE N'' END
                   + N'{"idAlmacen":' + CONVERT(NVARCHAR(20), @Almacen) + N',"idAccesorio":' + CONVERT(NVARCHAR(20), @IdNuevo) + N'}';

        SET @Orden += 1;
    END

    IF LEN(@Json) > 0 SET @Creados = N'[' + @Json + N']';

    -- ID del accesorio en el almacen del cambio (existente o recien creado)
    IF @ID_Accesorio IS NULL
        SELECT TOP 1 @ID_Accesorio = A.ID_Accesorio
          FROM dbo.Accesorios A
         WHERE A.ID_Modelo = @ID_Modelo
           AND A.ID_Almacen = @ID_Almacen
           AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB
           AND (CASE WHEN A.Descripcion_Accesorio LIKE '%(Usado)' THEN 1 ELSE 0 END) = @Usado
         ORDER BY CASE WHEN A.Estado = 'A' THEN 0 ELSE 1 END, A.ID_Accesorio;
END
GO


-- ############################################################
-- 5. CONSULTAS DE LA PANTALLA DE CAMBIO DE PIEZA
-- ############################################################

CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Equipo_Consultar
    @Imei VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ID_Grupo_Laptops INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');

    SELECT TOP 1
           E.Imei,
           Modelo = M.Descripcion,
           Marca = Ma.Descripcion,
           Costo_Equipo = CAST(E.Costo_Equipo AS DECIMAL(18,2)),
           Precio_Detalle = CAST(E.Precio_Detalle AS DECIMAL(18,2)),
           Precio_Por_Mayor = CAST(E.Precio_Por_Mayor AS DECIMAL(18,2)),
           Configuracion = dbo.fn_Equipo_Configuracion_Texto(E.Imei),
           E.ID_Almacen,
           AC.Descripcion_Almacen,
           E.Disponible,
           Es_Laptop = CAST(CASE WHEN M.ID_Grupo = @ID_Grupo_Laptops THEN 1 ELSE 0 END AS BIT)
    FROM dbo.Equipos E
    INNER JOIN dbo.Modelos M ON M.ID_Modelo = E.ID_Modelo
    INNER JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
    INNER JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = E.ID_Almacen
    WHERE E.Imei = @Imei;
END
GO

-- ============================================================
-- 2. Accesorios (Componentes) de un almacén
--    @Tipo_Pieza: NULL = todos | 'RAM' (modelos RAM*) | 'ALMACENAMIENTO' (modelos SSD*/HDD*)
--    @SoloConStock = 1 -> solo con existencia (para la pieza instalada)
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Accesorios
    @ID_Almacen    INT,
    @Tipo_Pieza    VARCHAR(20) = NULL,
    @Filtro        VARCHAR(100) = '',
    @SoloConStock  BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ID_Grupo_Componentes INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes');

    SELECT A.ID_Accesorio,
           A.Descripcion_Accesorio,
           Modelo = M.Descripcion,
           Cantidad = ISNULL(A.Cantidad, 0),
           Costo_Equipo = CAST(A.Costo_Equipo AS DECIMAL(18,2))
    FROM dbo.Accesorios A
    INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
    WHERE M.ID_Grupo = @ID_Grupo_Componentes
      AND A.ID_Almacen = @ID_Almacen
      AND A.Estado = 'A'
      AND (@SoloConStock = 0 OR ISNULL(A.Cantidad, 0) > 0)
      AND (@Tipo_Pieza IS NULL
           OR (@Tipo_Pieza = 'RAM' AND M.Descripcion LIKE 'RAM%')
           OR (@Tipo_Pieza = 'ALMACENAMIENTO' AND (M.Descripcion LIKE 'SSD%' OR M.Descripcion LIKE 'HDD%')))
      AND (ISNULL(@Filtro, '') = ''
           OR A.Descripcion_Accesorio LIKE '%' + @Filtro + '%'
           OR M.Descripcion LIKE '%' + @Filtro + '%')
    ORDER BY M.Descripcion, A.Descripcion_Accesorio;
END
GO


-- ############################################################
-- 6. CAMBIO DE PIEZA: registrar, recibir, entregar al cliente, pendientes, anular, historial y comprobante
-- ############################################################

CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Registrar
    @Imei                   VARCHAR(30),
    @Tipo_Pieza             VARCHAR(20),          -- 'RAM' | 'ALMACENAMIENTO'
    @ID_Accesorio_Instalado BIGINT,
    @Nueva_Capacidad_GB     INT = NULL,           -- opcional: se calcula de la pieza instalada
    @Nuevo_Tipo             VARCHAR(30) = NULL,   -- opcional: se toma del modelo de la pieza instalada
    @ID_Accesorio_Retirado  BIGINT = NULL,        -- opcional: lo resuelve el SP desde el modulo retirado
    @Destino_Retirada       VARCHAR(10) = NULL,   -- 'CUSTODIA' | 'CLIENTE' (se ignora si no hay retirada)
    @Tecnico                VARCHAR(60),
    @ID_Almacen             INT,
    @ID_PreFactura          INT = NULL,
    @ID_Reparacion          INT = NULL,
    @Nuevo_Precio_Detalle   DECIMAL(18,2) = NULL,
    @Nuevo_Precio_Por_Mayor DECIMAL(18,2) = NULL,
    @Usuario                VARCHAR(60),
    @Horas_Max_Custodia     INT = 24,             -- Tarea 16: viene de appsettings Laptops:HorasMaxCustodia
    @Bloquear_Vencidos      BIT = 0,              -- Tarea 16: Laptops:BloquearTecnicoConPendientesVencidos
    @ID_Cambio              INT = NULL,           -- solo lo envia el API al replicar en la espejo (mismo ID)
    @ID_Modulo_Retirado     INT = NULL,           -- NUEVO: modulo de la laptop que sale (NULL = slot libre)
    @ID_Modulo_Instalado    INT = NULL,           -- NUEVO: solo al replicar (mismo ID del modulo nuevo)
    @Accesorios_Ids         NVARCHAR(MAX) = NULL, -- solo al replicar (JSON de accesorios creados)
    @Porcentaje_Costo_Usado DECIMAL(5,2) = 50     -- NUEVO: Laptops:PorcentajeCostoUsado (costo de la pieza usada)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- --------------------------------------------------------
        -- Replicacion en la espejo: si el cambio ya existe con ese ID, no se repite
        -- --------------------------------------------------------
        IF @ID_Cambio IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Equipo_Cambio_Pieza WHERE ID_Cambio = @ID_Cambio)
        BEGIN
            SELECT C.*,
                   Accesorios_Creados = CAST(NULL AS NVARCHAR(MAX)),
                   Mensaje = CASE WHEN C.Imei = @Imei
                                  THEN 'Este cambio de pieza ya existe'
                                  ELSE CONCAT('Error: el ID de cambio ', @ID_Cambio, ' ya lo usa otro equipo (', C.Imei, ')') END
            FROM dbo.Equipo_Cambio_Pieza C
            WHERE C.ID_Cambio = @ID_Cambio;
            RETURN;
        END

        -- --------------------------------------------------------
        -- Validaciones de parametros
        -- --------------------------------------------------------
        IF @Tipo_Pieza NOT IN ('RAM', 'ALMACENAMIENTO')
            THROW 50001, 'Tipo de pieza invalido. Use RAM o ALMACENAMIENTO.', 1;

        IF @Tecnico IS NULL OR LTRIM(RTRIM(@Tecnico)) = ''
            THROW 50004, 'El tecnico es requerido.', 1;

        IF @ID_Accesorio_Instalado IS NULL
            THROW 50005, 'Debe indicar el accesorio instalado.', 1;

        IF (@ID_Modulo_Retirado IS NOT NULL OR @ID_Accesorio_Retirado IS NOT NULL)
           AND ISNULL(@Destino_Retirada, '') NOT IN ('CUSTODIA', 'CLIENTE')
            THROW 50006, 'Destino de la pieza retirada invalido. Use CUSTODIA o CLIENTE.', 1;

        -- Tarea 16: un tecnico con piezas en custodia vencidas no puede registrar mas cambios
        -- (en la replicacion no se aplica: la base de trabajo ya lo valido)
        IF @Bloquear_Vencidos = 1 AND @ID_Cambio IS NULL
           AND EXISTS (SELECT 1
                         FROM dbo.Equipo_Cambio_Pieza P
                        WHERE P.Tecnico = @Tecnico
                          AND P.Estado_Custodia = 'PENDIENTE'
                          AND DATEDIFF(MINUTE, P.Fecha_Registro, DATEADD(HOUR, -4, GETDATE())) > @Horas_Max_Custodia * 60)
        BEGIN
            DECLARE @MsgVencidos NVARCHAR(300) = CONCAT('El tecnico ', @Tecnico, ' tiene piezas retiradas pendientes de recibir por mas de ', @Horas_Max_Custodia, ' horas. Deben recibirse antes de registrar nuevos cambios.');
            THROW 50008, @MsgVencidos, 1;
        END

        -- R2 / R3: los IDs de grupo se obtienen por descripcion una vez y se comparan por ID
        DECLARE @ID_Grupo_Laptops   INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');
        DECLARE @ID_Grupo_Componentes INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Componentes');

        IF @ID_Grupo_Laptops IS NULL OR @ID_Grupo_Componentes IS NULL
            THROW 50007, 'Faltan los grupos Laptops/Componentes. Ejecute CREATE_Laptops_Fase1_Grupos.sql.', 1;

        BEGIN TRAN;

        -- --------------------------------------------------------
        -- 1. Validar el equipo: existe, es laptop (R2), no vendido (R7), en el almacen
        --    (UPDLOCK evita que dos cambios simultaneos usen el mismo costo base)
        -- --------------------------------------------------------
        DECLARE @ID_Modelo INT, @Disponible CHAR(1), @ID_Almacen_Equipo INT, @Costo_Anterior DECIMAL(18,2),
                @Precio_Detalle_Ant DECIMAL(18,2), @Precio_Mayor_Ant DECIMAL(18,2);

        SELECT TOP 1
               @ID_Modelo = E.ID_Modelo,
               @Disponible = E.Disponible,
               @ID_Almacen_Equipo = E.ID_Almacen,
               @Costo_Anterior = CAST(E.Costo_Equipo AS DECIMAL(18,2)),
               @Precio_Detalle_Ant = CAST(E.Precio_Detalle AS DECIMAL(18,2)),
               @Precio_Mayor_Ant = CAST(E.Precio_Por_Mayor AS DECIMAL(18,2))
        FROM dbo.Equipos E WITH (UPDLOCK, ROWLOCK)
        WHERE E.Imei = @Imei;

        IF @ID_Modelo IS NULL
            THROW 50010, 'El equipo no existe.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Modelos WHERE ID_Modelo = @ID_Modelo AND ID_Grupo = @ID_Grupo_Laptops)
            THROW 50011, 'El equipo no es una laptop (su modelo no pertenece al grupo Laptops).', 1;

        IF @Disponible <> 'S'
            THROW 50012, 'La laptop no esta disponible (vendida o no disponible). Solo se permiten cambios en laptops disponibles.', 1;

        IF @ID_Almacen_Equipo <> @ID_Almacen
            THROW 50013, 'La laptop no se encuentra en el almacen indicado.', 1;

        -- Una prefactura pendiente guarda el costo de la laptop al crearse; si el costo cambia
        -- antes de procesarla, la utilidad de la venta saldria con el costo viejo.
        DECLARE @PreFactura_Pendiente INT = (
            SELECT TOP 1 pf.Numero_PreFactura
              FROM dbo.PreDetalle_Facturas pf
              INNER JOIN dbo.PreFacturas p ON p.Numero_PreFactura = pf.Numero_PreFactura
             WHERE pf.ID_Equipo = @Imei AND pf.ID_Tipo = 1 AND pf.Estado = 'P' AND p.Estado = 'P'
             ORDER BY pf.Numero_PreFactura DESC);

        -- (en la replicacion no se aplica: la base de trabajo ya lo valido)
        IF @PreFactura_Pendiente IS NOT NULL AND @ID_Cambio IS NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La laptop esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de cambiar piezas.');
            THROW 50014, @MsgPre, 1;
        END

        -- --------------------------------------------------------
        -- 2. Validar y descontar el accesorio instalado (R3: debe ser un Componente del tipo de pieza elegido)
        -- --------------------------------------------------------
        DECLARE @Costo_Instalado DECIMAL(18,2), @ID_Modelo_Inst INT, @Desc_Inst VARCHAR(100);

        SELECT @Costo_Instalado = CAST(A.Costo_Equipo AS DECIMAL(18,2)),
               @ID_Modelo_Inst  = A.ID_Modelo,
               @Desc_Inst       = A.Descripcion_Accesorio
        FROM dbo.Accesorios A
        INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
        WHERE A.ID_Accesorio = @ID_Accesorio_Instalado
          AND A.ID_Almacen = @ID_Almacen
          AND A.Estado = 'A'
          AND M.ID_Grupo = @ID_Grupo_Componentes;

        IF @Costo_Instalado IS NULL
            THROW 50020, 'El accesorio instalado no existe en este almacen, esta inactivo o no es un Componente.', 1;

        DECLARE @Pieza_Inst VARCHAR(20), @Tipo_Inst VARCHAR(30);

        SELECT @Pieza_Inst = T.Tipo_Pieza, @Tipo_Inst = T.Tipo
          FROM dbo.Componente_Modelo_Tipo T
         WHERE T.ID_Modelo = @ID_Modelo_Inst;

        IF @Pieza_Inst IS NULL
            THROW 50060, 'El modelo del accesorio instalado no es un modelo de componente reconocido (RAM / almacenamiento).', 1;

        IF @Pieza_Inst <> @Tipo_Pieza
            THROW 50061, 'La pieza instalada no corresponde al tipo de pieza elegido (RAM / almacenamiento).', 1;

        DECLARE @Cap_Inst INT = dbo.fn_Componente_Capacidad_GB(@Desc_Inst);
        IF @Cap_Inst IS NULL SET @Cap_Inst = NULLIF(@Nueva_Capacidad_GB, 0);

        IF @Cap_Inst IS NULL
            THROW 50062, 'No se pudo determinar la capacidad de la pieza; revise la descripcion del accesorio.', 1;

        UPDATE dbo.Accesorios
           SET Cantidad = Cantidad - 1
         WHERE ID_Accesorio = @ID_Accesorio_Instalado
           AND ID_Almacen = @ID_Almacen
           AND ISNULL(Cantidad, 0) > 0;

        IF @@ROWCOUNT = 0
            THROW 50021, 'No hay existencia del accesorio instalado en este almacen.', 1;

        -- --------------------------------------------------------
        -- 3. Modulo retirado y su accesorio (para custodia, inventario y costo)
        -- --------------------------------------------------------
        DECLARE @Mod_Ret_Modelo INT, @Mod_Ret_Cap INT, @Mod_Ret_Acc BIGINT, @Creados NVARCHAR(MAX) = NULL;

        -- Compatibilidad: si solo llega el accesorio retirado, se busca el modulo activo que le corresponde
        IF @ID_Modulo_Retirado IS NULL AND @ID_Accesorio_Retirado IS NOT NULL
            SELECT TOP 1 @ID_Modulo_Retirado = Mo.ID_Modulo
              FROM dbo.Equipo_Configuracion_Modulo Mo
             INNER JOIN dbo.Accesorios AR ON AR.ID_Accesorio = @ID_Accesorio_Retirado
             WHERE Mo.Imei = @Imei AND Mo.Estado = 'A' AND Mo.Tipo_Pieza = @Tipo_Pieza
               AND Mo.ID_Modelo_Componente = AR.ID_Modelo
               AND Mo.Capacidad_GB = dbo.fn_Componente_Capacidad_GB(AR.Descripcion_Accesorio)
             ORDER BY Mo.ID_Modulo;

        IF @ID_Modulo_Retirado IS NOT NULL
        BEGIN
            SELECT @Mod_Ret_Modelo = Mo.ID_Modelo_Componente, @Mod_Ret_Cap = Mo.Capacidad_GB, @Mod_Ret_Acc = Mo.ID_Accesorio
              FROM dbo.Equipo_Configuracion_Modulo Mo WITH (UPDLOCK, ROWLOCK)
             WHERE Mo.ID_Modulo = @ID_Modulo_Retirado AND Mo.Imei = @Imei AND Mo.Estado = 'A' AND Mo.Tipo_Pieza = @Tipo_Pieza;

            IF @Mod_Ret_Modelo IS NULL
                THROW 50063, 'El modulo retirado no pertenece a esta laptop, no esta instalado o no es del tipo de pieza elegido.', 1;

            IF @ID_Cambio IS NULL
            BEGIN
                -- (principal) 1) si el accesorio del modulo ya es una pieza USADA, se usa tal cual (con su propio costo)
                SET @ID_Accesorio_Retirado = NULL;

                IF @Mod_Ret_Acc IS NOT NULL
                    SELECT TOP 1 @ID_Accesorio_Retirado = X.ID_Accesorio
                      FROM dbo.Accesorios R
                     INNER JOIN dbo.Accesorios X ON X.ID_Accesorio = R.ID_Accesorio
                            OR (X.ID_Modelo = R.ID_Modelo AND X.Descripcion_Accesorio = R.Descripcion_Accesorio)
                     WHERE R.ID_Accesorio = @Mod_Ret_Acc AND R.Descripcion_Accesorio LIKE '%(Usado)'
                       AND X.ID_Almacen = @ID_Almacen
                     ORDER BY CASE WHEN X.ID_Accesorio = R.ID_Accesorio THEN 0 ELSE 1 END, X.ID_Accesorio;

                -- 2) y 3) la variante USADA del componente (mismo modelo y capacidad) del almacen; si no existe, se crea
                IF @ID_Accesorio_Retirado IS NULL
                    EXEC dbo.Accesorio_Componente_Asegurar
                         @ID_Modelo = @Mod_Ret_Modelo, @Capacidad_GB = @Mod_Ret_Cap, @ID_Almacen = @ID_Almacen,
                         @Usuario = @Usuario, @Ids_Explicitos = NULL,
                         @ID_Accesorio = @ID_Accesorio_Retirado OUTPUT, @Creados = @Creados OUTPUT,
                         @Usado = 1, @Porcentaje_Costo_Usado = @Porcentaje_Costo_Usado;
            END
            ELSE IF @Accesorios_Ids IS NOT NULL
            BEGIN
                -- (replica) se crean con los mismos ID que en la base de trabajo; @ID_Accesorio_Retirado llega ya resuelto
                DECLARE @IdDescartado BIGINT;
                EXEC dbo.Accesorio_Componente_Asegurar
                     @ID_Modelo = @Mod_Ret_Modelo, @Capacidad_GB = @Mod_Ret_Cap, @ID_Almacen = @ID_Almacen,
                     @Usuario = @Usuario, @Ids_Explicitos = @Accesorios_Ids,
                     @ID_Accesorio = @IdDescartado OUTPUT, @Creados = @Creados OUTPUT,
                     @Usado = 1, @Porcentaje_Costo_Usado = @Porcentaje_Costo_Usado;
                IF @ID_Accesorio_Retirado IS NULL SET @ID_Accesorio_Retirado = @IdDescartado;
            END
        END

        -- RAM: no se mezclan tipos (DDR3 / DDR4 / DDR5) entre los modulos que quedan y el que se instala
        IF @Tipo_Pieza = 'RAM'
           AND EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo Mo
                        WHERE Mo.Imei = @Imei AND Mo.Estado = 'A' AND Mo.Tipo_Pieza = 'RAM'
                          AND Mo.ID_Modulo <> ISNULL(@ID_Modulo_Retirado, 0) AND Mo.Tipo <> @Tipo_Inst)
            THROW 50064, 'No se pueden mezclar tipos de RAM (DDR3 / DDR4 / DDR5) en una misma laptop.', 1;

        -- Una laptop con configuracion antigua y sin modulos debe migrarse primero (Tarea 2)
        IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion C
                    WHERE C.Imei = @Imei AND (C.Ram_GB IS NOT NULL OR C.Almacenamiento_GB IS NOT NULL))
           AND NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo Mo WHERE Mo.Imei = @Imei AND Mo.Estado = 'A')
            THROW 50065, 'La laptop tiene configuracion pero no tiene modulos registrados. Ejecute la migracion de modulos (CREATE_Equipo_Configuracion_Modulo.sql).', 1;

        -- --------------------------------------------------------
        -- 4. Pieza retirada: costo de catalogo, R5
        -- --------------------------------------------------------
        DECLARE @Costo_Retirado DECIMAL(18,2) = 0;
        DECLARE @Estado_Custodia VARCHAR(20) = 'NO_APLICA';

        IF @ID_Accesorio_Retirado IS NOT NULL
        BEGIN
            SELECT @Costo_Retirado = CAST(A.Costo_Equipo AS DECIMAL(18,2))
            FROM dbo.Accesorios A
            INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
            WHERE A.ID_Accesorio = @ID_Accesorio_Retirado
              AND M.ID_Grupo = @ID_Grupo_Componentes;

            IF @@ROWCOUNT = 0
                THROW 50030, 'El accesorio retirado no existe o no es un Componente.', 1;

            SET @Estado_Custodia = CASE WHEN @Destino_Retirada = 'CLIENTE' THEN 'ENTREGADA_CLIENTE' ELSE 'PENDIENTE' END;
        END

        -- --------------------------------------------------------
        -- 5. Costo nuevo (R4 / R10): si la pieza se va con el cliente no se resta
        -- --------------------------------------------------------
        DECLARE @Costo_Nuevo DECIMAL(18,2) =
            @Costo_Anterior
            - CASE WHEN @Estado_Custodia = 'PENDIENTE' THEN @Costo_Retirado ELSE 0 END
            + @Costo_Instalado;

        IF @Costo_Nuevo < 0
            THROW 50040, 'El costo resultante de la laptop seria negativo. Verifique los costos de las piezas.', 1;

        -- Precios: el costo nuevo no puede superar los precios de venta
        DECLARE @Precio_Detalle_Nuevo DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Detalle, @Precio_Detalle_Ant);
        DECLARE @Precio_Mayor_Nuevo   DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Por_Mayor, @Precio_Mayor_Ant);
        DECLARE @MsgPrecio NVARCHAR(400);

        IF @Precio_Detalle_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio al detalle (', @Precio_Detalle_Nuevo, '). Indique un precio al detalle nuevo.');
            THROW 50041, @MsgPrecio, 1;
        END

        IF @Precio_Mayor_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio por mayor (', @Precio_Mayor_Nuevo, '). Indique un precio por mayor nuevo.');
            THROW 50042, @MsgPrecio, 1;
        END

        IF (@Nuevo_Precio_Detalle IS NOT NULL OR @Nuevo_Precio_Por_Mayor IS NOT NULL)
           AND @Precio_Mayor_Nuevo > @Precio_Detalle_Nuevo
            THROW 50043, 'El precio por mayor no puede ser mayor que el precio al detalle.', 1;

        -- Foto de la fila actual en el historico de equipos (antes de modificar costo/precios)
        INSERT INTO dbo.Equipos_Historico
            (Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
             Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
             Nota_Historico, Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
             Usuario_Historico, Fecha_registro_Historico, Comision_Detalle, Comision_Por_Mayor)
        SELECT Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
               Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
               'Cambio de pieza de laptop: ajuste de costo y/o precios', Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
               @Usuario, DATEADD(HOUR, -4, GETDATE()), Comision_Detalle, Comision_Por_Mayor
          FROM dbo.Equipos
         WHERE Imei = @Imei;

        UPDATE dbo.Equipos
           SET Costo_Equipo = @Costo_Nuevo,
               Precio_Detalle = @Precio_Detalle_Nuevo,
               Precio_Por_Mayor = @Precio_Mayor_Nuevo
         WHERE Imei = @Imei;

        -- --------------------------------------------------------
        -- 6. Modulos y configuracion de la laptop
        -- --------------------------------------------------------
        DECLARE @Config_Anterior VARCHAR(100) = dbo.fn_Equipo_Configuracion_Texto(@Imei);

        IF NOT EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion WHERE Imei = @Imei)
        BEGIN
            INSERT INTO dbo.Equipo_Configuracion (Imei, Usuario, Fecha_Registro)
            VALUES (@Imei, @Usuario, DATEADD(HOUR, -4, GETDATE()));
        END

        IF @ID_Modulo_Retirado IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET Estado = 'R' WHERE ID_Modulo = @ID_Modulo_Retirado;

        DECLARE @ID_Modulo_Nuevo INT;

        IF @ID_Modulo_Instalado IS NULL
        BEGIN
            INSERT INTO dbo.Equipo_Configuracion_Modulo
                (Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, ID_Accesorio, Estado, Usuario, Fecha_Registro)
            VALUES
                (@Imei, @Tipo_Pieza, @Cap_Inst, @ID_Modelo_Inst, @Tipo_Inst, @ID_Accesorio_Instalado, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));
            SET @ID_Modulo_Nuevo = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            -- Replicacion: mismo ID de modulo que en la base de trabajo
            IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo WHERE ID_Modulo = @ID_Modulo_Instalado)
                THROW 50066, 'Error: el ID de modulo indicado ya existe en esta base.', 1;

            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo ON;
            INSERT INTO dbo.Equipo_Configuracion_Modulo
                (ID_Modulo, Imei, Tipo_Pieza, Capacidad_GB, ID_Modelo_Componente, Tipo, ID_Accesorio, Estado, Usuario, Fecha_Registro)
            VALUES
                (@ID_Modulo_Instalado, @Imei, @Tipo_Pieza, @Cap_Inst, @ID_Modelo_Inst, @Tipo_Inst, @ID_Accesorio_Instalado, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));
            SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo OFF;
            SET @ID_Modulo_Nuevo = @ID_Modulo_Instalado;
        END

        EXEC dbo.Equipo_Configuracion_Recalcular @Imei = @Imei, @Usuario = @Usuario;

        DECLARE @Config_Nueva VARCHAR(100) = dbo.fn_Equipo_Configuracion_Texto(@Imei);

        -- --------------------------------------------------------
        -- 7. Historial + custodia
        -- --------------------------------------------------------
        DECLARE @ID_Nuevo INT;

        IF @ID_Cambio IS NULL
        BEGIN
            INSERT INTO dbo.Equipo_Cambio_Pieza
            (Imei, Tipo_Pieza, ID_Accesorio_Instalado, Costo_Instalado, ID_Accesorio_Retirado, Costo_Retirado,
             Config_Anterior, Config_Nueva, Costo_Equipo_Anterior, Costo_Equipo_Nuevo, ID_Almacen,
             ID_PreFactura, ID_Reparacion, Tecnico, Estado_Custodia, Usuario_Registro, Fecha_Registro,
             ID_Modulo_Retirado, ID_Modulo_Instalado)
            VALUES
            (@Imei, @Tipo_Pieza, @ID_Accesorio_Instalado, @Costo_Instalado, @ID_Accesorio_Retirado, @Costo_Retirado,
             @Config_Anterior, @Config_Nueva, @Costo_Anterior, @Costo_Nuevo, @ID_Almacen,
             @ID_PreFactura, @ID_Reparacion, @Tecnico, @Estado_Custodia, @Usuario, DATEADD(HOUR, -4, GETDATE()),
             @ID_Modulo_Retirado, @ID_Modulo_Nuevo);

            SET @ID_Nuevo = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            -- Replicacion: mismo ID que en la base de trabajo
            SET IDENTITY_INSERT dbo.Equipo_Cambio_Pieza ON;

            INSERT INTO dbo.Equipo_Cambio_Pieza
                (ID_Cambio, Imei, Tipo_Pieza, ID_Accesorio_Instalado, Costo_Instalado, ID_Accesorio_Retirado, Costo_Retirado,
             Config_Anterior, Config_Nueva, Costo_Equipo_Anterior, Costo_Equipo_Nuevo, ID_Almacen,
             ID_PreFactura, ID_Reparacion, Tecnico, Estado_Custodia, Usuario_Registro, Fecha_Registro,
             ID_Modulo_Retirado, ID_Modulo_Instalado)
            VALUES
                (@ID_Cambio, @Imei, @Tipo_Pieza, @ID_Accesorio_Instalado, @Costo_Instalado, @ID_Accesorio_Retirado, @Costo_Retirado,
             @Config_Anterior, @Config_Nueva, @Costo_Anterior, @Costo_Nuevo, @ID_Almacen,
             @ID_PreFactura, @ID_Reparacion, @Tecnico, @Estado_Custodia, @Usuario, DATEADD(HOUR, -4, GETDATE()),
             @ID_Modulo_Retirado, @ID_Modulo_Nuevo);

            SET IDENTITY_INSERT dbo.Equipo_Cambio_Pieza OFF;
            SET @ID_Nuevo = @ID_Cambio;
        END

        -- El cambio que dio de baja / alta a cada modulo
        IF @ID_Modulo_Retirado IS NOT NULL
            UPDATE dbo.Equipo_Configuracion_Modulo SET ID_Cambio_Baja = @ID_Nuevo WHERE ID_Modulo = @ID_Modulo_Retirado;

        UPDATE dbo.Equipo_Configuracion_Modulo SET ID_Cambio_Alta = @ID_Nuevo WHERE ID_Modulo = @ID_Modulo_Nuevo;

        -- Precios de venta anteriores (para poder restablecerlos si el cambio se anula)
        UPDATE dbo.Equipo_Cambio_Pieza
           SET Precio_Detalle_Anterior = @Precio_Detalle_Ant,
               Precio_Por_Mayor_Anterior = @Precio_Mayor_Ant
         WHERE ID_Cambio = @ID_Nuevo;

        COMMIT TRAN;

        -- --------------------------------------------------------
        -- 8. Devolver el registro insertado (Accesorios_Creados = JSON para replicar los mismos ID)
        -- --------------------------------------------------------
        SELECT C.*,
               Accesorios_Creados = @Creados,
               Mensaje = 'Cambio de pieza registrado satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Nuevo;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        SET IDENTITY_INSERT dbo.Equipo_Cambio_Pieza OFF;
        SET IDENTITY_INSERT dbo.Equipo_Configuracion_Modulo OFF;
        SET IDENTITY_INSERT dbo.Accesorios OFF;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Recibir
    @ID_Cambio         INT,
    @Usuario           VARCHAR(60),
    @Nota              VARCHAR(250) = NULL,
    @Costo_Equipo      DECIMAL(18,2) = NULL,     -- solo si el accesorio no tiene costo / precios
    @Precio_Por_Mayor  DECIMAL(18,2) = NULL,
    @Precio_Detalle    DECIMAL(18,2) = NULL,
    @ID_Accesorio_Nuevo BIGINT = NULL,           -- solo al replicar (mismo ID del accesorio creado, Tarea 4)
    @Accesorios_Ids     NVARCHAR(MAX) = NULL,    -- NUEVO: solo al replicar (JSON de las piezas usadas creadas)
    @Porcentaje_Costo_Usado DECIMAL(5,2) = 50    -- NUEVO: Laptops:PorcentajeCostoUsado
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
        DECLARE @ID_Accesorio_Destino BIGINT, @ID_Accesorio_Creado BIGINT = NULL, @Creados NVARCHAR(MAX) = NULL;

        -- Cambios anteriores a la Tarea 5: el accesorio retirado es la pieza NUEVA; la pieza entra a su variante USADA
        DECLARE @R_Modelo INT, @R_Desc VARCHAR(100), @R_Cap INT;
        SELECT @R_Modelo = A.ID_Modelo, @R_Desc = A.Descripcion_Accesorio
          FROM dbo.Accesorios A
         WHERE A.ID_Accesorio = @ID_Accesorio_Retirado;
        SET @R_Cap = dbo.fn_Componente_Capacidad_GB(@R_Desc);

        IF @R_Modelo IS NOT NULL AND @R_Cap IS NOT NULL AND @R_Desc NOT LIKE '%(Usado)'
           AND EXISTS (SELECT 1 FROM dbo.Componente_Modelo_Tipo WHERE ID_Modelo = @R_Modelo)
            EXEC dbo.Accesorio_Componente_Asegurar
                 @ID_Modelo = @R_Modelo, @Capacidad_GB = @R_Cap, @ID_Almacen = @ID_Almacen,
                 @Usuario = @Usuario, @Ids_Explicitos = @Accesorios_Ids,
                 @ID_Accesorio = @ID_Accesorio_Destino OUTPUT, @Creados = @Creados OUTPUT,
                 @Usado = 1, @Porcentaje_Costo_Usado = @Porcentaje_Costo_Usado;

        IF @ID_Accesorio_Destino IS NULL
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
        BEGIN
            -- No existe en el almacen del cambio: se crea copiando el accesorio retirado (Cantidad = 0)
            IF NOT EXISTS (SELECT 1 FROM dbo.Accesorios WHERE ID_Accesorio = @ID_Accesorio_Retirado)
                THROW 50105, 'El accesorio retirado ya no existe en el catalogo; no se puede recibir la pieza.', 1;

            IF @ID_Accesorio_Nuevo IS NOT NULL
               AND EXISTS (SELECT 1 FROM dbo.Accesorios WHERE ID_Accesorio = @ID_Accesorio_Nuevo)
                THROW 50110, 'Error: el ID de accesorio indicado ya existe en esta base.', 1;

            IF @ID_Accesorio_Nuevo IS NULL
            BEGIN
                INSERT INTO dbo.Accesorios
                    (Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura, Descripcion_Accesorio, Cantidad, Can_Max, Can_Min,
                     Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado, Usuario)
                SELECT O.Imei_Equipo, O.ID_Modelo, @ID_Almacen, O.Numero_de_Factura, O.Descripcion_Accesorio, 0, O.Can_Max, O.Can_Min,
                       O.Precio_Por_Mayor, O.Precio_Detalle, O.Costo_Equipo, O.Disponible, O.Disponible_Detalle, O.Estado, 'Sistema'
                  FROM dbo.Accesorios O
                 WHERE O.ID_Accesorio = @ID_Accesorio_Retirado;
                SET @ID_Accesorio_Creado = SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                -- Replicacion: mismo ID que en la base de trabajo
                SET IDENTITY_INSERT dbo.Accesorios ON;
                INSERT INTO dbo.Accesorios
                    (ID_Accesorio, Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura, Descripcion_Accesorio, Cantidad, Can_Max, Can_Min,
                     Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado, Usuario)
                SELECT @ID_Accesorio_Nuevo, O.Imei_Equipo, O.ID_Modelo, @ID_Almacen, O.Numero_de_Factura, O.Descripcion_Accesorio, 0, O.Can_Max, O.Can_Min,
                       O.Precio_Por_Mayor, O.Precio_Detalle, O.Costo_Equipo, O.Disponible, O.Disponible_Detalle, O.Estado, 'Sistema'
                  FROM dbo.Accesorios O
                 WHERE O.ID_Accesorio = @ID_Accesorio_Retirado;
                SET IDENTITY_INSERT dbo.Accesorios OFF;
                SET @ID_Accesorio_Creado = @ID_Accesorio_Nuevo;
            END

            UPDATE dbo.Accesorios SET Codigo_Barra = 1000000000 + @ID_Accesorio_Creado WHERE ID_Accesorio = @ID_Accesorio_Creado;
            SET @ID_Accesorio_Destino = @ID_Accesorio_Creado;
        END

        -- --------------------------------------------------------
        -- Costo y precios del accesorio (obligatorios si no los tiene)
        -- --------------------------------------------------------
        DECLARE @ID_Modelo_Acc INT, @Desc_Acc VARCHAR(100), @Costo_Acc MONEY, @Mayor_Acc MONEY, @Detalle_Acc MONEY;

        SELECT @ID_Modelo_Acc = A.ID_Modelo, @Desc_Acc = A.Descripcion_Accesorio,
               @Costo_Acc = A.Costo_Equipo, @Mayor_Acc = A.Precio_Por_Mayor, @Detalle_Acc = A.Precio_Detalle
          FROM dbo.Accesorios A
         WHERE A.ID_Accesorio = @ID_Accesorio_Destino;

        IF ISNULL(@Costo_Acc, 0) <= 0 OR ISNULL(@Mayor_Acc, 0) <= 0 OR ISNULL(@Detalle_Acc, 0) <= 0
        BEGIN
            IF ISNULL(@Costo_Equipo, 0) <= 0 OR ISNULL(@Precio_Por_Mayor, 0) <= 0 OR ISNULL(@Precio_Detalle, 0) <= 0
                THROW 50107, 'El accesorio no tiene costo y precios registrados. Indique el costo, el precio por mayor y el precio al detalle.', 1;

            IF @Costo_Equipo > @Precio_Por_Mayor
                THROW 50108, 'El costo no puede ser mayor que el precio por mayor.', 1;

            IF @Precio_Por_Mayor > @Precio_Detalle
                THROW 50109, 'El precio por mayor no puede ser mayor que el precio al detalle.', 1;

            -- Todos los almacenes con el mismo modelo y descripcion; el accesorio queda activo
            UPDATE dbo.Accesorios
               SET Costo_Equipo     = @Costo_Equipo,
                   Precio_Por_Mayor = @Precio_Por_Mayor,
                   Precio_Detalle   = @Precio_Detalle,
                   Estado           = 'A',
                   Usuario          = LEFT(@Usuario, 50)
             WHERE ID_Modelo = @ID_Modelo_Acc
               AND Descripcion_Accesorio = @Desc_Acc;
        END

        -- --------------------------------------------------------
        -- Sumar 1 al inventario y marcar como recibida
        -- --------------------------------------------------------
        UPDATE dbo.Accesorios
           SET Cantidad = ISNULL(Cantidad, 0) + 1
         WHERE ID_Accesorio = @ID_Accesorio_Destino;

        UPDATE dbo.Equipo_Cambio_Pieza
           SET Estado_Custodia = 'RECIBIDA',
               Usuario_Recepcion = @Usuario,
               Fecha_Recepcion = DATEADD(HOUR, -4, GETDATE()),
               Nota_Recepcion = @Nota
         WHERE ID_Cambio = @ID_Cambio
           AND Estado_Custodia = 'PENDIENTE';

        IF @@ROWCOUNT = 0
            THROW 50106, 'La pieza ya fue procesada por otro usuario.', 1;

        COMMIT TRAN;

        SELECT C.*, Accesorio_Creado = @ID_Accesorio_Creado, Accesorios_Creados = @Creados, Mensaje = 'Pieza recibida satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        SET IDENTITY_INSERT dbo.Accesorios OFF;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_EntregarCliente
    @ID_Cambio              INT,
    @Usuario                VARCHAR(60),
    @Nota                   VARCHAR(250) = NULL,
    @Nuevo_Precio_Detalle   DECIMAL(18,2) = NULL,
    @Nuevo_Precio_Por_Mayor DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Usuario IS NULL OR LTRIM(RTRIM(@Usuario)) = ''
            THROW 50200, 'El usuario es requerido.', 1;

        BEGIN TRAN;

        DECLARE @Estado VARCHAR(20), @Imei VARCHAR(30), @Costo_Retirado DECIMAL(18,2);

        SELECT @Estado = C.Estado_Custodia,
               @Imei = C.Imei,
               @Costo_Retirado = C.Costo_Retirado
        FROM dbo.Equipo_Cambio_Pieza C WITH (UPDLOCK, ROWLOCK)
        WHERE C.ID_Cambio = @ID_Cambio;

        IF @Estado IS NULL
            THROW 50201, 'El cambio de pieza no existe.', 1;

        IF @Estado <> 'PENDIENTE'
            THROW 50202, 'Solo se puede entregar al cliente una pieza en estado PENDIENTE.', 1;

        -- Laptop: existe y no esta vendida
        DECLARE @Disponible CHAR(1), @Costo_Actual DECIMAL(18,2),
                @Precio_Detalle DECIMAL(18,2), @Precio_Mayor DECIMAL(18,2);

        SELECT TOP 1
               @Disponible = E.Disponible,
               @Costo_Actual = CAST(E.Costo_Equipo AS DECIMAL(18,2)),
               @Precio_Detalle = CAST(E.Precio_Detalle AS DECIMAL(18,2)),
               @Precio_Mayor = CAST(E.Precio_Por_Mayor AS DECIMAL(18,2))
        FROM dbo.Equipos E WITH (UPDLOCK, ROWLOCK)
        WHERE E.Imei = @Imei;

        IF @Disponible IS NULL
            THROW 50203, 'La laptop del cambio no existe.', 1;

        IF @Disponible <> 'S'
            THROW 50204, 'La laptop ya no esta disponible (vendida o no disponible). No se puede entregar la pieza al cliente.', 1;

        -- Una prefactura pendiente guarda el costo de la laptop al crearse; no se puede cambiar el costo
        -- antes de procesarla (la utilidad saldria con el costo viejo).
        DECLARE @PreFactura_Pendiente INT = (
            SELECT TOP 1 pf.Numero_PreFactura
              FROM dbo.PreDetalle_Facturas pf
              INNER JOIN dbo.PreFacturas p ON p.Numero_PreFactura = pf.Numero_PreFactura
             WHERE pf.ID_Equipo = @Imei AND pf.ID_Tipo = 1 AND pf.Estado = 'P' AND p.Estado = 'P'
             ORDER BY pf.Numero_PreFactura DESC);

        IF @PreFactura_Pendiente IS NOT NULL
        BEGIN
            DECLARE @MsgPre NVARCHAR(300) = CONCAT('La laptop esta en la prefactura pendiente #', @PreFactura_Pendiente, '. Procesela o anulela antes de entregar la pieza al cliente.');
            THROW 50206, @MsgPre, 1;
        END

        -- R10: se vuelve a sumar el costo de la pieza que se va con la laptop
        DECLARE @Costo_Nuevo DECIMAL(18,2) = @Costo_Actual + @Costo_Retirado;

        -- Precios: el costo nuevo no puede superar los precios de venta
        DECLARE @Precio_Detalle_Nuevo DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Detalle, @Precio_Detalle);
        DECLARE @Precio_Mayor_Nuevo   DECIMAL(18,2) = ISNULL(@Nuevo_Precio_Por_Mayor, @Precio_Mayor);
        DECLARE @MsgPrecio NVARCHAR(400);

        IF @Precio_Detalle_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio al detalle (', @Precio_Detalle_Nuevo, '). Indique un precio al detalle nuevo.');
            THROW 50241, @MsgPrecio, 1;
        END

        IF @Precio_Mayor_Nuevo < @Costo_Nuevo
        BEGIN
            SET @MsgPrecio = CONCAT('El costo nuevo (', @Costo_Nuevo, ') supera el precio por mayor (', @Precio_Mayor_Nuevo, '). Indique un precio por mayor nuevo.');
            THROW 50242, @MsgPrecio, 1;
        END

        IF (@Nuevo_Precio_Detalle IS NOT NULL OR @Nuevo_Precio_Por_Mayor IS NOT NULL)
           AND @Precio_Mayor_Nuevo > @Precio_Detalle_Nuevo
            THROW 50243, 'El precio por mayor no puede ser mayor que el precio al detalle.', 1;

        -- Foto de la fila actual en el historico de equipos (antes de modificar costo/precios)
        INSERT INTO dbo.Equipos_Historico
            (Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
             Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
             Nota_Historico, Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
             Usuario_Historico, Fecha_registro_Historico, Comision_Detalle, Comision_Por_Mayor)
        SELECT Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion, ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor,
               Numero_de_Factura, Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado, Disponible, Nota_Adicional,
               'Pieza retirada entregada al cliente: se suma su costo a la laptop', Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
               @Usuario, DATEADD(HOUR, -4, GETDATE()), Comision_Detalle, Comision_Por_Mayor
          FROM dbo.Equipos
         WHERE Imei = @Imei;

        UPDATE dbo.Equipos
           SET Costo_Equipo = @Costo_Nuevo,
               Precio_Detalle = @Precio_Detalle_Nuevo,
               Precio_Por_Mayor = @Precio_Mayor_Nuevo
         WHERE Imei = @Imei;

        UPDATE dbo.Equipo_Cambio_Pieza
           SET Estado_Custodia = 'ENTREGADA_CLIENTE',
               Usuario_Recepcion = @Usuario,
               Fecha_Recepcion = DATEADD(HOUR, -4, GETDATE()),
               Nota_Recepcion = @Nota
         WHERE ID_Cambio = @ID_Cambio
           AND Estado_Custodia = 'PENDIENTE';

        IF @@ROWCOUNT = 0
            THROW 50205, 'La pieza ya fue procesada por otro usuario.', 1;

        COMMIT TRAN;

        SELECT C.*, Costo_Equipo_Actual = @Costo_Nuevo, Mensaje = 'Pieza entregada al cliente satisfactoriamente'
        FROM dbo.Equipo_Cambio_Pieza C
        WHERE C.ID_Cambio = @ID_Cambio;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Pendientes
    @Tecnico     VARCHAR(60) = NULL,
    @ID_Almacen  INT = NULL,
    @Horas_Max   INT = 24
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Cambio,
           C.Imei,
           Modelo = M.Descripcion,
           Marca = Ma.Descripcion,
           Laptop_Disponible = E.Disponible,
           C.Tipo_Pieza,
           C.ID_Accesorio_Retirado,
           Pieza_Retirada = AR.Descripcion_Accesorio,
           C.Costo_Retirado,
           C.Tecnico,
           C.ID_Almacen,
           AC.Descripcion_Almacen,
           C.Usuario_Registro,
           C.Fecha_Registro,
           Horas_Transcurridas = CAST(DATEDIFF(MINUTE, C.Fecha_Registro, DATEADD(HOUR, -4, GETDATE())) / 60.0 AS DECIMAL(10,1)),
           Vencido = CAST(CASE WHEN DATEDIFF(MINUTE, C.Fecha_Registro, DATEADD(HOUR, -4, GETDATE())) > @Horas_Max * 60 THEN 1 ELSE 0 END AS BIT),
           -- NUEVO: el accesorio retirado no tiene costo y precios; la recepcion los pide
           Requiere_Precios = CAST(CASE WHEN ISNULL(AR.Costo_Equipo, 0) <= 0
                                          OR ISNULL(AR.Precio_Por_Mayor, 0) <= 0
                                          OR ISNULL(AR.Precio_Detalle, 0) <= 0
                                        THEN 1 ELSE 0 END AS BIT)
    FROM dbo.Equipo_Cambio_Pieza C
    INNER JOIN dbo.Equipos E ON E.Imei = C.Imei
    INNER JOIN dbo.Modelos M ON M.ID_Modelo = E.ID_Modelo
    INNER JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
    LEFT JOIN dbo.Accesorios AR ON AR.ID_Accesorio = C.ID_Accesorio_Retirado
    LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = C.ID_Almacen
    WHERE C.Estado_Custodia = 'PENDIENTE'
      AND (@Tecnico IS NULL OR C.Tecnico = @Tecnico)
      AND (@ID_Almacen IS NULL OR C.ID_Almacen = @ID_Almacen)
    ORDER BY C.Tecnico, C.Fecha_Registro, C.ID_Cambio;
END
GO

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
               Fecha_Anula     = DATEADD(HOUR, -4, GETDATE()),
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

CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Comprobante
    @ID_Cambio INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Cambio,
           C.Fecha_Registro,
           C.Imei,
           Marca = Ma.Descripcion,
           Modelo = M.Descripcion,
           C.Tipo_Pieza,
           Pieza_Retirada = AR.Descripcion_Accesorio,
           Modelo_Retirada = MR.Descripcion,
           Pieza_Instalada = AI.Descripcion_Accesorio,
           Modelo_Instalada = MI.Descripcion,
           C.Config_Anterior,
           C.Config_Nueva,
           C.Costo_Retirado,
           C.Tecnico,
           C.Usuario_Registro,
           C.Estado_Custodia,
           C.ID_Almacen,
           AC.Descripcion_Almacen
      FROM dbo.Equipo_Cambio_Pieza C
      LEFT JOIN dbo.Equipos E ON E.Imei = C.Imei
      LEFT JOIN dbo.Modelos M ON M.ID_Modelo = E.ID_Modelo
      LEFT JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
      LEFT JOIN dbo.Accesorios AR ON AR.ID_Accesorio = C.ID_Accesorio_Retirado
      LEFT JOIN dbo.Modelos MR ON MR.ID_Modelo = AR.ID_Modelo
      LEFT JOIN dbo.Accesorios AI ON AI.ID_Accesorio = C.ID_Accesorio_Instalado
      LEFT JOIN dbo.Modelos MI ON MI.ID_Modelo = AI.ID_Modelo
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = C.ID_Almacen
     WHERE C.ID_Cambio = @ID_Cambio;
END
GO


-- ############################################################
-- 7. KARDEX DE ACCESORIOS Y REPORTE DE CONSUMO DE COMPONENTES
-- ############################################################

-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 7 (kardex por accesorio y reporte general de consumo)
-- Proposito: ver que movimientos tuvo un accesorio (y los componentes en general) y por que.
--   Movimientos que el sistema REGISTRA (compras, ajustes manuales y transferencias de accesorios no dejan registro):
--     VENTA        (Salida)  : detalle de factura de accesorios (ID_Tipo = 2) de facturas vigentes (no anuladas).
--     CAMBIO_PIEZA (Salida)  : pieza instalada en un cambio de pieza de laptop (no anulado).
--     RECEPCION    (Entrada) : pieza retirada recibida en custodia (entra a la variante en el almacen del cambio).
--   1. dbo.fn_Accesorio_Movimientos(): todos esos movimientos en una sola lista (la usan el kardex y el reporte).
--   2. Accesorio_Kardex_Buscar: busca accesorios por descripcion / modelo / codigo (para elegir cual consultar).
--   3. Accesorio_Kardex_Consultar @ID_Accesorio, @Fecha_Inicio, @Fecha_Final: lista ordenada por fecha con saldo.
--        El saldo es REFERENCIAL: se calcula hacia atras desde la existencia actual y los movimientos registrados
--        (Saldo inicial = existencia actual - movimientos registrados desde la fecha de inicio). No incluye lo que el
--        sistema no registra (compras, ajustes, transferencias).
--   4. Componentes_Movimientos_Consultar: reporte general de consumo de componentes con filtros por fecha, almacen,
--        tecnico, tipo de pieza (RAM / ALMACENAMIENTO) y motivo.
--   Nota: todas las fechas estan en hora local (servidor - 4 horas). Los cambios de pieza anteriores a
--   UPDATE_Laptops_Fechas_Hora_Local.sql (Tarea 9) quedaron con la hora del servidor.
-- Requiere: scripts de las Tareas 1 a 6 ejecutados ANTES (usa Equipo_Cambio_Pieza y Componente_Modelo_Tipo).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER). Solo consulta: no modifica datos.
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================

-- ------------------------------------------------------------
-- 1. Movimientos registrados de los accesorios
-- ------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.fn_Accesorio_Movimientos ()
RETURNS TABLE
AS
RETURN
(
    -- Ventas: facturas vigentes
    SELECT A.ID_Accesorio,
           Fecha     = F.Fecha_registro,
           Motivo    = CAST('VENTA' AS VARCHAR(20)),
           Documento = CAST(LTRIM(RTRIM(ISNULL(F.Literal, ''))) + CONVERT(VARCHAR(10), F.Numero_Factura) AS VARCHAR(40)),
           Entrada   = CAST(0 AS INT),
           Salida    = CAST(DF.Cantidad AS INT),
           Usuario   = CAST(F.Usuario AS VARCHAR(60)),
           Tecnico   = CAST(NULL AS VARCHAR(60))
      FROM dbo.Detalle_Facturas DF
     INNER JOIN dbo.Facturas F
             ON F.Numero_Factura = DF.Numero_Factura
            AND DF.LiteralF = ISNULL(F.Literal, '') + CONVERT(VARCHAR(10), F.Numero_Factura)
     INNER JOIN dbo.Accesorios A ON A.ID_Accesorio = TRY_CAST(DF.ID_Equipo AS BIGINT)
     WHERE DF.ID_Tipo = 2
       AND DF.Estado = 'A'
       AND F.Estado = 'A'

    UNION ALL

    -- Cambio de pieza: la pieza instalada sale del inventario
    SELECT C.ID_Accesorio_Instalado,
           C.Fecha_Registro,
           'CAMBIO_PIEZA',
           CAST(C.Imei AS VARCHAR(40)),
           0,
           1,
           CAST(C.Usuario_Registro AS VARCHAR(60)),
           CAST(C.Tecnico AS VARCHAR(60))
      FROM dbo.Equipo_Cambio_Pieza C
     WHERE C.Estado_Custodia <> 'ANULADO'
       AND C.ID_Accesorio_Instalado IS NOT NULL

    UNION ALL

    -- Recepcion de la pieza retirada: entra a la variante del almacen del cambio
    -- (la pieza usada; en cambios anteriores a la Tarea 5 puede ser la misma pieza o su variante '(Usado)')
    SELECT A.ID_Accesorio,
           C.Fecha_Recepcion,
           'RECEPCION',
           CAST(C.Imei AS VARCHAR(40)),
           1,
           0,
           CAST(C.Usuario_Recepcion AS VARCHAR(60)),
           CAST(C.Tecnico AS VARCHAR(60))
      FROM dbo.Equipo_Cambio_Pieza C
     INNER JOIN dbo.Accesorios R ON R.ID_Accesorio = C.ID_Accesorio_Retirado
     INNER JOIN dbo.Accesorios A
             ON A.ID_Almacen = C.ID_Almacen
            AND (A.ID_Accesorio = R.ID_Accesorio
                 OR (A.ID_Modelo = R.ID_Modelo
                     AND (A.Descripcion_Accesorio = R.Descripcion_Accesorio
                          OR (R.Descripcion_Accesorio NOT LIKE '%(Usado)'
                              AND A.Descripcion_Accesorio = R.Descripcion_Accesorio + ' (Usado)'))))
     WHERE C.Estado_Custodia = 'RECIBIDA'
       AND C.Fecha_Recepcion IS NOT NULL
       -- Pieza que aun no era usada: si existe su variante '(Usado)' en el almacen, la entrada fue a esa variante
       AND (R.Descripcion_Accesorio LIKE '%(Usado)'
            OR A.Descripcion_Accesorio LIKE '%(Usado)'
            OR NOT EXISTS (SELECT 1 FROM dbo.Accesorios U
                            WHERE U.ID_Almacen = C.ID_Almacen AND U.ID_Modelo = R.ID_Modelo
                              AND U.Descripcion_Accesorio = R.Descripcion_Accesorio + ' (Usado)'))
)
GO

-- ------------------------------------------------------------
-- 2. Buscar accesorios para el kardex
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Accesorio_Kardex_Buscar
    @ID_Almacen INT = NULL,
    @Filtro     VARCHAR(100) = ''
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 100
           A.ID_Accesorio,
           A.Descripcion_Accesorio,
           Modelo = M.Descripcion,
           A.ID_Almacen,
           AC.Descripcion_Almacen,
           Cantidad = ISNULL(A.Cantidad, 0),
           A.Estado
      FROM dbo.Accesorios A
     INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = A.ID_Almacen
     WHERE (@ID_Almacen IS NULL OR A.ID_Almacen = @ID_Almacen)
       AND (ISNULL(@Filtro, '') = ''
            OR A.Descripcion_Accesorio LIKE '%' + @Filtro + '%'
            OR M.Descripcion LIKE '%' + @Filtro + '%'
            OR CONVERT(VARCHAR(30), A.Codigo_Barra) = @Filtro
            OR CONVERT(VARCHAR(30), A.ID_Accesorio) = @Filtro)
     ORDER BY M.Descripcion, A.Descripcion_Accesorio, AC.Descripcion_Almacen;
END
GO

-- ------------------------------------------------------------
-- 3. Kardex de un accesorio (saldo referencial)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Accesorio_Kardex_Consultar
    @ID_Accesorio BIGINT,
    @Fecha_Inicio DATETIME,
    @Fecha_Final  DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Existencia INT, @Descripcion VARCHAR(100), @Almacen VARCHAR(100);

    SELECT @Existencia = ISNULL(A.Cantidad, 0),
           @Descripcion = A.Descripcion_Accesorio,
           @Almacen = AC.Descripcion_Almacen
      FROM dbo.Accesorios A
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = A.ID_Almacen
     WHERE A.ID_Accesorio = @ID_Accesorio;

    IF @Existencia IS NULL
        THROW 50700, 'El accesorio no existe.', 1;

    DECLARE @Ini DATETIME = CAST(CAST(ISNULL(@Fecha_Inicio, '19000101') AS DATE) AS DATETIME);
    DECLARE @FinExcl DATETIME = DATEADD(DAY, 1, CAST(CAST(ISNULL(@Fecha_Final, DATEADD(HOUR, -4, GETDATE())) AS DATE) AS DATETIME));

    -- Saldo inicial referencial = existencia actual - lo registrado desde la fecha de inicio
    DECLARE @NetoDesdeInicio INT =
        (SELECT ISNULL(SUM(Mv.Entrada - Mv.Salida), 0)
           FROM dbo.fn_Accesorio_Movimientos() Mv
          WHERE Mv.ID_Accesorio = @ID_Accesorio AND Mv.Fecha >= @Ini);
    DECLARE @SaldoInicial INT = @Existencia - @NetoDesdeInicio;

    ;WITH M AS (
        SELECT Mv.Fecha, Mv.Motivo, Mv.Documento, Mv.Entrada, Mv.Salida, Mv.Usuario
          FROM dbo.fn_Accesorio_Movimientos() Mv
         WHERE Mv.ID_Accesorio = @ID_Accesorio AND Mv.Fecha >= @Ini AND Mv.Fecha < @FinExcl
    ),
    S AS (
        SELECT M.*,
               Saldo = @SaldoInicial + SUM(M.Entrada - M.Salida)
                       OVER (ORDER BY M.Fecha, M.Motivo, M.Documento ROWS UNBOUNDED PRECEDING)
          FROM M
    )
    SELECT X.Fecha, X.Motivo, X.Documento, X.Entrada, X.Salida, X.Usuario, X.Saldo,
           Descripcion_Accesorio = @Descripcion, Descripcion_Almacen = @Almacen, Existencia_Actual = @Existencia
      FROM (
            SELECT Orden = 0, Fecha = @Ini, Motivo = CAST('SALDO_INICIAL' AS VARCHAR(20)), Documento = CAST(NULL AS VARCHAR(40)),
                   Entrada = 0, Salida = 0, Usuario = CAST(NULL AS VARCHAR(60)), Saldo = @SaldoInicial
            UNION ALL
            SELECT 1, S.Fecha, S.Motivo, S.Documento, S.Entrada, S.Salida, S.Usuario, S.Saldo FROM S
           ) X
     ORDER BY X.Orden, X.Fecha, X.Motivo, X.Documento;
END
GO

-- ------------------------------------------------------------
-- 4. Reporte general de consumo de componentes
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Componentes_Movimientos_Consultar
    @Fecha_Inicio DATETIME,
    @Fecha_Final  DATETIME,
    @ID_Almacen   INT = NULL,
    @Tecnico      VARCHAR(60) = NULL,
    @Tipo_Pieza   VARCHAR(20) = NULL,    -- 'RAM' | 'ALMACENAMIENTO'
    @Motivo       VARCHAR(20) = NULL     -- 'VENTA' | 'CAMBIO_PIEZA' | 'RECEPCION'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Ini DATETIME = CAST(CAST(ISNULL(@Fecha_Inicio, '19000101') AS DATE) AS DATETIME);
    DECLARE @FinExcl DATETIME = DATEADD(DAY, 1, CAST(CAST(ISNULL(@Fecha_Final, DATEADD(HOUR, -4, GETDATE())) AS DATE) AS DATETIME));

    SELECT Mv.Fecha,
           Mv.Motivo,
           Mv.Documento,
           A.ID_Accesorio,
           A.Descripcion_Accesorio,
           Modelo = Mo.Descripcion,
           T.Tipo_Pieza,
           A.ID_Almacen,
           AC.Descripcion_Almacen,
           Mv.Entrada,
           Mv.Salida,
           Costo = CAST(A.Costo_Equipo AS DECIMAL(18,2)),
           Mv.Usuario,
           Mv.Tecnico
      FROM dbo.fn_Accesorio_Movimientos() Mv
     INNER JOIN dbo.Accesorios A ON A.ID_Accesorio = Mv.ID_Accesorio
     INNER JOIN dbo.Modelos Mo ON Mo.ID_Modelo = A.ID_Modelo
     INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = A.ID_Modelo        -- solo componentes
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = A.ID_Almacen
     WHERE Mv.Fecha >= @Ini AND Mv.Fecha < @FinExcl
       AND (@ID_Almacen IS NULL OR A.ID_Almacen = @ID_Almacen)
       AND (@Tecnico IS NULL OR Mv.Tecnico = @Tecnico)
       AND (@Tipo_Pieza IS NULL OR T.Tipo_Pieza = @Tipo_Pieza)
       AND (@Motivo IS NULL OR Mv.Motivo = @Motivo)
     ORDER BY Mv.Fecha DESC, Mv.Motivo, Mv.Documento;
END
GO


-- ############################################################
-- 8. PROCEDIMIENTOS QUE MUESTRAN LA CONFIGURACION DE LA LAPTOP (combo de modelos, disponibles, vendidos, facturacion)
-- ############################################################

CREATE OR ALTER PROCEDURE [dbo].[Modelos_Consulta_Combo]
    @criterio INT = NULL
AS
SELECT ID_Modelo, Descripcion, ID_Grupo
  FROM Modelos
 WHERE ID_Marca = @criterio
    OR @criterio IS NULL
ORDER BY Descripcion ASC
GO

CREATE OR ALTER PROCEDURE [dbo].[Proc_Equipo_Disponible_Consulta]
    @PageIndex INT,
    @PageSize INT,
    @Almacen INT = 0,
    @Suplidor VARCHAR(60) = '',
    @Imei VARCHAR(60) = '',
    @ID_Modelo INT = 0,
    @Grupo INT = NULL,
    @Fecha_Inicio DATETIME = '',
    @Fecha_Final DATETIME = ''
AS
BEGIN
    DECLARE @FechaI DATETIME, @FechaF DATETIME

    SET @FechaI = @Fecha_Inicio
    SET @FechaF = @Fecha_Final

    IF @Almacen = 4
        SET @Almacen = 0;
    ELSE
        SET @Almacen = (SELECT Id_almacen FROM Sucursales_cata WHERE ID_Sucursal = @Almacen);

    DECLARE @FIni INT, @FFin INT

    SET @FIni = YEAR(@FechaI) * 10000 + MONTH(@FechaI) * 100 + DAY(@FechaI)
    SET @FFin = YEAR(@FechaF) * 10000 + MONTH(@FechaF) * 100 + DAY(@FechaF)
    DECLARE @PageIni INT, @PageEnd INT, @Linea INT

    SET @PageIni = 0;
    SET @PageEnd = 0;

    SET @PageIndex = CASE WHEN @PageIndex = 0 THEN 1 ELSE @PageIndex END;
    SET @PageEnd = (@PageIndex * @PageSize);
    SET @PageIni = (@PageEnd - @PageSize);

    -- Suma total inventario
    DECLARE @TotalInventario MONEY;
    SELECT @TotalInventario = SUM(Costo_Equipo)
      FROM Equipos E
      INNER JOIN Modelos AS M ON E.ID_Modelo = M.ID_Modelo
      LEFT JOIN dbo.Suplidores AS su ON su.id_suplidor = E.ID_Suplidor
      LEFT JOIN contactos AS CS ON CS.id_contacto = su.id_contacto
      WHERE (CS.Nombres = '' OR CS.Nombres + ' ' + CS.Apellidos LIKE '%' + @Suplidor + '%')
        AND ((@Almacen = 0) OR (E.ID_Almacen = @Almacen))
        AND ((@ID_Modelo = 0) OR (E.ID_Modelo = @ID_Modelo))
        AND (@Grupo IS NULL OR M.ID_Grupo = @Grupo)
        AND ((@Fecha_Inicio = '' AND @Fecha_Final = '') OR (YEAR(E.Fecha_registro) * 10000 + MONTH(E.Fecha_registro) * 100 + DAY(E.Fecha_registro)) BETWEEN @FIni AND @FFin)
        AND E.Disponible = 'S';

    SELECT  E.ID_Equipo numero,
            E.Imei,
            M.Descripcion,
            T.Tecnologia_Descripcion AS Tecnologia,
            G.Descripcion_Garantia AS Garantia,
            Es.Descripcion_Estado_Bloqueo AS [Estado Bloqueo],
            E.Precio_Por_Mayor AS [Por Mayor],
            E.Precio_Detalle AS [Al Detalle],
            E.Costo_Equipo AS [Costo],
            E.Comision_Detalle AS [Comision Detalle],
            E.Comision_Por_Mayor AS [Comision Por Mayor],
            E.Desbloqueado,
            E.Disponible,
            E.Nota_Adicional AS Nota,
            AC.Descripcion_Almacen,
            C.Nombres + ' ' + C.Apellidos AS Suplidor,
            S.ID_Suplidor,
            M.ID_Modelo,
            E.ID_Almacen,
            E.Fecha_registro,
            TotalInventario = @TotalInventario,
            M.ID_Grupo,
            ISNULL(GC.Descripcion, '') AS Grupo,
            dbo.fn_Equipo_Configuracion_Texto(E.Imei) AS Configuracion
    INTO #Temporal
    FROM Equipos AS E WITH(NOLOCK)
        JOIN Modelos AS M ON E.ID_Modelo = M.ID_Modelo
        JOIN dbo.Tecnologias_Cata AS T ON E.ID_Tecnologia = T.ID_Tecnologia
        JOIN dbo.Garantias_Cata AS G ON E.ID_Garantia = G.ID_Garantia
        JOIN dbo.Estados_Bloqueos_Cata AS Es ON E.ID_Estado_Bloqueo = Es.ID_Estado_Bloqueo
        LEFT JOIN dbo.Suplidores AS S ON E.ID_Suplidor = S.ID_Suplidor
        JOIN dbo.Almacen_Cata AS AC ON E.ID_Almacen = AC.ID_Almacen
        JOIN Contactos C ON S.ID_Contacto = C.ID_Contacto
        LEFT JOIN dbo.Grupos_Cata AS GC ON M.ID_Grupo = GC.ID_Grupo
    WHERE
        (C.Nombres = '' OR C.Nombres + ' ' + C.Apellidos LIKE '%' + @Suplidor + '%')
        AND ((@Almacen = 0) OR (E.ID_Almacen = @Almacen))
        AND ((@Imei = '') OR (E.Imei = @Imei))
        AND ((@ID_Modelo = 0) OR (E.ID_Modelo = @ID_Modelo))
        AND (@Grupo IS NULL OR M.ID_Grupo = @Grupo)
        AND ((@Fecha_Inicio = '' AND @Fecha_Final = '') OR (YEAR(E.Fecha_registro) * 10000 + MONTH(E.Fecha_registro) * 100 + DAY(E.Fecha_registro)) BETWEEN @FIni AND @FFin)
        AND E.Disponible = 'S'
    ORDER BY M.Descripcion

    -- Registros
    DECLARE @Registros INT
    SET @Registros = (SELECT COUNT(numero) FROM #Temporal);

    IF @PageEnd > @Registros
        SET @PageEnd = @Registros

    SELECT *,
           Linea = @PageIni + 1,
           Ultima_Linea = @PageEnd,
           Cantidad_Registros = @Registros
    FROM #Temporal
    ORDER BY numero
    OFFSET @PageSize * (@PageIndex - 1) ROWS
    FETCH NEXT @PageSize ROWS ONLY OPTION (RECOMPILE);

    DROP TABLE #Temporal
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Equipo_Busca_Disponible] --'4444444444',25

@Equipo varchar (50),
@Id_Almacen int
as

Begin
    begin
      if exists ( select E.Imei
      from Equipos as E
      join Modelos as M
      on e.ID_Modelo= M.ID_Modelo
	  join Sucursales_cata S
	  on s.ID_Almacen = E.ID_Almacen
      where E.Imei =@Equipo
      and E.Disponible = 'S'
      and S.ID_Sucursal = @Id_Almacen)
      --and E.id_almacen = @Id_Almacen)

      select Equipo = E.Imei,Modelo = M.Descripcion
	  ,Marca = ma.Descripcion
	  ,precioCosto = E.Costo_Equipo
	  ,precioPorMayor = E.Precio_Por_Mayor
	  ,PrecioDetalle = E.Precio_Detalle
	  ,E.Disponible,E.Nota_Adicional
	  , Mensaje = 'Equipo Encontrado'
	  , Configuracion = dbo.fn_Equipo_Configuracion_Texto(E.Imei)
      from Equipos as E
      inner join Modelos as M on e.ID_Modelo= M.ID_Modelo
	  inner join Marcas ma on m.ID_Marca = ma.ID_Marca
	  inner join Sucursales_cata S on s.ID_Almacen = E.ID_Almacen
      where E.Imei =@Equipo
      and E.Disponible = 'S'
      and S.ID_Sucursal = @Id_Almacen
	  --and E.ID_Almacen = @Id_Almacen
    end;

    Begin
      if exists (select E.Imei
      from Equipos as E
      inner join Modelos as M on e.ID_Modelo= M.ID_Modelo
	  inner join Marcas ma on m.ID_Marca = ma.ID_Marca
	  inner join Sucursales_cata S on s.ID_Almacen = E.ID_Almacen
      where E.Imei =@Equipo
      and E.Disponible = 'N'
	  and S.ID_Sucursal = @Id_Almacen)
      --and E.ID_Almacen = @Id_Almacen)

       select Equipo = E.Imei,Modelo = M.Descripcion,Marca = ma.Descripcion,precioCosto = E.Costo_Equipo,
	  precioPorMayor = E.Precio_Por_Mayor,PrecioDetalle = E.Precio_Detalle,
	  E.Disponible,E.Nota_Adicional, Mensaje = 'Equipo No Disponible por '+E.Nota_Adicional
	  , Configuracion = dbo.fn_Equipo_Configuracion_Texto(E.Imei)
      from Equipos as E
      inner join Modelos as M on e.ID_Modelo= M.ID_Modelo
	  inner join Marcas ma on m.ID_Marca = ma.ID_Marca
	  inner join Sucursales_cata S on s.ID_Almacen = E.ID_Almacen
      where E.Imei =@Equipo
      and E.Disponible = 'N'
	  and S.ID_Sucursal = @Id_Almacen
      --and E.ID_Almacen = @Id_Almacen
    End;

    Begin
      if exists (select E.Imei
      from Equipos as E
      join Modelos as M on e.ID_Modelo= M.ID_Modelo
	  join Sucursales_cata S on s.ID_Almacen = E.ID_Almacen
      where E.Imei =@Equipo
      and E.Disponible = 'S'
      and S.ID_Sucursal <> @Id_Almacen)

      select Equipo = E.Imei
	  ,Modelo = M.Descripcion ,precioCosto = E.Costo_Equipo
	  ,Marca = ma.Descripcion
	  ,precioPorMayor = E.Precio_Por_Mayor
	  ,PrecioDetalle = E.Precio_Detalle,
	  Disponible = 'N'
	  ,E.Nota_Adicional, Mensaje = 'Equipo No Pertenece a esta Sucursal'
	  , Configuracion = dbo.fn_Equipo_Configuracion_Texto(E.Imei)
      from Equipos as E
       inner join Modelos as M on e.ID_Modelo= M.ID_Modelo
	   inner join Marcas ma on m.ID_Marca = ma.ID_Marca
	   inner join Sucursales_cata S on s.ID_Almacen = E.ID_Almacen
	   where E.Imei =@Equipo
      and E.Disponible = 'S'
      and S.ID_Sucursal <> @Id_Almacen
    end;

    Begin
      if not exists (select E.Imei
      from Equipos as E
      where E.Imei =@Equipo)

      select Equipo = '',Modelo='',Marca ='',
	  precioPorMayor=0,PrecioDetalle=0,Disponible='',
	  Nota_Adicional='', Mensaje = 'Equipo No está Registrado'
	  , Configuracion = CAST(NULL AS VARCHAR(100))

    end;
End;
GO

CREATE OR ALTER PROCEDURE [dbo].[Proc_Equipo_Consulta_Vendidos]
@PageIndex int ,
@PageSize Int ,
@Almacen int = 0,
@Imei varchar(30) ='',
@Telefono bigint= 0,
@Suplidor varchar(150)='',
@Vendedor varchar(150)='',
@ID_Modelo int = 0,  -- CAMBIADO: De @Modelo varchar(60) a @ID_Modelo int
@Fecha_factura_Inicial datetime = '',
@Fecha_factura_Final datetime = '',
@Fecha_Inicio datetime ='',
@Fecha_Final datetime =''
AS

BEGIN

IF @Almacen = 4
BEGIN
    SET @Almacen = 0;
END
ELSE
BEGIN
    SET @Almacen = (SELECT Id_almacen FROM Sucursales_cata WHERE ID_Sucursal = @Almacen);
END

--Set DateFormat MDY;
--Rangos por fecha facturacion--

DECLARE @FechaRi datetime,
@FechaRf datetime

SET @FechaRi = @Fecha_factura_Inicial
SET @FechaRf = @Fecha_factura_Final

DECLARE @FrIni int,
        @FrFin int

SET @FrIni = Year(@FechaRi)*10000+
             Month(@FechaRi)*100+
             Day(@FechaRi)
SET @FrFin = Year(@FechaRf)*10000+
             Month(@FechaRf)*100+
             Day(@FechaRf)
------------------------------
--Rangos por fecha registro---

DECLARE @FechaI Datetime,
@FechaF Datetime

SET @FechaI =@Fecha_Inicio
SET @FechaF =@Fecha_Final

DECLARE @FIni int,
        @FFin Int

  SET @FIni = Year(@FechaI)*10000+
              Month(@FechaI)*100+
              Day(@FechaI)
  SET @FFin = Year(@FechaF)*10000+
              Month(@FechaF)*100+
              Day(@FechaF)
-------------------------
--------Pagination-------
    DECLARE @PageIni int;
    DECLARE @PageEnd int;
    DECLARE @Linea int;

    SET @PageIni = 0;
    SET @PageEnd = 0;

    SET @PageIndex = CASE WHEN @PageIndex = 0 THEN 1 ELSE @PageIndex END;  -- Si es 0 Cambia a 1
    SET @Pageend = (@PageIndex*@PageSize); -- Multiplicamos el indice por el size y obtenemos la pagina final
    SET @PageIni = (@Pageend-@PageSize); -- Este Proceso Captura la pag inicial
---------------------------

-- MEJORADO: CTE más eficiente usando ROW_NUMBER() en lugar de MAX()
WITH EquipoFacturas AS (
    SELECT E.Imei,
           dt.numero_factura,
           ROW_NUMBER() OVER (PARTITION BY E.Imei ORDER BY ft.fecha_registro DESC) as rn
    FROM Equipos AS E
    INNER JOIN detalle_facturas AS dt ON dt.ID_Equipo = e.imei AND dt.estado = 'A'
    INNER JOIN facturas AS ft ON ft.numero_factura = dt.numero_factura AND ft.estado = 'A'
    WHERE ((@Fecha_Inicio ='' AND @Fecha_Final ='') OR
           (Year(E.Fecha_registro) * 10000+Month(E.Fecha_registro)* 100+Day(E.Fecha_registro)) BETWEEN @FIni AND @FFin)
)
SELECT Imei, numero_factura
INTO #CTE
FROM EquipoFacturas
WHERE rn = 1;


   --Suma total inventario
    DECLARE @TotalInventario money;
    DECLARE @TotalFacturado  money;
    DECLARE @TotalGanancia   money;
    DECLARE @TotalComisionDetalle  money;
    DECLARE @TotalComisionXMayor   money;
    DECLARE @TotalItbis   money;

     SELECT @TotalInventario =  SUM(df.Costo_Equipo ),
            @TotalFacturado =SUM(df.total),
            @TotalGanancia =@TotalFacturado- @TotalInventario ,
            @TotalComisionDetalle = SUM(e.Comision_Detalle),
            @TotalComisionXMayor = SUM(e.Comision_Por_Mayor),
            @TotalItbis = SUM(df.Itbis)
       FROM equipos E
      INNER JOIN Modelos AS M
         ON e.ID_Modelo= M.ID_Modelo
       LEFT JOIN dbo.Suplidores AS su
         ON su.id_suplidor = e.ID_Suplidor
       LEFT JOIN contactos AS CS
         ON CS.id_contacto = su.id_contacto
      INNER JOIN Detalle_Facturas df
         ON e.Imei = df.ID_Equipo and df.estado = 'A'
       LEFT JOIN facturas AS ft
         ON ft.numero_factura = df.numero_factura and df.estado = 'A'
      INNER JOIN Vendedores v
         ON v.ID_Vendedor = ft.ID_Vendedor
       LEFT JOIN clientes AS cl
         ON cl.id_cliente = ft.id_cliente
      INNER JOIN Contactos c
         ON c.ID_Contacto = v.ID_Contacto
       LEFT JOIN contactos AS co
         ON co.id_contacto = cl.id_contacto
            JOIN Telefonos AS te
         ON co.ID_Contacto = te.ID_Contacto
      WHERE ( CS.Nombres ='' OR CS.Nombres +' '+ CS.Apellidos like '%'+ @Suplidor  + '%' )
        AND ( c.Nombres ='' OR c.Nombres +' '+ c.Apellidos like '%'+ @Vendedor  + '%' )
        AND ((@Almacen=0) OR  (E.ID_Almacen =@Almacen ))
        AND ((@imei ='') OR (df.ID_Equipo = @Imei	))
        AND ((@Telefono =0) OR (te.Numero_Telefono = @Telefono	))
        AND ((@ID_Modelo = 0) OR (M.ID_Modelo = @ID_Modelo))  -- CAMBIADO: Filtro por ID en lugar de LIKE con descripción
        AND (
     (@Imei <> '')
     OR (
          ((@Fecha_factura_Inicial ='' AND @Fecha_factura_Final ='')
           OR (Year(df.Fecha_registro) * 10000 + Month(df.Fecha_registro) * 100 + Day(df.Fecha_registro)) BETWEEN @FrIni AND @FrFin
          )
         AND
          ((@Fecha_Inicio ='' AND @Fecha_Final ='')
           OR (Year(E.Fecha_registro) * 10000 + Month(E.Fecha_registro) * 100 + Day(E.Fecha_registro)) BETWEEN @FIni AND @FFin
          )
     )
)
        AND	((@Fecha_Inicio ='' AND @Fecha_Final ='') or (Year(E.Fecha_registro) * 10000+Month(E.Fecha_registro)* 100+Day(E.Fecha_registro) ) BETWEEN @FIni AND @FFin)
         AND e.disponible = 'N' -- equipo este vendido
         AND df.Estado = 'A' -- que la factura no este anulada
         --AND ft.id_tipo_pagos = 1 -- tipo de pago sea de contado
         AND df.ID_Tipo = 1 -- que sea equipo
         AND ft.ID_Tipo_Factura <>3 -- diferente de consignacion
        AND ft.Literal+Convert(Varchar(10),Ft.Numero_Factura) =df.literalF

    SELECT
            E.ID_Equipo numero,
            e.Imei,
            M.Descripcion,
            E.Precio_Por_Mayor AS [Por Mayor],
            E.Precio_Detalle AS [Al Detalle],
            E.Costo_Equipo AS [Costo],
            df.total AS [Precio Vendido],
            tpc.descripcion_tipo_pagos AS [Tipo Factura],
            E.Desbloqueado AS Desb,
            df.Itbis,
            e.Nota_Adicional AS Nota,
            al.Descripcion_Almacen,
            cs.Nombres +' '+cs.Apellidos AS Suplidor,
            c.Nombres +' '+c.Apellidos AS Vendedor,
            co.Nombres +' '+co.Apellidos AS cliente,
            df.fecha_registro AS [Fecha facturado],
            ft.Numero_Factura AS Factura,
            E.Fecha_registro ,
            e.Comision_Detalle AS [Comision Detalle],
            e.Comision_Por_Mayor AS [Comision Por Mayor],
            TotalInventario = @TotalInventario,
            TotalFacturado  = @TotalFacturado,
            TotalGanancia   = @TotalGanancia,
            TotalComisionDetalle = @TotalComisionDetalle,
            TotalComisionXMayor =@TotalComisionXMayor,
            TotalItbis= @TotalItbis,
            dbo.fn_Equipo_Configuracion_Texto(e.Imei) AS Configuracion
            INTO #Temporal
      FROM #CTE cte WITH(NOLOCK)
     INNER JOIN equipos e
        ON e.imei = cte.imei
     INNER JOIN Modelos AS M
        ON e.ID_Modelo= M.ID_Modelo
     INNER JOIN dbo.Tecnologias_Cata AS T
        ON E.ID_Tecnologia = T.ID_Tecnologia
     INNER JOIN Almacen_Cata AS al
        ON E.ID_Almacen = al.ID_Almacen
      LEFT JOIN facturas AS ft
        ON cte.numero_factura = ft.numero_factura and ft.estado = 'A'
      LEFT JOIN clientes AS cl
        ON cl.id_cliente = ft.id_cliente
      LEFT JOIN contactos AS co
        ON co.id_contacto = cl.id_contacto
      JOIN Telefonos AS te
        ON co.ID_Contacto = te.ID_Contacto
      LEFT JOIN dbo.Suplidores AS su
        ON su.id_suplidor = e.ID_Suplidor
      LEFT JOIN contactos AS CS
        ON CS.id_contacto = su.id_contacto
     INNER JOIN Detalle_Facturas df
        ON e.Imei = df.ID_Equipo
     INNER JOIN Vendedores v
        ON v.ID_Vendedor = ft.ID_Vendedor
     INNER JOIN Contactos c
        ON c.ID_Contacto = v.ID_Contacto
     INNER JOIN Tipos_Facturas_Cata tfc
        ON tfc.ID_Tipo_Factura = ft.ID_Tipo_Factura
     INNER JOIN tipos_pagos_cata tpc
        ON tpc.id_tipo_pagos = ft.id_tipo_pagos

     WHERE ( CS.Nombres ='' OR CS.Nombres +' '+ CS.Apellidos like '%'+ @Suplidor  + '%' )
        AND ( c.Nombres ='' OR c.Nombres +' '+ c.Apellidos like '%'+ @Vendedor  + '%' )
        AND ((@Almacen=0) OR  (E.ID_Almacen =@Almacen ))
        AND ((@imei ='') OR (df.ID_Equipo = @Imei	))
        AND ((@Telefono =0) OR (te.Numero_Telefono = @Telefono	))
        AND ((@ID_Modelo = 0) OR (M.ID_Modelo = @ID_Modelo))  -- CAMBIADO: Filtro por ID en lugar de LIKE con descripción
        AND (
     (@Imei <> '')
     OR (
          ((@Fecha_factura_Inicial ='' AND @Fecha_factura_Final ='')
           OR (Year(df.Fecha_registro) * 10000 + Month(df.Fecha_registro) * 100 + Day(df.Fecha_registro)) BETWEEN @FrIni AND @FrFin
          )
         AND
          ((@Fecha_Inicio ='' AND @Fecha_Final ='')
           OR (Year(E.Fecha_registro) * 10000 + Month(E.Fecha_registro) * 100 + Day(E.Fecha_registro)) BETWEEN @FIni AND @FFin
          )
     )
)
        AND	((@Fecha_Inicio ='' AND @Fecha_Final ='') or (Year(E.Fecha_registro) * 10000+Month(E.Fecha_registro)* 100+Day(E.Fecha_registro) ) BETWEEN @FIni AND @FFin)
         AND e.disponible = 'N' -- equipo este vendido
         AND df.Estado = 'A' -- que la factura no este anulada
         --AND ft.id_tipo_pagos = 1 -- tipo de pago sea de contado
         AND df.ID_Tipo = 1 -- que sea equipo
         AND ft.ID_Tipo_Factura <>3 -- diferente de consignacion
        AND ft.Literal+Convert(Varchar(10),Ft.Numero_Factura) =df.literalF
   ORDER BY ft.fecha_registro ASC

         -- Registros
  DECLARE @Registros Int
  SET @Registros = (SELECT COUNT(numero) From #Temporal);
  --SELECT  @Registros = Max(Linea) FROM #Temporal
    IF @PageEnd > @Registros
    BEGIN
        SET @PageEnd = @Registros
    END
   -- Devolver Resultado
      SELECT  *,
              Linea = @PageIni + 1,
              Ultima_Linea = @PageEnd,
              Cantidad_Registros = @Registros
         FROM #Temporal
     ORDER BY Linea
     OFFSET @PageSize * (@PageIndex - 1) ROWS
    FETCH NEXT @PageSize ROWS ONLY  ;

      DROP TABLE #CTE
      DROP TABLE #Temporal
END
GO


-- ############################################################
-- 9. CORRECCION: Equipo_Actualiza_Estado_y_precio solo actualiza "Desbloqueado" del equipo indicado
-- ############################################################

CREATE OR ALTER PROCEDURE [dbo].[Equipo_Actualiza_Estado_y_precio]
@Imei varchar(20),
@Id_Condicion int,
@Costo_Equipo money,
@Precio_Por_Mayor money,
@Precio_Detalle money,
@Comision_Detalle money,
@Comision_Por_Mayor money,
@Id_estado_Bloqueo int,
@Usuario varchar (50)
as

--------------------------------------------------------------------------------------
-- inserta en el historico--------

INSERT INTO [dbo].[Equipos_Historico]
           ([Imei]
           ,[ID_Modelo]
           ,[ID_Tecnologia]
           ,[ID_Garantia]
           ,[ID_Condicion]
           ,[ID_Estado_Bloqueo]
           ,[ID_Almacen]
           ,[ID_Suplidor]
           ,[Numero_de_Factura]
           ,[Precio_Por_Mayor]
           ,[Precio_Detalle]
           ,[Costo_Equipo]
           ,[Desbloqueado]
           ,[Disponible]
           ,[Nota_Adicional]
		   , Nota_Historico
           ,[Usuario]
           ,[Fecha_registro]
           ,[Numero_Invetario]
           ,[Numero_Registro]
		   ,Usuario_Historico
		   ,Fecha_registro_Historico
		   ,[Comision_Detalle]
		   ,[Comision_Por_Mayor])

		   SELECT [Imei]
      ,[ID_Modelo]
      ,[ID_Tecnologia]
      ,[ID_Garantia]
      ,[ID_Condicion]
      ,[ID_Estado_Bloqueo]
      ,[ID_Almacen]
      ,[ID_Suplidor]
      ,[Numero_de_Factura]
      ,[Precio_Por_Mayor]
      ,[Precio_Detalle]
      ,[Costo_Equipo]
      ,[Desbloqueado]
      ,[Disponible]
      ,[Nota_Adicional]
	  ,'Actualización de los precios y/o la condición del equipo'
      ,[Usuario]
      ,[Fecha_registro]
      ,[Numero_Invetario]
      ,[Numero_Registro]
	   ,@Usuario
       ,dateadd (hour,-4,getdate())
	   ,[Comision_Detalle]
	   ,[Comision_Por_Mayor]
   from Equipos With(NoLock)
 where Imei = @Imei

update dbo.Equipos
set ID_Condicion = @Id_Condicion,
Costo_Equipo = @Costo_Equipo,
Precio_Por_Mayor = @Precio_Por_Mayor,
Precio_Detalle = @Precio_Detalle,
Comision_Detalle =@Comision_Detalle,
Comision_Por_Mayor = @Comision_Por_Mayor,

ID_Estado_Bloqueo =@Id_estado_Bloqueo,
Desbloqueado = CASE WHEN @Id_estado_Bloqueo = 1 THEN 'S' ELSE 'N' END,
Nota_Adicional = 'Actualización de los precios y/o el estado del equipo',
Usuario = @Usuario
--Fecha_registro =dateadd (hour,-4,getdate())
where Imei = @Imei
GO

-- ============================================================
-- DIAGNOSTICO (solo lectura, ejecutar por separado): equipos cuyo Desbloqueado no
-- coincide con su estado de bloqueo. Sirve para medir el daño que pudo dejar el error.
-- NO se corrige automaticamente: si ID_Estado_Bloqueo = 1 no significa lo mismo en todos
-- los flujos (ej. Equipo_Insert), revisa el resultado antes de decidir una correccion masiva.
-- ============================================================
/*
SELECT Coincide = CASE WHEN (ID_Estado_Bloqueo = 1 AND Desbloqueado = 'S')
                         OR (ID_Estado_Bloqueo <> 1 AND Desbloqueado = 'N') THEN 'SI' ELSE 'NO' END,
       Disponible, Cantidad = COUNT(*)
  FROM dbo.Equipos
 GROUP BY CASE WHEN (ID_Estado_Bloqueo = 1 AND Desbloqueado = 'S')
                 OR (ID_Estado_Bloqueo <> 1 AND Desbloqueado = 'N') THEN 'SI' ELSE 'NO' END,
          Disponible
 ORDER BY 1, 2;

-- Detalle de los que no coinciden:
SELECT Imei, ID_Estado_Bloqueo, Desbloqueado, Disponible, Nota_Adicional
  FROM dbo.Equipos
 WHERE (ID_Estado_Bloqueo = 1 AND Desbloqueado <> 'S')
    OR (ID_Estado_Bloqueo <> 1 AND Desbloqueado <> 'N');
*/


-- ############################################################
-- 10. PREFACTURAS: anular, consultar y procesar (el estado de anuladas es X; el detalle tambien cambia de estado)
-- ############################################################

-- ------------------------------------------------------------
-- PreFacturas_Anular: anular una prefactura (estado 'X', el estandar de anuladas en el sistema)
-- ------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[PreFacturas_Anular]
@Numero_PreFactura INT,
@Usuario_Anulacion VARCHAR(50)
AS
BEGIN
    -- Cambia el estado a 'X' (Anulada), registra usuario y fecha de anulacion
    UPDATE PreFacturas
    SET Estado = 'X',
        Fecha_Procesamiento = GETDATE(),
        Usuario_Procesamiento = @Usuario_Anulacion
    WHERE Numero_PreFactura = @Numero_PreFactura

    -- Tambien se actualizan los detalles
    UPDATE PreDetalle_Facturas
    SET Estado = 'X',
        Usuario = @Usuario_Anulacion,
        Fecha_registro = GETDATE()
    WHERE Numero_PreFactura = @Numero_PreFactura
END
GO

-- ------------------------------------------------------------
-- PreFacturas_Consultar
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[PreFacturas_Consultar]
  @Estado CHAR(1) = NULL,         -- Opcional: filtrar por estado ('P', 'C', 'X') P= pendientes; C= cerradas; X= anuladas
  @ID_Sucursal INT = NULL,        -- Opcional: filtrar por sucursal
  @ID_Cliente INT = NULL,         -- Opcional: filtrar por cliente
  @Criterio VARCHAR(100) = NULL   -- Opcional: buscar por nombre o telefono
AS
BEGIN
    SELECT
        pf.Numero_PreFactura,
        pf.ID_Cliente,
        c.Nombres,
        c.Apellidos,
        t.Numero_Telefono,
        pf.ID_Tipo_Factura,
        tf.Descripcion_Factura_Tipo AS Tipo_Factura,
        pf.ID_Vendedor,
        ISNULL(cv.Nombres, '') AS Vendedor_Nombres,
        pf.ID_Tipo_Pagos,
        tp.Descripcion_Tipo_Pagos AS Tipo_Pago,
        pf.ID_Sucursal,
        sc.Sucursal_Descripcion AS Sucursal,
        pf.SubTotal,
        pf.Descuento,
        pf.Itbis,
        pf.Total,
        pf.Cantidad_Articulos,
        pf.Nota,
        pf.Estado,
        pf.Usuario,
        pf.Fecha_Registro,
        pf.Fecha_Procesamiento,
        pf.Usuario_Procesamiento,
        pf.NFCTipoNumero,
        pf.NFCFISCAL,
        pf.NCF_NUMERO,
        pf.RNC,
        pf.Cotizacion_Numero
    FROM PreFacturas pf
    INNER JOIN Clientes cl ON pf.ID_Cliente = cl.ID_Cliente
    INNER JOIN Contactos c ON cl.ID_Contacto = c.ID_Contacto
    LEFT JOIN Telefonos t ON t.ID_Contacto = c.ID_Contacto
    INNER JOIN Tipos_Facturas_Cata tf ON pf.ID_Tipo_Factura = tf.ID_Tipo_Factura
    LEFT JOIN Vendedores v ON v.ID_Vendedor = pf.ID_Vendedor
    LEFT JOIN Contactos cv ON v.ID_Contacto = cv.ID_Contacto
    LEFT JOIN Tipos_Pagos_Cata tp ON pf.ID_Tipo_Pagos = tp.ID_Tipo_Pagos
    LEFT JOIN Sucursales_cata sc ON pf.ID_Sucursal = sc.id_sucursal
    WHERE (@Estado IS NULL OR pf.Estado = @Estado)
      AND (@ID_Sucursal IS NULL OR pf.ID_Sucursal = @ID_Sucursal)
      AND (@ID_Cliente IS NULL OR pf.ID_Cliente = @ID_Cliente)
      AND (
            @Criterio IS NULL
            OR c.Nombres LIKE '%' + @Criterio + '%'
            OR c.Apellidos LIKE '%' + @Criterio + '%'
            OR c.Nombres + ' '+ c.Apellidos LIKE '%' + @Criterio + '%'
            OR t.Numero_Telefono LIKE '%' + @Criterio + '%'
          )
    ORDER BY pf.Fecha_Registro DESC
END
GO

-- ------------------------------------------------------------
-- Procesar_PreFactura: ademas de la prefactura, tambien cambia el estado de su detalle ('C')
-- ------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
            Literal, Cotizacion_Numero
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
            Cotizacion_Numero
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


-- ############################################################
-- 11. FECHAS: valores por defecto de las columnas de fecha del modulo en hora local (solo si aun usan la hora del servidor)
-- ############################################################

DECLARE @Columnas TABLE (Orden INT IDENTITY(1, 1) PRIMARY KEY, Tabla SYSNAME, Columna SYSNAME);
INSERT INTO @Columnas (Tabla, Columna) VALUES
    ('Equipo_Configuracion',            'Fecha_Registro'),
    ('Equipo_Configuracion_Modulo',     'Fecha_Registro'),
    ('Equipo_Configuracion_Correccion', 'Fecha'),
    ('Equipo_Cambio_Pieza',             'Fecha_Registro');

DECLARE @Orden INT = 1, @Max INT = (SELECT MAX(Orden) FROM @Columnas);
DECLARE @Tabla SYSNAME, @Columna SYSNAME, @Restriccion SYSNAME, @Definicion NVARCHAR(400), @Sql NVARCHAR(MAX);

WHILE @Orden <= @Max
BEGIN
    SELECT @Tabla = Tabla, @Columna = Columna FROM @Columnas WHERE Orden = @Orden;
    SET @Restriccion = NULL;
    SET @Definicion = NULL;

    SELECT @Restriccion = dc.name, @Definicion = dc.definition
      FROM sys.default_constraints dc
     INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
     WHERE dc.parent_object_id = OBJECT_ID('dbo.' + @Tabla) AND c.name = @Columna;

    -- Solo si el default sigue usando la hora del servidor
    IF @Restriccion IS NOT NULL AND @Definicion LIKE '%getdate%' AND @Definicion NOT LIKE '%-4%'
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' DROP CONSTRAINT ' + QUOTENAME(@Restriccion) + N';'
                 + N' ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' ADD CONSTRAINT ' + QUOTENAME('DF_' + @Tabla + '_' + @Columna + '_Local')
                 + N' DEFAULT (DATEADD(HOUR, -4, GETDATE())) FOR ' + QUOTENAME(@Columna) + N';';
        EXEC (@Sql);
    END

    SET @Orden += 1;
END
GO

-- ============================================================
-- FIN DEL SCRIPT
-- ============================================================
