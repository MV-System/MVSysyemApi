-- ============================================================
-- Feature: Grupo/Familia de Equipos
-- Propósito: Clasificar Modelos en Grupos (Celulares, Televisores,
--            Aires Acondicionados, Abanicos, etc.) para poder filtrar
--            el inventario de Equipos por Grupo, sin importar Marca/Modelo.
-- Autor: MVSystem
-- Fecha: 2026-08-16
-- ============================================================

-- ============================================================
-- 1. Tabla catálogo de Grupos
-- ============================================================
CREATE TABLE dbo.Grupos_Cata (
    ID_Grupo INT IDENTITY(1,1) PRIMARY KEY,
    Descripcion VARCHAR(100) NOT NULL,
    Estado CHAR(1) NOT NULL DEFAULT 'A',
    Usuario VARCHAR(60) NULL,
    Fecha_Registro DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- 2. SP: Insertar un nuevo Grupo
-- ============================================================
CREATE PROCEDURE dbo.Grupos_Insert
    @Descripcion VARCHAR(100),
    @Usuario VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Grupos_Cata (Descripcion, Estado, Usuario, Fecha_Registro)
    VALUES (@Descripcion, 'A', @Usuario, GETDATE());

    SELECT ID_Grupo, Descripcion, Estado, Usuario,
           'Grupo registrado satisfactoriamente' AS Mensaje
    FROM dbo.Grupos_Cata
    WHERE ID_Grupo = SCOPE_IDENTITY();
END
GO

-- ============================================================
-- 3. SP: Listado combo de Grupos (activos)
-- ============================================================
CREATE PROCEDURE dbo.Grupos_Consulta_Combo
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID_Grupo, Descripcion
    FROM dbo.Grupos_Cata
    WHERE Estado = 'A'
    ORDER BY Descripcion;
END
GO

-- ============================================================
-- 4. Grupo por defecto: 'Equipos'
--    Todos los modelos existentes quedarán asignados a este grupo
--    hasta que el usuario los reclasifique desde el módulo de Modelos.
-- ============================================================
INSERT INTO dbo.Grupos_Cata (Descripcion, Estado, Usuario, Fecha_Registro)
VALUES ('Equipos', 'A', 'sistema', GETDATE());
GO

-- ============================================================
-- 5. Agregar columna ID_Grupo a Modelos (nullable, con FK)
-- ============================================================
ALTER TABLE dbo.Modelos ADD ID_Grupo INT NULL;
GO

ALTER TABLE dbo.Modelos
    ADD CONSTRAINT FK_Modelos_Grupos_Cata
    FOREIGN KEY (ID_Grupo) REFERENCES dbo.Grupos_Cata(ID_Grupo);
GO

-- ============================================================
-- 6. Asignar el grupo 'Equipos' a todos los modelos existentes
-- ============================================================
UPDATE dbo.Modelos
SET ID_Grupo = (SELECT ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Equipos')
WHERE ID_Grupo IS NULL;
GO

-- ============================================================
-- 7. Modelos_Insert: agregar @ID_Grupo (default = 1, el grupo
--    'Equipos', ya que es el primer registro insertado en el paso 4)
-- ============================================================
ALTER procedure [dbo].[Modelos_Insert]


@Descripcion varchar(100),
@ID_Marca int,
@Estado char(1) = 'A',
@Usuario varchar(50),
@ID_Grupo int = 1

as

if not exists(select descripcion from [dbo].[Modelos] where descripcion = @Descripcion )
begin

if @ID_Grupo is null
begin
    set @ID_Grupo = 1
end

insert into Modelos
     (Descripcion
    ,ID_Marca
    ,Estado
    ,Usuario
    ,Fecha_registro
    ,ID_Grupo)
values (@Descripcion
    ,@ID_Marca
    ,@Estado
    ,@Usuario
    ,dateadd (hour,-4,getdate())
    ,@ID_Grupo)

    declare @ID_Modelo int = @@identity   
select ID_Modelo, Descripcion, Mensaje ='Modelo Guardado Satisfactoriamente'
From [dbo].Modelos
Where ID_Modelo = @ID_Modelo
end
else 
select ID_Modelo, Descripcion,Mensaje ='Esta Marca ya existe'
From [dbo].Modelos
 where descripcion = @Descripcion
GO

-- ============================================================
-- 8. Proc_Equipo_Disponible_Consulta: agregar filtro @Grupo,
--    JOIN a Grupos_Cata y columnas ID_Grupo/Grupo en el SELECT.
-- ============================================================
ALTER PROCEDURE [dbo].[Proc_Equipo_Disponible_Consulta]
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
            ISNULL(GC.Descripcion, '') AS Grupo
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
