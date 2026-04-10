-- =============================================
-- FIX: Rebajar ITBIS de las ganancias en Ver_Utilidades_Precio_Detalle
-- El campo Total de Detalle_Facturas incluye ITBIS, por lo que
-- la ganancia debe ser: Total - Itbis - Costo
-- =============================================

ALTER procedure [dbo].[Ver_Utilidades_Precio_Detalle]
@Sucursal int = null,
@Fecha_Inicio datetime,
@Fecha_Final datetime
as
begin
Set DateFormat MDY
	select * from 
(
    --Ganancia en Equipo
    select 
        coalesce(sum(df.costo_equipo), 0) as Costo_Equipos,
        coalesce(sum(df.total), 0) as Venta_Equipos,
        coalesce(sum(df.Itbis), 0) as Itbis_Equipos,
        coalesce(sum(df.total - df.costo_equipo - df.Itbis), 0) as Ganancia_Equipos
    from  Detalle_Facturas df
    left join Equipos eq on df.id_equipo = eq.IMEI
    left join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura
    where 
        ft.estado = 'A'
        and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal) 
        and id_tipo in (1)
        and ID_Tipo_Factura <> 3
		and eq.Disponible = 'N'
        and df.literalF = ft.literal + convert(varchar(10), ft.numero_factura)
        and cast(df.fecha_registro as date) between @Fecha_Inicio and @Fecha_Final  
        and df.Estado = 'A'
		
) as EQUIPOS
	, (
		--Ganancia en Accesorio
		select coalesce(sum(ac.costo_equipo * df.cantidad), 0)                          as Costo_Accesorios
			 , coalesce(sum(df.total), 0)                                                as Venta_Accesorios
			 , coalesce(sum(df.Itbis), 0)                                                as Itbis_Accesorios
			 , coalesce(sum(df.total - (ac.costo_equipo * df.cantidad) - df.Itbis), 0)   as Ganancia_Accesorios
		  from Detalle_Facturas df
		 inner join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura		  
		 inner join Accesorios ac on df.id_equipo = ac.id_accesorio
		 where df.estado = 'A'
		     and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal) 
		   and id_tipo = 2
		   and ID_Tipo_Factura <> 3
		   and df.literalF = ft.literal + convert(varchar(10), ft.numero_factura)
		   and cast(df.fecha_registro as date) between @Fecha_Inicio and @Fecha_Final 														  
		   and df.Estado = 'A') as ACCESORIOS
	, (
		--Ganancia en reparaciones                                                                      
		select coalesce(sum(r.Costo * df.cantidad), 0)          as Costo_Reparaciones
			 , coalesce(sum(df.total), 0)                       as Venta_Reparaciones
			 , coalesce(sum(df.Itbis), 0)                       as Itbis_Reparaciones
			 , coalesce(sum(r.precio - r.Costo - df.Itbis), 0)  as Ganancia_Reparaciones
		  from Detalle_Facturas df		
		 inner join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura	
		 inner join dbo.Reparaciones r on r.numero_factura = ft.numero_factura	  		 
		 where df.estado = 'A'
		   and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal)  
		   and id_tipo in(3,4)
		   and ft.id_tipo_factura = 4
		   and df.literalF = ft.literal + convert(varchar(10), ft.numero_factura)
		   and cast(df.fecha_registro as date) between @Fecha_Inicio and @Fecha_Final 	
		   and df.Estado = 'A' And r.Estado = 'A') as REPARACIONES
	, (
		--Ganancia en desbloqueo
		select coalesce(sum(d.SubTotal * df.cantidad), 0)                       as Costo_Desbloqueos
			 , coalesce(sum(df.total), 0)                                       as Venta_Desbloqueos
			 , coalesce(sum(df.Itbis), 0)                                       as Itbis_Desbloqueos
			 , coalesce(sum(df.total - (d.SubTotal * df.cantidad) - df.Itbis), 0) as Ganancia_Desbloqueos
		  from Detalle_Facturas df
		 inner join dbo.Facturas ft on ft.Numero_Factura = df.Numero_Factura		  
		 inner join dbo.Desbloqueos d on df.id_equipo = d.ID_Desbloqueos and d.numero_factura = ft.numero_factura
		 where df.estado = 'A'
		     and ft.ID_Sucursal = coalesce(@Sucursal, ft.ID_Sucursal) 
		   and id_tipo = 5
		   and df.literalF = ft.literal + convert(varchar(10), ft.numero_factura)
		   and cast(df.fecha_registro as date) between @Fecha_Inicio and @Fecha_Final 	
		   and df.Estado = 'A' And d.Estado = 'A') as DESBLOQUEOS
end
