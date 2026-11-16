-- ============================================================
-- Feature: Prefacturas anuladas - usuario real y motivo de la anulacion
-- Proposito:
--   1. Columna nueva y opcional en PreFacturas: Motivo_Anulacion VARCHAR(500).
--      Las prefacturas ya anuladas quedan con el motivo en NULL (sin registro).
--   2. PreFacturas_Anular recibe @Motivo VARCHAR(500) = NULL (parametro nuevo al final; los
--      existentes no cambian). La logica original queda IDENTICA; solo se guarda el motivo en
--      el mismo UPDATE. El usuario que anula llega en @Usuario_Anulacion (ahora el real, tomado
--      de la sesion del API).
--   3. PreFacturas_Consultar devuelve tambien Motivo_Anulacion.
--   El motivo es obligatorio: lo exige el API (minimo 5 caracteres) y el front. El SP lo acepta
--   NULL para no romper otros llamadores.
-- Es la misma definicion de la seccion 10 de SCRIPT_MODULO_LAPTOPS_COMPLETO.sql; se puede correr
-- antes o despues (idempotente).
-- Autor: MVSystem
-- Fecha: 2026-11-16
-- ============================================================

-- ------------------------------------------------------------
-- Columna Motivo_Anulacion en PreFacturas (motivo de la anulacion, opcional en la tabla)
-- ------------------------------------------------------------
IF COL_LENGTH('dbo.PreFacturas', 'Motivo_Anulacion') IS NULL
    ALTER TABLE dbo.PreFacturas ADD Motivo_Anulacion VARCHAR(500) NULL;
GO

-- ------------------------------------------------------------
-- PreFacturas_Anular: anular una prefactura (estado 'X', el estandar de anuladas en el sistema)
-- ------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[PreFacturas_Anular]
@Numero_PreFactura INT,
@Usuario_Anulacion VARCHAR(50),
@Literal CHAR(1) = NULL,   -- el numero de prefactura se repite entre literales: con @Literal solo se anula la de esa literal
@Motivo VARCHAR(500) = NULL
AS
BEGIN
    -- Cambia el estado a 'X' (Anulada), registra usuario, fecha y motivo de anulacion
    UPDATE PreFacturas
    SET Estado = 'X',
        Fecha_Procesamiento = GETDATE(),
        Usuario_Procesamiento = @Usuario_Anulacion,
        Motivo_Anulacion = NULLIF(LTRIM(RTRIM(@Motivo)), '')
    WHERE Numero_PreFactura = @Numero_PreFactura
      AND (@Literal IS NULL OR Literal = @Literal)

    -- Tambien se actualizan los detalles
    UPDATE PreDetalle_Facturas
    SET Estado = 'X',
        Usuario = @Usuario_Anulacion,
        Fecha_registro = GETDATE()
    WHERE Numero_PreFactura = @Numero_PreFactura
      AND (@Literal IS NULL OR LiteralF = @Literal + CONVERT(VARCHAR(10), @Numero_PreFactura))
END
GO

-- ------------------------------------------------------------
-- PreFacturas_Consultar
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[PreFacturas_Consultar]
  @Estado CHAR(1) = NULL,         -- Opcional: filtrar por estado ('P', 'C', 'X') P= pendientes; C= cerradas; X= anuladas
  @ID_Sucursal INT = NULL,        -- Opcional: filtrar por sucursal
  @ID_Cliente INT = NULL,         -- Opcional: filtrar por cliente
  @Criterio VARCHAR(100) = NULL   -- Opcional: buscar por nombre o telefono
AS
BEGIN
    SELECT
        pf.Numero_PreFactura,
        pf.ID_Cliente,
        c.Nombres,
        c.Apellidos,
        t.Numero_Telefono,
        pf.ID_Tipo_Factura,
        tf.Descripcion_Factura_Tipo AS Tipo_Factura,
        pf.ID_Vendedor,
        ISNULL(cv.Nombres, '') AS Vendedor_Nombres,
        pf.ID_Tipo_Pagos,
        tp.Descripcion_Tipo_Pagos AS Tipo_Pago,
        pf.ID_Sucursal,
        sc.Sucursal_Descripcion AS Sucursal,
        pf.SubTotal,
        pf.Descuento,
        pf.Itbis,
        pf.Total,
        pf.Cantidad_Articulos,
        pf.Nota,
        pf.Estado,
        pf.Usuario,
        pf.Fecha_Registro,
        pf.Fecha_Procesamiento,
        pf.Usuario_Procesamiento,
        pf.Motivo_Anulacion,
        pf.NFCTipoNumero,
        pf.NFCFISCAL,
        pf.NCF_NUMERO,
        pf.RNC,
        pf.Cotizacion_Numero,
        pf.Literal
    FROM PreFacturas pf
    INNER JOIN Clientes cl ON pf.ID_Cliente = cl.ID_Cliente
    INNER JOIN Contactos c ON cl.ID_Contacto = c.ID_Contacto
    LEFT JOIN Telefonos t ON t.ID_Contacto = c.ID_Contacto
    INNER JOIN Tipos_Facturas_Cata tf ON pf.ID_Tipo_Factura = tf.ID_Tipo_Factura
    LEFT JOIN Vendedores v ON v.ID_Vendedor = pf.ID_Vendedor
    LEFT JOIN Contactos cv ON v.ID_Contacto = cv.ID_Contacto
    LEFT JOIN Tipos_Pagos_Cata tp ON pf.ID_Tipo_Pagos = tp.ID_Tipo_Pagos
    LEFT JOIN Sucursales_cata sc ON pf.ID_Sucursal = sc.id_sucursal
    WHERE (@Estado IS NULL OR pf.Estado = @Estado)
      AND (@ID_Sucursal IS NULL OR pf.ID_Sucursal = @ID_Sucursal)
      AND (@ID_Cliente IS NULL OR pf.ID_Cliente = @ID_Cliente)
      AND (
            @Criterio IS NULL
            OR c.Nombres LIKE '%' + @Criterio + '%'
            OR c.Apellidos LIKE '%' + @Criterio + '%'
            OR c.Nombres + ' '+ c.Apellidos LIKE '%' + @Criterio + '%'
            OR t.Numero_Telefono LIKE '%' + @Criterio + '%'
          )
    ORDER BY pf.Fecha_Registro DESC
END
GO
