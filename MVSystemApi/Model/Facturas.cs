using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class FacturaCliente
    {
        public Facturas Factura { get; set; }
        public List<FacturaReporte> FacturaReporte { get; set; }
        public Clientes_consulta Cliente { get; set; }
        public string VendedorNombre { get; set; }
        public DatosEmpresa DatosEmpresaFactura { get; internal set; }
        public string QrUrl { get; set; }
    }

    public class FacturaReporte
    {

        public string PRODUCTO { get; set; }
        public int CANTIDAD_PROD { get; set; }
        public decimal PRECIO_PROD { get; set; }
        public decimal SUBTOTAL_PROD { get; set; }
        public decimal DESCUENTO_PROD { get; set; }
        public decimal TOTAL_PROD { get; set; }
    }
    public class FacturaFilter : Paginate
    {
        public int NumeroFactura { get; set; }
        public string Criterio { get; set; }
        public int Almacen { get; set; }
        public string TipoPago { get; set; }
        public string empresa { get; set; }
        public string IdEmpresa { get; set; }
        public string IdSucursal { get; set; }
        public string EstadoFactura { get; set; }
        public int ComprobanteCheck { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoPago { get; set; }
        public string Usuario { get; set; }
        public string FechaInicio { get; set; }
        public string FechaFinal { get; set; }

    }
    public class FacturaConsulta : PagingDTO
    {
        public string logo { get; set; }

        public int NumeroFactura { get; set; }
        public int ClienteId { get; set; }
        public long Telefono { get; set; }
        public string Ncf { get; set; }
        public int NcfTipoNumero { get; set; }
        public string TipoComprobante { get; set; }
        public string Rnc { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Vendedor { get; set; }
        public string TipoFactura { get; set; }
        public string Almacen { get; set; }
        public int IdSucursal { get; set; }
        public string Nota { get; set; }
        // Datos de la anulación (solo vienen cuando la factura está anulada)
        public string MotivoAnulacion { get; set; }
        public string UsuarioAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        // true cuando la factura que se reimprime está anulada (las vistas muestran el aviso)
        public bool Anulada { get; set; }
        // Garantía elegida al crear la factura (descripción), para el encabezado impreso
        public string Garantia { get; set; }
        public string TipoPago { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal TotalSinItbis { get; set; }
        public DateTime FechaFacturacion { get; set; }
        public string empresa { get; set; }

        // Valores por defecto para factura normal
        public string FacturaTitulo { get; set; } = "Factura No.";
        public string FacturaTituloTipo { get; set; } = "Tipo de factura";
        public string VerNCF { get; set; } = "NCF:";

        public decimal Abono { get; internal set; }
        public DatosEmpresa DatosEmpresaFactura { get; internal set; }

    }
    public class Facturas
    {
        public string logo { get; set; }
        public int NumeroFactura { get; set; }
        public int IdCliente { get; set; }
        public int IdTipoFactura { get; set; }
        public string TipoFactura { get; set; }
        public int IdVendedor { get; set; }
        public int IdGarantia { get; set; }
        public int IdTipoPago { get; set; }
        public string TipoPago { get; set; }
        public int NcfTipoNumero { get; set; }
        public int Telefono { get; set; }
        public int? IdAlmacen { get; set; }
        public string Almacen { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public DateTime fechaLimitePago { get; set; }
        public decimal Total { get; set; }
        public int CantidadArticulos { get; set; }
        public string Nota { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int Abono { get; set; }
        public string RNC { get; set; }
        public string RncEmpresa { get; set; }
        public string NcfNumero { get; set; }
        public List<DetalleFactura> DetalleFacturaList { get; set; }

        public Clientes Cliente { get; set; }
        public FacturaConsulta Factura { get; internal set; }
        public string empresa { get; set; }
        public string FacturaTitulo { get; set; }
        public string IdEmpresa { get; set; }
        public string IdSucursal { get; set; }
        public DatosEmpresa DatosEmpresaFactura { get; internal set; }
        public string QrUrl { get; set; }
        public string FechaFirma { get; set; }
        public string CodigoSeguridad { get; set; }
    }

    public class DetalleFactura
    {
        public int NumeroFactura { get; set; }
        public string IdEquipo { get; set; }
        public int IdTipo { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoFactura { get; set; }
        public int IdGarantia { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Descripcion { get; set; }
    }
    public class FacturaAnulacionInfo
    {
        public string Motivo { get; set; }
        public string Usuario { get; set; }
        public DateTime? Fecha { get; set; }
    }
    public class FilterAnulaFactura
    {
        public int NumeroFactura { get; set; }
        public string Usuario { get; set; }
        public string IDsucursal { get; set; }
        // Nota de la anulación. Obligatoria en la anulación manual (se valida en el controlador).
        public string Motivo { get; set; }
    }
    public class NotaCreditoInfo
    {
        public string DocReferencia { get; set; }
        public DateTime FechaDocReferencia { get; set; }
        public string EncfReferencia { get; set; }
        public int CodModificacion { get; set; } = 1;
    }
    public class NotaCreditoProveedor2Request
    {
        public int NumeroFacturaOrigen { get; set; }
        public string IDsucursal { get; set; }
        public string Usuario { get; set; }
        public string Motivo { get; set; }
    }
    public class NotaCreditoDetalleReporte
    {
        public string CodProducto { get; set; }
        public string Descripcion { get; set; }
        public string UndMedida { get; set; }
        public string TipoProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
    }
    public class NotaCreditoReporteViewModel
    {
        public string Encf { get; set; }
        public string EncfReferencia { get; set; }
        public string Condicion { get; set; }
        public DateTime FechaEmision { get; set; }
        public string NombreCliente { get; set; }
        public string RncCliente { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public List<NotaCreditoDetalleReporte> Detalle { get; set; } = new List<NotaCreditoDetalleReporte>();
        public DatosEmpresa DatosEmpresaFactura { get; set; }
        public string QrUrl { get; set; }
        public string CodigoSeguridad { get; set; }
        public string FechaFirma { get; set; }
    }
    public class EditaDetalleFactura
    {
        public int NumeroFactura { get; set; }
        public string EquipoActual { get; set; }
        public string EquipoNuevo { get; set; }
        public string IDsucursal { get; set; }
        public string Usuario { get; set; }
        public string mensaje { get; set; }
    }
    public class FacturaEditada
    {
        public string mensaje { get; set; }
    }
    public class RebajaEquipoModel
    {
        public int NumeroFactura { get; set; }
        public string imei { get; set; }
        public string IDsucursal { get; set; }
        public string Usuario { get; set; }
        public string mensaje { get; set; }
    }
}
