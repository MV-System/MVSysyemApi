using System;
using System.Collections.Generic;

namespace MVSystemApi.Model
{
    public class PreFactura
    {
        public int Numero_PreFactura { get; set; }
        public int ID_Cliente { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public long Numero_Telefono { get; set; }
        public int ID_Tipo_Factura { get; set; }
        public string Tipo_Factura { get; set; }
        public int? ID_Vendedor { get; set; }
        public string Vendedor_Nombres { get; set; }
        public int? ID_Tipo_Pagos { get; set; }
        public string Tipo_Pago { get; set; }
        public int ID_Sucursal { get; set; }
        public string Sucursal { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? Itbis { get; set; }
        public decimal? Abono { get; set; }
        public DateTime? Tiempo_Credito { get; set; }
        public decimal Total { get; set; }
        public int Cantidad_Articulos { get; set; }
        public string Nota { get; set; }
        public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime? Fecha_Registro { get; set; }
        public DateTime? Fecha_Procesamiento { get; set; }
        public string Usuario_Procesamiento { get; set; }
        public string NFCTipoNumero { get; set; }
        public string NFCFISCAL { get; set; }
        public string NCF_NUMERO { get; set; }
        public string RNC { get; set; }
        public string Literal { get; set; }
        public int Cotizacion_Numero { get; set; }
        public List<PreDetalleFactura> DetalleFacturaList { get; set; }
    }

    public class PreDetalleFactura
    {
        public int Numero_PreFactura { get; set; }
        public string ID_Equipo { get; set; }
        public string Descripcion { get; set; }
        public int ID_Tipo { get; set; }
        public int ID_Garantia { get; set; }
        public int? cantidad { get; set; }
        public string LiteralF { get; set; }
        public decimal? precio { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? Itbis { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime Fecha_registro { get; set; }
        public decimal? Costo_Equipo { get; set; }
    }

    public class PreFacturaFiltro
    {
        public string Estado { get; set; }
        public string Criterio { get; set; }
        public int? ID_Sucursal { get; set; }
        public int? ID_Cliente { get; set; }
    }
}
