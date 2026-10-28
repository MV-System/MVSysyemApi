-- ============================================================
-- Feature: Módulo de Laptops - Fase 2, Tarea 7 (paso 1)
-- Propósito: Equipo_Busca_Disponible devuelve la columna nueva
--            Configuracion = dbo.fn_Equipo_Configuracion_Texto(Imei)
--            (ej. '12GB DDR4 / 256GB SSD SATA'; NULL si no tiene) para
--            que la factura/cotización describa la laptop como
--            'Modelo · Configuracion'.
--            No se quita ni se renombra ninguna columna ni parámetro.
-- Requiere: CREATE_Equipo_Configuracion.sql ejecutado.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
--      '1111111111'
--      '4444444444'
------------Consultar Equipos para facturar-----------------------------------------------------
--                         Equipo_Busca_Disponible '354895096613422',25

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
