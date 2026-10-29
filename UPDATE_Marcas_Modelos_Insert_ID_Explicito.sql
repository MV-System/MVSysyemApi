-- ============================================================
-- Sincronizacion con el espejo: Marcas_Insert y Modelos_Insert con ID explicito (opcional)
--
-- Problema: cada base genera sus propios IDs (identity). Cuando se desfasan, un modelo queda con un ID
-- en la base de trabajo y otro en la espejo (caso GARMIN APPROACH S12 en TECNOSTYLE: 4025 / 4024), los
-- equipos de ese modelo no se pueden copiar y, peor, pueden asociarse al modelo equivocado sin error.
--
-- Solucion: ambos SP reciben un parametro NUEVO y OPCIONAL (@ID_Marca / @ID_Modelo, por defecto NULL).
--   - Sin el parametro: se comportan EXACTAMENTE como antes (la logica original no cambia).
--   - Con el parametro (lo envia el API al replicar): el registro se crea con ese mismo ID
--     (SET IDENTITY_INSERT). Si el ID ya existe con la misma descripcion, es idempotente; si lo usa otro
--     registro, responde 'Error: ...' (el API lo registra como [SYNC-FALLO]) y no inserta.
--     Al insertar un ID mayor que el contador, el identity se ajusta solo.
--
-- Ejecutar en TODAS las bases (principal y espejo de cada empresa) ANTES de publicar el API nuevo.
-- Script idempotente (CREATE OR ALTER). El resto de la definicion es identica a la vigente.
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================

-- ------------------------------------------------------------
-- 1. Marcas_Insert
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[Marcas_Insert]
    @Descripcion VARCHAR(100),
    --@Estado CHAR(1),
    @Usuario VARCHAR(50),
    @ID_Marca INT = NULL      -- NUEVO: solo para replicar con el mismo ID
AS
BEGIN
    DECLARE @NewId INT;

    -- NUEVO: replicacion con ID explicito
    IF @ID_Marca IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Marcas WHERE ID_Marca = @ID_Marca)
        BEGIN
            SELECT ID_Marca, Descripcion,
                   Mensaje = CASE WHEN Descripcion = @Descripcion
                                  THEN 'Esta Marca ya existe'
                                  ELSE 'Error: el ID ' + CONVERT(VARCHAR(20), @ID_Marca) + ' ya lo usa otra marca (' + Descripcion + ')' END
            FROM dbo.Marcas
            WHERE ID_Marca = @ID_Marca;
            RETURN;
        END

        SET IDENTITY_INSERT dbo.Marcas ON;
        INSERT INTO dbo.Marcas (ID_Marca, Descripcion, Estado, Usuario, Fecha_registro)
        VALUES (@ID_Marca, @Descripcion, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));
        SET IDENTITY_INSERT dbo.Marcas OFF;

        SELECT ID_Marca, Descripcion, Mensaje = 'Registro Insertado Correctamente'
        FROM dbo.Marcas
        WHERE ID_Marca = @ID_Marca;
        RETURN;
    END

    -- Logica original (sin cambios)
    -- Insertar el nuevo registro en la tabla Marcas
    INSERT INTO dbo.Marcas (Descripcion, Estado, Usuario, Fecha_registro)
    VALUES (@Descripcion, 'A', @Usuario, DATEADD(HOUR, -4, GETDATE()));

    -- Obtener el ID del registro insertado
    SET @NewId = SCOPE_IDENTITY();

    -- Retornar el registro insertado completo
    SELECT ID_Marca,Descripcion,Mensaje ='Registro Insertado Correctamente'
    FROM dbo.Marcas
    WHERE ID_Marca = @NewId;
END;
GO

-- ------------------------------------------------------------
-- 2. Modelos_Insert
--    (el local @ID_Modelo de la version original se renombra @ID_Nuevo porque ahora existe el parametro)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[Modelos_Insert]
@Descripcion varchar(100),
@ID_Marca int,
@Estado char(1) = 'A',
@Usuario varchar(50),
@ID_Grupo int = 1,
@ID_Modelo int = NULL       -- NUEVO: solo para replicar con el mismo ID
as

-- NUEVO: replicacion con ID explicito
if @ID_Modelo is not null
begin
    if @ID_Grupo is null
    begin
        set @ID_Grupo = 1
    end

    -- Ese ID ya existe: idempotente si es el mismo modelo; error si lo usa otro
    if exists (select 1 from [dbo].[Modelos] where ID_Modelo = @ID_Modelo)
    begin
        select ID_Modelo, Descripcion,
               Mensaje = case when Descripcion = @Descripcion
                              then 'Este Modelo ya existe'
                              else 'Error: el ID ' + convert(varchar(20), @ID_Modelo) + ' ya lo usa otro modelo (' + Descripcion + ')' end
        from [dbo].Modelos
        where ID_Modelo = @ID_Modelo
        return
    end

    -- El modelo ya existe con OTRO ID: no se duplica (hay que resolverlo a mano)
    if exists (select 1 from [dbo].[Modelos] where descripcion = @Descripcion)
    begin
        select ID_Modelo, Descripcion,
               Mensaje = 'Error: este modelo ya existe con el ID ' + convert(varchar(20), ID_Modelo) + ' y se pidio el ID ' + convert(varchar(20), @ID_Modelo)
        from [dbo].Modelos
        where descripcion = @Descripcion
        return
    end

    set identity_insert [dbo].[Modelos] on
    insert into Modelos
         (ID_Modelo
        ,Descripcion
        ,ID_Marca
        ,Estado
        ,Usuario
        ,Fecha_registro
        ,ID_Grupo)
    values (@ID_Modelo
        ,@Descripcion
        ,@ID_Marca
        ,@Estado
        ,@Usuario
        ,dateadd (hour,-4,getdate())
        ,@ID_Grupo)
    set identity_insert [dbo].[Modelos] off

    select ID_Modelo, Descripcion, Mensaje ='Modelo Guardado Satisfactoriamente'
    From [dbo].Modelos
    Where ID_Modelo = @ID_Modelo
    return
end

-- Logica original (sin cambios)
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

    declare @ID_Nuevo int = @@identity
select ID_Modelo, Descripcion, Mensaje ='Modelo Guardado Satisfactoriamente'
From [dbo].Modelos
Where ID_Modelo = @ID_Nuevo
end
else
select ID_Modelo, Descripcion,Mensaje ='Este Modelo ya existe'
From [dbo].Modelos
 where descripcion = @Descripcion
GO
