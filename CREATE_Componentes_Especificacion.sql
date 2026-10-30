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
    VALUES ('Componentes', 'A', 'sistema', GETDATE());
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
