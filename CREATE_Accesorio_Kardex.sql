-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 7 (kardex por accesorio y reporte general de consumo)
-- Proposito: ver que movimientos tuvo un accesorio (y los componentes en general) y por que.
--   Movimientos que el sistema REGISTRA (compras, ajustes manuales y transferencias de accesorios no dejan registro):
--     VENTA        (Salida)  : detalle de factura de accesorios (ID_Tipo = 2) de facturas vigentes (no anuladas).
--     CAMBIO_PIEZA (Salida)  : pieza instalada en un cambio de pieza de laptop (no anulado).
--     RECEPCION    (Entrada) : pieza retirada recibida en custodia (entra a la variante en el almacen del cambio).
--   1. dbo.fn_Accesorio_Movimientos(): todos esos movimientos en una sola lista (la usan el kardex y el reporte).
--   2. Accesorio_Kardex_Buscar: busca accesorios por descripcion / modelo / codigo (para elegir cual consultar).
--   3. Accesorio_Kardex_Consultar @ID_Accesorio, @Fecha_Inicio, @Fecha_Final: lista ordenada por fecha con saldo.
--        El saldo es REFERENCIAL: se calcula hacia atras desde la existencia actual y los movimientos registrados
--        (Saldo inicial = existencia actual - movimientos registrados desde la fecha de inicio). No incluye lo que el
--        sistema no registra (compras, ajustes, transferencias).
--   4. Componentes_Movimientos_Consultar: reporte general de consumo de componentes con filtros por fecha, almacen,
--        tecnico, tipo de pieza (RAM / ALMACENAMIENTO) y motivo.
--   Nota: todas las fechas estan en hora local (servidor - 4 horas). Los cambios de pieza anteriores a
--   UPDATE_Laptops_Fechas_Hora_Local.sql (Tarea 9) quedaron con la hora del servidor.
-- Requiere: scripts de las Tareas 1 a 6 ejecutados ANTES (usa Equipo_Cambio_Pieza y Componente_Modelo_Tipo).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER). Solo consulta: no modifica datos.
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================

-- ------------------------------------------------------------
-- 1. Movimientos registrados de los accesorios
-- ------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.fn_Accesorio_Movimientos ()
RETURNS TABLE
AS
RETURN
(
    -- Ventas: facturas vigentes
    SELECT A.ID_Accesorio,
           Fecha     = F.Fecha_registro,
           Motivo    = CAST('VENTA' AS VARCHAR(20)),
           Documento = CAST(LTRIM(RTRIM(ISNULL(F.Literal, ''))) + CONVERT(VARCHAR(10), F.Numero_Factura) AS VARCHAR(40)),
           Entrada   = CAST(0 AS INT),
           Salida    = CAST(DF.Cantidad AS INT),
           Usuario   = CAST(F.Usuario AS VARCHAR(60)),
           Tecnico   = CAST(NULL AS VARCHAR(60))
      FROM dbo.Detalle_Facturas DF
     INNER JOIN dbo.Facturas F
             ON F.Numero_Factura = DF.Numero_Factura
            AND DF.LiteralF = ISNULL(F.Literal, '') + CONVERT(VARCHAR(10), F.Numero_Factura)
     INNER JOIN dbo.Accesorios A ON A.ID_Accesorio = TRY_CAST(DF.ID_Equipo AS BIGINT)
     WHERE DF.ID_Tipo = 2
       AND DF.Estado = 'A'
       AND F.Estado = 'A'

    UNION ALL

    -- Cambio de pieza: la pieza instalada sale del inventario
    SELECT C.ID_Accesorio_Instalado,
           C.Fecha_Registro,
           'CAMBIO_PIEZA',
           CAST(C.Imei AS VARCHAR(40)),
           0,
           1,
           CAST(C.Usuario_Registro AS VARCHAR(60)),
           CAST(C.Tecnico AS VARCHAR(60))
      FROM dbo.Equipo_Cambio_Pieza C
     WHERE C.Estado_Custodia <> 'ANULADO'
       AND C.ID_Accesorio_Instalado IS NOT NULL

    UNION ALL

    -- Recepcion de la pieza retirada: entra a la variante del almacen del cambio
    -- (la pieza usada; en cambios anteriores a la Tarea 5 puede ser la misma pieza o su variante '(Usado)')
    SELECT A.ID_Accesorio,
           C.Fecha_Recepcion,
           'RECEPCION',
           CAST(C.Imei AS VARCHAR(40)),
           1,
           0,
           CAST(C.Usuario_Recepcion AS VARCHAR(60)),
           CAST(C.Tecnico AS VARCHAR(60))
      FROM dbo.Equipo_Cambio_Pieza C
     INNER JOIN dbo.Accesorios R ON R.ID_Accesorio = C.ID_Accesorio_Retirado
     INNER JOIN dbo.Accesorios A
             ON A.ID_Almacen = C.ID_Almacen
            AND (A.ID_Accesorio = R.ID_Accesorio
                 OR (A.ID_Modelo = R.ID_Modelo
                     AND (A.Descripcion_Accesorio = R.Descripcion_Accesorio
                          OR (R.Descripcion_Accesorio NOT LIKE '%(Usado)'
                              AND A.Descripcion_Accesorio = R.Descripcion_Accesorio + ' (Usado)'))))
     WHERE C.Estado_Custodia = 'RECIBIDA'
       AND C.Fecha_Recepcion IS NOT NULL
       -- Pieza que aun no era usada: si existe su variante '(Usado)' en el almacen, la entrada fue a esa variante
       AND (R.Descripcion_Accesorio LIKE '%(Usado)'
            OR A.Descripcion_Accesorio LIKE '%(Usado)'
            OR NOT EXISTS (SELECT 1 FROM dbo.Accesorios U
                            WHERE U.ID_Almacen = C.ID_Almacen AND U.ID_Modelo = R.ID_Modelo
                              AND U.Descripcion_Accesorio = R.Descripcion_Accesorio + ' (Usado)'))
)
GO

