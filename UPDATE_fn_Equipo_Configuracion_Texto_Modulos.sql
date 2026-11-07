-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 2 (modulos por laptop)
-- Proposito: fn_Equipo_Configuracion_Texto arma el texto desde los MODULOS activos de la laptop:
--              RAM     = total + tipo           -> '16GB DDR4'
--              Discos  = cada uno               -> '256GB SSD SATA + 1TB HDD'
--              Texto   = 'RAM / discos'         -> '16GB DDR4 / 256GB SSD SATA + 1TB HDD'
--            (1024 GB o multiplos se muestran en TB). Si la laptop no tiene modulos, el comportamiento
--            es el anterior (lee Equipo_Configuracion). Devuelve NULL si no hay configuracion.
-- Requiere: CREATE_Equipo_Configuracion_Modulo.sql ejecutado ANTES.
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER); el resto del sistema sigue usando la funcion igual.
-- Autor: MVSystem
-- Fecha: 2026-10-29
-- ============================================================
CREATE OR ALTER FUNCTION dbo.fn_Equipo_Configuracion_Texto (@Imei VARCHAR(30))
RETURNS VARCHAR(100)
AS
BEGIN
    DECLARE @Texto VARCHAR(100);

    -- Con modulos: se arma desde los modulos activos
    IF EXISTS (SELECT 1 FROM dbo.Equipo_Configuracion_Modulo WHERE Imei = @Imei AND Estado = 'A')
    BEGIN
        DECLARE @Ram INT, @RamTipo VARCHAR(30), @Discos VARCHAR(100);

        SELECT @Ram = SUM(M.Capacidad_GB)
          FROM dbo.Equipo_Configuracion_Modulo M
         WHERE M.Imei = @Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM';

        SELECT TOP 1 @RamTipo = M.Tipo
          FROM dbo.Equipo_Configuracion_Modulo M
         WHERE M.Imei = @Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'RAM'
         ORDER BY M.ID_Modulo;

        SET @Discos = STUFF((
            SELECT ' + '
                   + CASE WHEN M.Capacidad_GB >= 1024 AND M.Capacidad_GB % 1024 = 0
                          THEN CAST(M.Capacidad_GB / 1024 AS VARCHAR(10)) + 'TB'
                          ELSE CAST(M.Capacidad_GB AS VARCHAR(10)) + 'GB' END
                   + ' ' + M.Tipo
              FROM dbo.Equipo_Configuracion_Modulo M
             WHERE M.Imei = @Imei AND M.Estado = 'A' AND M.Tipo_Pieza = 'ALMACENAMIENTO'
             ORDER BY M.ID_Modulo
               FOR XML PATH('')), 1, 3, '');

        SET @Texto = NULLIF(LTRIM(RTRIM(
                ISNULL(CASE WHEN @Ram IS NOT NULL THEN CAST(@Ram AS VARCHAR(10)) + 'GB ' + @RamTipo END, '')
                + CASE WHEN @Ram IS NOT NULL AND @Discos IS NOT NULL THEN ' / ' ELSE '' END
                + ISNULL(@Discos, ''))), '');

        RETURN @Texto;
    END

    -- Sin modulos: comportamiento anterior
    SELECT @Texto =
        NULLIF(
            LTRIM(RTRIM(
                ISNULL(CASE WHEN C.Ram_GB IS NOT NULL
                            THEN CAST(C.Ram_GB AS VARCHAR(10)) + 'GB' + ISNULL(' ' + C.Ram_Tipo, '')
                       END, '')
                + CASE WHEN C.Ram_GB IS NOT NULL AND C.Almacenamiento_GB IS NOT NULL THEN ' / ' ELSE '' END
                + ISNULL(CASE WHEN C.Almacenamiento_GB IS NOT NULL
                              THEN CAST(C.Almacenamiento_GB AS VARCHAR(10)) + 'GB' + ISNULL(' ' + C.Almacenamiento_Tipo, '')
                         END, '')
            )), '')
    FROM dbo.Equipo_Configuracion C
    WHERE C.Imei = @Imei;

    RETURN @Texto;
END
GO

-- Verificacion: texto de cada laptop con configuracion
SELECT C.Imei, Texto = dbo.fn_Equipo_Configuracion_Texto(C.Imei)
  FROM dbo.Equipo_Configuracion C
 ORDER BY C.ID_Configuracion;
GO
