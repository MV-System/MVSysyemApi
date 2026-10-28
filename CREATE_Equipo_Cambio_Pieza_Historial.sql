-- ============================================================
-- Feature: Módulo de Laptops - Fase 3, Tarea 10
-- Propósito: SP Equipo_Cambio_Pieza_Historial. Devuelve todos los
--            cambios de pieza de una laptop (más reciente primero),
--            con la descripción de las piezas instalada y retirada y
--            el estado de custodia.
-- Requiere: CREATE_Equipo_Cambio_Pieza.sql
-- Autor: MVSystem
-- Fecha: 2026-10-27
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Equipo_Cambio_Pieza_Historial
    @Imei VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.ID_Cambio,
           C.Imei,
           C.Tipo_Pieza,
           C.ID_Accesorio_Instalado,
           Pieza_Instalada = AI.Descripcion_Accesorio,
           C.Costo_Instalado,
           C.ID_Accesorio_Retirado,
           Pieza_Retirada = AR.Descripcion_Accesorio,
           C.Costo_Retirado,
           C.Config_Anterior,
           C.Config_Nueva,
           C.Costo_Equipo_Anterior,
           C.Costo_Equipo_Nuevo,
           C.ID_Almacen,
           C.ID_PreFactura,
           C.ID_Reparacion,
           C.Tecnico,
           C.Estado_Custodia,
           C.Usuario_Registro,
           C.Fecha_Registro,
           C.Usuario_Recepcion,
           C.Fecha_Recepcion,
           C.Nota_Recepcion
    FROM dbo.Equipo_Cambio_Pieza C
    LEFT JOIN dbo.Accesorios AI ON AI.ID_Accesorio = C.ID_Accesorio_Instalado
    LEFT JOIN dbo.Accesorios AR ON AR.ID_Accesorio = C.ID_Accesorio_Retirado
    WHERE C.Imei = @Imei
    ORDER BY C.Fecha_Registro DESC, C.ID_Cambio DESC;
END
GO