-- ------------------------------------------------------------
-- 2. Buscar accesorios para el kardex
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Accesorio_Kardex_Buscar
    @ID_Almacen INT = NULL,
    @Filtro     VARCHAR(100) = ''
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 100
           A.ID_Accesorio,
           A.Descripcion_Accesorio,
           Modelo = M.Descripcion,
           A.ID_Almacen,
           AC.Descripcion_Almacen,
           Cantidad = ISNULL(A.Cantidad, 0),
           A.Estado
      FROM dbo.Accesorios A
     INNER JOIN dbo.Modelos M ON M.ID_Modelo = A.ID_Modelo
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = A.ID_Almacen
     WHERE (@ID_Almacen IS NULL OR A.ID_Almacen = @ID_Almacen)
       AND (ISNULL(@Filtro, '') = ''
            OR A.Descripcion_Accesorio LIKE '%' + @Filtro + '%'
            OR M.Descripcion LIKE '%' + @Filtro + '%'
            OR CONVERT(VARCHAR(30), A.Codigo_Barra) = @Filtro
            OR CONVERT(VARCHAR(30), A.ID_Accesorio) = @Filtro)
     ORDER BY M.Descripcion, A.Descripcion_Accesorio, AC.Descripcion_Almacen;
END
GO

-- ------------------------------------------------------------
-- 3. Kardex de un accesorio (saldo referencial)
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Accesorio_Kardex_Consultar
    @ID_Accesorio BIGINT,
    @Fecha_Inicio DATETIME,
    @Fecha_Final  DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Existencia INT, @Descripcion VARCHAR(100), @Almacen VARCHAR(100);

    SELECT @Existencia = ISNULL(A.Cantidad, 0),
           @Descripcion = A.Descripcion_Accesorio,
           @Almacen = AC.Descripcion_Almacen
      FROM dbo.Accesorios A
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = A.ID_Almacen
     WHERE A.ID_Accesorio = @ID_Accesorio;

    IF @Existencia IS NULL
        THROW 50700, 'El accesorio no existe.', 1;

    DECLARE @Ini DATETIME = CAST(CAST(ISNULL(@Fecha_Inicio, '19000101') AS DATE) AS DATETIME);
    DECLARE @FinExcl DATETIME = DATEADD(DAY, 1, CAST(CAST(ISNULL(@Fecha_Final, DATEADD(HOUR, -4, GETDATE())) AS DATE) AS DATETIME));

    -- Saldo inicial referencial = existencia actual - lo registrado desde la fecha de inicio
    DECLARE @NetoDesdeInicio INT =
        (SELECT ISNULL(SUM(Mv.Entrada - Mv.Salida), 0)
           FROM dbo.fn_Accesorio_Movimientos() Mv
          WHERE Mv.ID_Accesorio = @ID_Accesorio AND Mv.Fecha >= @Ini);
    DECLARE @SaldoInicial INT = @Existencia - @NetoDesdeInicio;

    ;WITH M AS (
        SELECT Mv.Fecha, Mv.Motivo, Mv.Documento, Mv.Entrada, Mv.Salida, Mv.Usuario
          FROM dbo.fn_Accesorio_Movimientos() Mv
         WHERE Mv.ID_Accesorio = @ID_Accesorio AND Mv.Fecha >= @Ini AND Mv.Fecha < @FinExcl
    ),
    S AS (
        SELECT M.*,
               Saldo = @SaldoInicial + SUM(M.Entrada - M.Salida)
                       OVER (ORDER BY M.Fecha, M.Motivo, M.Documento ROWS UNBOUNDED PRECEDING)
          FROM M
    )
    SELECT X.Fecha, X.Motivo, X.Documento, X.Entrada, X.Salida, X.Usuario, X.Saldo,
           Descripcion_Accesorio = @Descripcion, Descripcion_Almacen = @Almacen, Existencia_Actual = @Existencia
      FROM (
            SELECT Orden = 0, Fecha = @Ini, Motivo = CAST('SALDO_INICIAL' AS VARCHAR(20)), Documento = CAST(NULL AS VARCHAR(40)),
                   Entrada = 0, Salida = 0, Usuario = CAST(NULL AS VARCHAR(60)), Saldo = @SaldoInicial
            UNION ALL
            SELECT 1, S.Fecha, S.Motivo, S.Documento, S.Entrada, S.Salida, S.Usuario, S.Saldo FROM S
           ) X
     ORDER BY X.Orden, X.Fecha, X.Motivo, X.Documento;
