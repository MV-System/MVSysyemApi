-- ============================================================
-- Feature: Módulo de Laptops - Fase 4, Tarea 14
-- Propósito: SP Equipo_Cambio_Pieza_Pendientes. Lista las piezas retiradas que
--            siguen en custodia (Estado_Custodia = 'PENDIENTE'), con las horas
--            transcurridas desde el cambio y si superan el límite (@Horas_Max).
--            Filtros opcionales por técnico y por almacén del cambio.
--            El límite lo envía el API desde appsettings.json (Laptops:HorasMaxCustodia, Tarea 16);
--            24 h es solo el valor por defecto del SP.
-- Requiere: CREATE_Equipo_Cambio_Pieza.sql
-- Autor: MVSystem
-- Fecha: 2026-10-28
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Pendientes
    @Tecnico     VARCHAR(60) = NULL,
    @ID_Almacen  INT = NULL,
    @Horas_Max   INT = 24
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Cambio,
           C.Imei,
           Modelo = M.Descripcion,
           Marca = Ma.Descripcion,
           Laptop_Disponible = E.Disponible,
           C.Tipo_Pieza,
           C.ID_Accesorio_Retirado,
           Pieza_Retirada = AR.Descripcion_Accesorio,
           C.Costo_Retirado,
           C.Tecnico,
           C.ID_Almacen,
           AC.Descripcion_Almacen,
           C.Usuario_Registro,
           C.Fecha_Registro,
           Horas_Transcurridas = CAST(DATEDIFF(MINUTE, C.Fecha_Registro, GETDATE()) / 60.0 AS DECIMAL(10,1)),
           Vencido = CAST(CASE WHEN DATEDIFF(MINUTE, C.Fecha_Registro, GETDATE()) > @Horas_Max * 60 THEN 1 ELSE 0 END AS BIT)
    FROM dbo.Equipo_Cambio_Pieza C
    INNER JOIN dbo.Equipos E ON E.Imei = C.Imei
    INNER JOIN dbo.Modelos M ON M.ID_Modelo = E.ID_Modelo
    INNER JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
    LEFT JOIN dbo.Accesorios AR ON AR.ID_Accesorio = C.ID_Accesorio_Retirado
    LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = C.ID_Almacen
    WHERE C.Estado_Custodia = 'PENDIENTE'
      AND (@Tecnico IS NULL OR C.Tecnico = @Tecnico)
      AND (@ID_Almacen IS NULL OR C.ID_Almacen = @ID_Almacen)
    ORDER BY C.Tecnico, C.Fecha_Registro, C.ID_Cambio;
END
GO
