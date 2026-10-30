-- ============================================================
-- Feature: Tareas pendientes - Facturas con cliente en cero / que no salen en la lista
-- Propósito: DIAGNOSTICO DE SOLO LECTURA. Ejecutar en CADA base de datos de produccion.
--   No modifica nada: solo SELECT.
--   Una factura no aparece en la lista de facturas (Proc_Facturas_Consulta) cuando falla alguna
--   de sus uniones INNER JOIN: Clientes, Contactos, Telefonos, Tipos_Facturas_Cata,
--   Tipos_Pagos_Cata o Sucursales_cata. Este script detecta cuales facturas estan en esa situacion
--   y por que, y ayuda a ver si hay un patron (usuario, sucursal, mes).
-- Resultados (6 tablas):
--   1. Resumen por causa
--   2. Detalle de las facturas afectadas (las 300 mas recientes)
--   3. Patron por usuario y sucursal (cliente en cero o inexistente)
--   4. Patron por mes
--   5. Prefacturas con cliente en cero o inexistente
--   6. Abonos de facturas a credito cuyo cliente no coincide con el de la factura
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================
SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#Afectadas') IS NOT NULL DROP TABLE #Afectadas;

SELECT F.Numero_Factura, F.ID_Sucursal, F.Fecha_Registro, F.Usuario, F.Total, F.ID_Tipo_Pagos, F.Estado, F.ID_Cliente,
       Causa = CAST(CASE
         WHEN F.ID_Cliente = 0 THEN 'cliente en cero'
         WHEN C.ID_Cliente IS NULL THEN 'cliente inexistente'
         WHEN Co.ID_Contacto IS NULL THEN 'cliente sin contacto'
         WHEN NOT EXISTS (SELECT 1 FROM dbo.Telefonos T WHERE T.ID_Contacto = Co.ID_Contacto) THEN 'cliente sin telefono'
         WHEN TF.ID_Tipo_Factura IS NULL THEN 'tipo de factura inexistente'
         WHEN TP.ID_Tipo_Pagos IS NULL THEN 'tipo de pago inexistente'
         WHEN SC.id_sucursal IS NULL THEN 'sucursal inexistente'
         END AS VARCHAR(40))
INTO #Afectadas
FROM dbo.Facturas F
LEFT JOIN dbo.Clientes C ON C.ID_Cliente = F.ID_Cliente
LEFT JOIN dbo.Contactos Co ON Co.ID_Contacto = C.ID_Contacto
LEFT JOIN dbo.Tipos_Facturas_Cata TF ON TF.ID_Tipo_Factura = F.ID_Tipo_Factura
LEFT JOIN dbo.Tipos_Pagos_Cata TP ON TP.ID_Tipo_Pagos = F.ID_Tipo_Pagos
LEFT JOIN dbo.Sucursales_cata SC ON SC.id_sucursal = F.ID_Sucursal
WHERE F.ID_Cliente = 0
   OR C.ID_Cliente IS NULL
   OR Co.ID_Contacto IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.Telefonos T WHERE T.ID_Contacto = Co.ID_Contacto)
   OR TF.ID_Tipo_Factura IS NULL
   OR TP.ID_Tipo_Pagos IS NULL
   OR SC.id_sucursal IS NULL;

-- 1. Resumen por causa
SELECT Causa, Facturas = COUNT(*), Desde = MIN(Fecha_Registro), Hasta = MAX(Fecha_Registro),
       Total_Facturado = SUM(Total), Anuladas = SUM(CASE WHEN Estado = 'X' THEN 1 ELSE 0 END)
  FROM #Afectadas
 GROUP BY Causa
 ORDER BY Facturas DESC;

-- 2. Detalle (las 300 mas recientes)
SELECT TOP (300) Numero_Factura, ID_Sucursal, Fecha_Registro, Usuario, Total, ID_Tipo_Pagos, Estado, ID_Cliente, Causa
  FROM #Afectadas
 ORDER BY Fecha_Registro DESC;

-- 3. Patron por usuario y sucursal (solo cliente en cero o inexistente)
SELECT Usuario, ID_Sucursal, Facturas = COUNT(*), Ultima = MAX(Fecha_Registro)
  FROM #Afectadas
 WHERE Causa IN ('cliente en cero', 'cliente inexistente')
 GROUP BY Usuario, ID_Sucursal
 ORDER BY Facturas DESC;

-- 4. Patron por mes
SELECT Mes = CONVERT(VARCHAR(7), Fecha_Registro, 120), Causa, Facturas = COUNT(*)
  FROM #Afectadas
 GROUP BY CONVERT(VARCHAR(7), Fecha_Registro, 120), Causa
 ORDER BY Mes DESC, Facturas DESC;

-- 5. Prefacturas con cliente en cero o inexistente
SELECT P.Numero_PreFactura, P.ID_Cliente, P.Estado, P.ID_Sucursal, P.Usuario
  FROM dbo.PreFacturas P
  LEFT JOIN dbo.Clientes C ON C.ID_Cliente = P.ID_Cliente
 WHERE P.ID_Cliente = 0 OR C.ID_Cliente IS NULL
 ORDER BY P.Numero_PreFactura DESC;

-- 6. Facturas a credito: abonos registrados con un cliente distinto al de la factura
--    (las consultas de abonos y utilidades cruzan por cliente, asi que esos abonos se pierden)
SELECT hp.Numero_factura, hp.Id_Sucursal, ClienteFactura = ft.ID_Cliente, ClienteAbono = hp.id_Cliente_Suplidor,
       hp.Total_Abono, hp.Fecha_registro
  FROM dbo.Historio_Pagos hp
  INNER JOIN dbo.Facturas ft
          ON ft.Numero_Factura = TRY_CONVERT(INT, hp.Numero_factura)
         AND ft.ID_Sucursal = hp.Id_Sucursal
 WHERE ft.ID_Tipo_Pagos = 2
   AND hp.id_Cliente_Suplidor <> ft.ID_Cliente
 ORDER BY hp.Fecha_registro DESC;

DROP TABLE #Afectadas;
GO