END
GO

-- ------------------------------------------------------------
-- 4. Reporte general de consumo de componentes
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.Componentes_Movimientos_Consultar
    @Fecha_Inicio DATETIME,
    @Fecha_Final  DATETIME,
    @ID_Almacen   INT = NULL,
    @Tecnico      VARCHAR(60) = NULL,
    @Tipo_Pieza   VARCHAR(20) = NULL,    -- 'RAM' | 'ALMACENAMIENTO'
    @Motivo       VARCHAR(20) = NULL     -- 'VENTA' | 'CAMBIO_PIEZA' | 'RECEPCION'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Ini DATETIME = CAST(CAST(ISNULL(@Fecha_Inicio, '19000101') AS DATE) AS DATETIME);
    DECLARE @FinExcl DATETIME = DATEADD(DAY, 1, CAST(CAST(ISNULL(@Fecha_Final, DATEADD(HOUR, -4, GETDATE())) AS DATE) AS DATETIME));

    SELECT Mv.Fecha,
           Mv.Motivo,
           Mv.Documento,
           A.ID_Accesorio,
           A.Descripcion_Accesorio,
           Modelo = Mo.Descripcion,
           T.Tipo_Pieza,
           A.ID_Almacen,
           AC.Descripcion_Almacen,
           Mv.Entrada,
           Mv.Salida,
           Costo = CAST(A.Costo_Equipo AS DECIMAL(18,2)),
           Mv.Usuario,
           Mv.Tecnico
      FROM dbo.fn_Accesorio_Movimientos() Mv
     INNER JOIN dbo.Accesorios A ON A.ID_Accesorio = Mv.ID_Accesorio
     INNER JOIN dbo.Modelos Mo ON Mo.ID_Modelo = A.ID_Modelo
     INNER JOIN dbo.Componente_Modelo_Tipo T ON T.ID_Modelo = A.ID_Modelo        -- solo componentes
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = A.ID_Almacen
     WHERE Mv.Fecha >= @Ini AND Mv.Fecha < @FinExcl
       AND (@ID_Almacen IS NULL OR A.ID_Almacen = @ID_Almacen)
       AND (@Tecnico IS NULL OR Mv.Tecnico = @Tecnico)
       AND (@Tipo_Pieza IS NULL OR T.Tipo_Pieza = @Tipo_Pieza)
       AND (@Motivo IS NULL OR Mv.Motivo = @Motivo)
     ORDER BY Mv.Fecha DESC, Mv.Motivo, Mv.Documento;
END
GO
