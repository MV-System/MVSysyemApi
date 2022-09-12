using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Equipos
    {
        public string Imei { get; set; }
        public int ID_Modelo { get; set; }
        public int ID_Suplidor { get; set; }
        public int ID_Tecnologia { get; set; }
        public int ID_Garantia { get; set; }
        public int ID_Condicion { get; set; }
        public int ID_Estado_Bloqueo { get; set; }
        public int ID_Almacen { get; set; }
        public string Numero_de_Factura { get; set; }
        public int Cantidad { get; set; }
        public int Can_Max { get; set; }
        public int Can_Min { get; set; }
        public decimal Precio_Por_Mayor { get; set; }
        public decimal Precio_Detalle { get; set; }
        public decimal Costo_Equipo { get; set; }
        public decimal Comision_Detalle { get; set; }
        public decimal Comision_Por_Mayor { get; set; }
        public string Desbloqueado { get; set; }
        public string Disponible { get; set; }
        public string Nota_Adicional { get; set; }
        
        public int? Numero_Registro { get; set; }
        public string Usuario { get; set; }

    }
   
    public class Equipo_return
    {
        public string Imei { get; internal set; }
        public string Modelo { get; internal set; }
        public string Marca { get; internal set; }
        public decimal precioPorMayor { get; set; }
        public decimal PrecioDetalle { get; set; }
        public string Disponible_Detalle { get; set; }
        public string Mensaje { get;  set; }
    }

    public class Numero_registro
    {
        public int Numero_Registro { get; internal set; }

    }
    //public class EquipoFilter
    //{
    //    public int Almacen{ get; set; }
    //    public string Modelo { get; set; }
    //    public string Suplidor{ get; set; }
    //    public string Vendedor{ get; set; }
    //    public string FechaRegistro{ get; set; }
    //    public int PageIndex{ get; set; }
    //    public int PageSize{ get; set; }
    //    public string FechaInicio{ get; set; }
    //    public string FechaFinal{ get; set; }
    //}
    public class EquipoVendidoReporte
    {
        public List<EquipoVendido> EquipoVendidos { get; set; }
        public string FechaImpresion { get; set; }
        public EquipoVendidoFilter EquipoFilter { get; set; }
    }
    public class EquipoVendido : PagingDTO
    { 

        public string Imei { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioPorMayor { get; set; }
        public decimal PrecioVendido { get; set; }
        public decimal PrecioDetalle { get; set; }
        public decimal CostoEquipo { get; set; }
        public decimal ComisionDetalle { get; set; }
        public decimal ComisionPorMayor { get; set; }
        public string TipoFactura { get; set; }
        public string IsDesbloqueado { get; set; }
        public string NotaAdicional { get; set; }
        public string DescripcionAlmacen { get; set; }
        public string Suplidor { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        //public int IdSuplidor { get; set; }
        //public int IdAlmacen { get; set; }
        public string FechaRegistro { get; set; }
        public string FechaFacturado { get; set; }
        public decimal TotalInventario { get; set; }
        public decimal TotalFacturado{ get; set; }
        public decimal TotalGanancia { get; set; }


    }
    public class EquipoVendidoFilter
    {
        [Required]
        public int PageIndex { get; set; }
        [Required]
        public int PageSize { get; set; }
        public int? Almacen { get; set; }
        public string? Suplidor { get; set; }
        public string? Vendedor { get; set; }
        public string? FechaRegistro { get; set; }
        public string? Modelo { get; set; }
        public string? FechaInicio { get; set; }
        public string? FechaFinal { get; set; }
    }
}
