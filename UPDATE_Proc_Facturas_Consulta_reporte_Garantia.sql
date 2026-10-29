-- ============================================================
-- Feature: Tareas pendientes - Garantía en el encabezado de la factura impresa
-- Propósito: Proc_Facturas_Consulta_reporte devuelve, además, la descripción de la garantía
--            que se eligió al crear la factura (Facturas.ID_Garantia -> Garantias_Cata),
--            en la columna nueva "Garantia". Si la factura no tiene garantía, queda NULL.
--            Solo se AGREGA una columna y un LEFT JOIN (más su GROUP BY); parámetros y demás
--            columnas son idénticos.
--   OJO: este script INCLUYE el parametro @Estado de UPDATE_Proc_Facturas_Consulta_reporte_Anuladas.sql
--   (reimpresion de anuladas), asi que reemplaza a ese script: si ya ejecutaste aquel, este lo
--   conserva; si aun no lo ejecutaste, con este bastan.
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================
-- Author:  Xavier Mejia
-- Create date: 23/OCT/2022
-- Description: Consultar las facturas.
-- Modification: En sustitución de [Facturas_Consulta]
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[Proc_Facturas_Consulta_reporte] --504,1
  @Numero_factura varchar(100),
  @Almacen int =0,
  @Vendedor INT = 0,
  @Fecha_Inicio datetime = '',
  @Fecha_Final datetime = '',
  @Estado char(1) = 'A'
  AS
  BEGIN


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

   --Suma total inventario
 DECLARE @TotalFacturado money;
  SELECT @TotalFacturado =  SUM(Total)
    FROM Facturas F
 INNER JOIN dbo.Clientes AS C
         ON F.ID_Cliente = C.ID_Cliente
 INNER JOIN dbo.Contactos Co
         ON C.ID_Contacto = Co.ID_Contacto
 INNER JOIN Telefonos AS te
         ON te.ID_Contacto = C.ID_Contacto
 INNER JOIN dbo.Tipos_Facturas_Cata AS TF
         ON F.ID_Tipo_Factura = TF.ID_Tipo_Factura
 INNER JOIN dbo.Tipos_Pagos_Cata AS TP
         ON F.ID_Tipo_Pagos = TP.ID_Tipo_Pagos
 INNER JOIN Vendedores v
      ON v.ID_Vendedor = F.ID_Vendedor
 INNER JOIN Contactos CS
      ON CS.ID_Contacto = v.ID_Contacto
 --INNER JOIN Historio_Pagos AS hp
        -- ON F.Numero_Factura = hp.numero_factura
    Where F.Numero_Factura = @Numero_factura
    AND F.id_sucursal =@Almacen
   -- AND F.ID_Vendedor =@Vendedor
    AND ((@Fecha_Inicio ='' AND @Fecha_Final ='') or (Year(F.Fecha_registro) * 10000+Month(F.Fecha_registro)* 100+Day(F.Fecha_registro) ) BETWEEN @FIni AND @FFin)
    AND F.Estado = @Estado


   SELECT F.Numero_Factura AS [Numero de Factura]
   ,Co.Nombres
   ,Co.Apellidos
   ,F.ID_Cliente,
   te.Numero_Telefono AS Telefono
   ,TF.Descripcion_Factura_Tipo AS [Tipo De Factura]
   ,CS.Nombres AS [Vendedor]
   ,TP.Descripcion_Tipo_Pagos AS [Tipo de Pago] ,
   SC.Sucursal_Descripcion AS Almacen
   ,F.SubTotal
   ,F.Descuento
   ,F.NCF_NUMERO AS NCF,
   F.RNC
   ,F.Abono as Abono
   ,F.Total
   ,F.Itbis
   ,F.Nota
   ,G.Descripcion_Garantia AS Garantia
   --,max(hp.Fecha_registro) as [Fecha de la Factura]
   ,F.Fecha_registro AS [Fecha de la Factura],
   TotalFacturado =@TotalFacturado
      -- INTO #Temporal
       FROM [Facturas] AS F
 INNER JOIN dbo.Clientes AS C
         ON F.ID_Cliente = C.ID_Cliente
 INNER JOIN dbo.Contactos Co
         ON C.ID_Contacto = Co.ID_Contacto
 INNER JOIN Telefonos AS te
         ON te.ID_Contacto = C.ID_Contacto
 INNER JOIN dbo.Tipos_Facturas_Cata AS TF
         ON F.ID_Tipo_Factura = TF.ID_Tipo_Factura
 INNER JOIN dbo.Tipos_Pagos_Cata AS TP
         ON F.ID_Tipo_Pagos = TP.ID_Tipo_Pagos
 INNER JOIN Vendedores v
      ON v.ID_Vendedor = F.ID_Vendedor
 INNER JOIN Contactos CS
      ON CS.ID_Contacto = v.ID_Contacto
 --INNER JOIN Historio_Pagos AS hp
   --      ON F.Numero_Factura = hp.numero_factura
 INNER JOIN Sucursales_cata AS SC
      ON SC.id_sucursal = F.id_sucursal
 LEFT JOIN dbo.Garantias_Cata AS G
      ON G.ID_Garantia = F.ID_Garantia
      Where F.Numero_Factura = @Numero_factura
    AND F.id_sucursal =@Almacen
     AND F.id_sucursal =@Almacen
   -- AND F.ID_Vendedor =@Vendedor
     AND ((@Fecha_Inicio ='' AND @Fecha_Final ='') or (Year(F.Fecha_registro) * 10000+Month(F.Fecha_registro)* 100+Day(F.Fecha_registro) ) BETWEEN @FIni AND @FFin)
     AND F.Estado = @Estado

GROUP BY F.Numero_Factura
  ,Co.Nombres
        ,Co.Apellidos
        ,TF.Descripcion_Factura_Tipo
        ,F.ID_Vendedor
  ,CS.Nombres
        ,TP.Descripcion_Tipo_Pagos
        ,F.SubTotal
        ,F.Descuento
  ,F.Abono
        ,F.Total
  ,F.Itbis
        ,F.Nota
  ,G.Descripcion_Garantia
  ,F.Fecha_registro
  ,F.id_sucursal
  ,F.ID_Cliente,
  F.NCF_NUMERO,
  F.RNC,
  te.Numero_Telefono,
  SC.Sucursal_Descripcion
  ORDER BY [Numero de Factura] ASC

END
GO
