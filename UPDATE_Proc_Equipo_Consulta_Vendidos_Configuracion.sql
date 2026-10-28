-- ============================================================
-- Feature: Módulo de Laptops - Fase 2, Tarea 7 (paso final)
-- Propósito: Proc_Equipo_Consulta_Vendidos devuelve la columna nueva
--            Configuracion = dbo.fn_Equipo_Configuracion_Texto(Imei)
--            (ej. '12GB DDR4 / 256GB SSD SATA'; NULL si no tiene).
--            No se quita ni se renombra ninguna columna ni parámetro.
-- Requiere: CREATE_Equipo_Configuracion.sql ejecutado.
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
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
