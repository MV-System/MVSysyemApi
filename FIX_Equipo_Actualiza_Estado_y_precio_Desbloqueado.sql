-- ============================================================
-- FIX: Equipo_Actualiza_Estado_y_precio actualizaba "Desbloqueado" de TODOS los equipos
-- Problema: al final del SP original
--      if @Id_estado_Bloqueo = 1
--      update equipos set desbloqueado = 'S'
--      else update equipos set desbloqueado = 'N'
--      where imei = @Imei
--   el WHERE pertenece solo al UPDATE del ELSE. Cuando el estado de bloqueo era 1 el UPDATE
--   no tenia WHERE y marcaba como desbloqueados (Desbloqueado = 'S') todos los equipos.
-- Correccion: Desbloqueado se calcula dentro del UPDATE principal, solo para @Imei.
-- No cambia la firma (parametros) ni el historico: el resto del SP es identico al original.
-- Ejecutar tambien en la BD espejo (el API replica este SP con _sync.Ejecutar).
-- Autor: MVSystem
-- Fecha: 2026-10-28
-- ============================================================
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
