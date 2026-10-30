-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 3 (cambio de pieza por modulos)
-- Proposito: SP Accesorio_Componente_Asegurar. Garantiza que exista en el catalogo el accesorio de un
--            componente (modelo + capacidad) en el almacen indicado y devuelve su ID.
--            Se usa cuando se retira una pieza que no esta registrada como accesorio.
--            - Si ya existe (modelo + capacidad leida de la descripcion) en ese almacen: lo devuelve.
--            - Si no, lo CREA en TODOS los almacenes con Cantidad = 0 (igual que Accesorio_Insert, que crea una
--              fila por almacen, con la cantidad solo en el almacen elegido; aqui ninguno recibe cantidad: la
--              existencia la suma la recepcion de la pieza):
--                * Si existe el mismo modelo y capacidad en cualquier almacen: copia descripcion, costo, precios,
--                  minimo/maximo y estado.
--                * Si no existe en ninguno: descripcion '<n>GB' (o '<n>TB'), costo y precios 0, Estado = 'I'
--                  (inactivo, para que nunca aparezca en ventas a precio 0). Los precios se piden al recibir la pieza.
--            - Codigo_Barra = 1000000000 + ID_Accesorio (misma regla de Accesorio_Insert).
--            Replicacion: @Ids_Explicitos (JSON [{"idAlmacen":1,"idAccesorio":123}, ...]) crea las filas con esos
--            mismos ID (el API lo envia a la espejo). @Creados devuelve el mismo JSON de lo creado.
-- Requiere: CREATE_Componentes_Especificacion.sql (fn_Componente_Capacidad_GB).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER).
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Accesorio_Componente_Asegurar
    @ID_Modelo       INT,
    @Capacidad_GB    INT,
    @ID_Almacen      INT,
    @Usuario         VARCHAR(50),
    @Ids_Explicitos  NVARCHAR(MAX) = NULL,
    @ID_Accesorio    BIGINT OUTPUT,
    @Creados         NVARCHAR(MAX) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @ID_Accesorio = NULL;
    SET @Creados = NULL;

    -- Ya existe en el almacen (se prefiere el activo)
    SELECT TOP 1 @ID_Accesorio = A.ID_Accesorio
      FROM dbo.Accesorios A
     WHERE A.ID_Modelo = @ID_Modelo
       AND A.ID_Almacen = @ID_Almacen
       AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB
     ORDER BY CASE WHEN A.Estado = 'A' THEN 0 ELSE 1 END, A.ID_Accesorio;

    -- Referencia: el mismo modelo y capacidad en cualquier almacen (se prefiere activo y con precios)
    DECLARE @Desc VARCHAR(100), @Costo MONEY = 0, @Mayor MONEY = 0, @Detalle MONEY = 0,
            @CanMax INT = 2, @CanMin INT = 1, @EstadoNuevo CHAR(1) = 'I';

    SELECT TOP 1 @Desc = A.Descripcion_Accesorio, @Costo = A.Costo_Equipo, @Mayor = A.Precio_Por_Mayor,
                 @Detalle = A.Precio_Detalle, @CanMax = ISNULL(A.Can_Max, 2), @CanMin = ISNULL(A.Can_Min, 1),
                 @EstadoNuevo = A.Estado
      FROM dbo.Accesorios A
     WHERE A.ID_Modelo = @ID_Modelo
       AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB
     ORDER BY CASE WHEN A.Estado = 'A' THEN 0 ELSE 1 END,
              CASE WHEN A.Precio_Detalle > 0 THEN 0 ELSE 1 END, A.ID_Accesorio;

    IF @Desc IS NULL
        SET @Desc = CASE WHEN @Capacidad_GB >= 1024 AND @Capacidad_GB % 1024 = 0
                         THEN CAST(@Capacidad_GB / 1024 AS VARCHAR(10)) + 'TB'
                         ELSE CAST(@Capacidad_GB AS VARCHAR(10)) + 'GB' END;

    -- Almacenes que aun no tienen el accesorio (misma descripcion)
    DECLARE @Faltan TABLE (Orden INT IDENTITY(1, 1) PRIMARY KEY, ID_Almacen INT NOT NULL, ID_Explicito BIGINT NULL);

    INSERT INTO @Faltan (ID_Almacen)
    SELECT AC.ID_Almacen
      FROM dbo.Almacen_Cata AC
     WHERE NOT EXISTS (SELECT 1 FROM dbo.Accesorios A
                        WHERE A.ID_Modelo = @ID_Modelo AND A.ID_Almacen = AC.ID_Almacen
                          AND dbo.fn_Componente_Capacidad_GB(A.Descripcion_Accesorio) = @Capacidad_GB)
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
                 @Mayor, @Detalle, @Costo, 'S', NULL, @EstadoNuevo, @Usuario);
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
                 @Mayor, @Detalle, @Costo, 'S', NULL, @EstadoNuevo, @Usuario);
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
         ORDER BY CASE WHEN A.Estado = 'A' THEN 0 ELSE 1 END, A.ID_Accesorio;
END
GO
