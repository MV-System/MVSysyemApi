-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 5 (costo de la pieza usada)
-- Proposito: Accesorio_Componente_Asegurar distingue la pieza NUEVA de la pieza USADA.
--   La pieza usada es un accesorio SEPARADO cuya descripcion termina en ' (Usado)' (ej. '8GB 3200 (Usado)'),
--   con su propio costo, precios y existencia.
--   - Nuevos parametros OPCIONALES: @Usado (0 = nueva, 1 = usada) y @Porcentaje_Costo_Usado (default 50).
--   - Con @Usado = 1 solo se buscan / crean accesorios cuya descripcion termina en '(Usado)'.
--       * Si ya existe la variante usada (en cualquier almacen) se copia su costo, precios y estado.
--       * Si no existe: costo = @Porcentaje_Costo_Usado % del costo de la pieza nueva del mismo modelo y capacidad
--         (0 si tampoco hay nueva), precios 0 y Estado 'I' (inactiva); los precios se piden al recibir la pieza.
--   - Con @Usado = 0 (default) el comportamiento es el de la Tarea 3, pero ignora las piezas '(Usado)'.
--   - Los accesorios que crea el SP quedan con Usuario = 'Sistema' (no el usuario logueado): asi se sabe que el
--     costo y los precios iniciales los configuro el sistema y no una persona. El parametro @Usuario se conserva
--     por compatibilidad con los SP que lo llaman, pero ya no se guarda en el accesorio.
--   Ver la regla de crear en todos los almacenes, Codigo_Barra y replicacion con ID explicito en
--   CREATE_Accesorio_Componente_Asegurar.sql (no cambian).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER).
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================
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
