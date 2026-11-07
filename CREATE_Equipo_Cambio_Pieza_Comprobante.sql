-- ============================================================
-- Feature: Mejoras del Modulo de Laptops - Tarea 8 (comprobante de custodia)
-- Proposito: SP Equipo_Cambio_Pieza_Comprobante @ID_Cambio. Devuelve los datos del ticket que se imprime al registrar
--            un cambio de pieza cuya pieza retirada queda en custodia del tecnico: fecha, laptop (serial, marca y modelo),
--            pieza retirada, pieza instalada, tecnico, usuario que registro y almacen.
--            El limite "debe entregarse antes de" lo calcula el API: fecha del cambio + Laptops:HorasMaxCustodia.
-- Requiere: scripts de las Tareas 2 a 5 ejecutados ANTES (Equipo_Cambio_Pieza con ID_Accesorio_Retirado).
-- Ejecutar en TODAS las bases. Script idempotente (CREATE OR ALTER). Solo consulta.
-- Autor: MVSystem
-- Fecha: 2026-10-30
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Comprobante
    @ID_Cambio INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Cambio,
           C.Fecha_Registro,
           C.Imei,
           Marca = Ma.Descripcion,
           Modelo = M.Descripcion,
           C.Tipo_Pieza,
           Pieza_Retirada = AR.Descripcion_Accesorio,
           Modelo_Retirada = MR.Descripcion,
           Pieza_Instalada = AI.Descripcion_Accesorio,
           Modelo_Instalada = MI.Descripcion,
           C.Config_Anterior,
           C.Config_Nueva,
           C.Costo_Retirado,
           C.Tecnico,
           C.Usuario_Registro,
           C.Estado_Custodia,
           C.ID_Almacen,
           AC.Descripcion_Almacen
      FROM dbo.Equipo_Cambio_Pieza C
      LEFT JOIN dbo.Equipos E ON E.Imei = C.Imei
      LEFT JOIN dbo.Modelos M ON M.ID_Modelo = E.ID_Modelo
      LEFT JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
      LEFT JOIN dbo.Accesorios AR ON AR.ID_Accesorio = C.ID_Accesorio_Retirado
      LEFT JOIN dbo.Modelos MR ON MR.ID_Modelo = AR.ID_Modelo
      LEFT JOIN dbo.Accesorios AI ON AI.ID_Accesorio = C.ID_Accesorio_Instalado
      LEFT JOIN dbo.Modelos MI ON MI.ID_Modelo = AI.ID_Modelo
      LEFT JOIN dbo.Almacen_Cata AC ON AC.ID_Almacen = C.ID_Almacen
     WHERE C.ID_Cambio = @ID_Cambio;
END
GO
