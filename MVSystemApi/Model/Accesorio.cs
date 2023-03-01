using System;

namespace MVSystemApi.Model
{
    public class Accesorio
    {
        public Int64 ID_Accesorio { get; set; }
        public string Imei_Equipo { get; set; }
        public int ID_Modelo { get; set; }
        public string Modelo { get; set; }
        public string Marca { get; set; }
        public string codigo { get; set; }
        public string Descripcion_Accesorio { get; set; }
        public int ID_Almacen { get; set; }
        public Int64? Cantidad { get; set; }
        public Int64? Can_Max { get; set; }
        public Int64? Can_Min { get; set; }
        public decimal Precio_Por_Mayor { get; set; }
        public decimal Precio_Detalle { get; set; }
        public decimal Costo_Equipo { get; set; }
        public string Disponible { get; set; }
        public string Disponible_Detalle { get; set; }
        public Int64 Cod_Barra { get; set; }
        public string Usuario { get; set; }
    }
    public class AccesorioModelId
    {
        public Int64 ID { get; set; }
        public string Codigo { get; set; }
        public string DescripcionAccesorio { get; set; }
        public Int64? Cantidad { get; set; }
        public decimal PrecioPorMayor { get; set; }
        public decimal PrecioDetalle { get; set; }
        public decimal CostoEquipo { get; set; }
        public string Estado { get; set; }

    }
    public class Accesorio_Respuesta
    {
        public string Descripcion_Accesorio { get; set; }
        public decimal Precio_Por_Mayor { get; set; }
        public decimal Precio_Detalle { get; set; }
        public Int64? Cantidad { get; set; }
    }


    public class AccesorioInventarioResponse : PaginationResponse
    {
        public string Codigo { get; set; }
        public string Modelo { get; set; }
        public string PrecioDetalle { get; set; }
        public string PrecioMayor { get; set; }
        public string Costo { get; set; }
        public string Existencia { get; set; }
        public string Almacen { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string FormatedDate => DateOnly.FromDateTime(FechaRegistro).ToShortDateString();
    }
}
