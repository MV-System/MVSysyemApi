using System;
using System.Collections.Generic;

namespace DTO
{
    public class FacturaRegistroDTO
    {
        public int NumeroFactura { get; set; }
        public int IdCliente { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoFactura { get; set; }
        public int IdTipoPago { get; set; }
        public int NcfTipoNumero { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaLimitePago { get; set; }
        public string Garantia { get; set; }
        public int IdAlmacen { get; set; }
        public string Nota { get; set; }
        public string Empresa { get; set; }
        public string RNC { get; set; }
        public string Descripcion { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public decimal Abono { get; set; }
        public decimal Descuento { get; set; }
        public int CantidadArticulos { get; set; }
        public string Usuario { get; set; }
        public string IdEmpresa { get; set; }
        public string IdSucursal { get; set; }

        // Nuevo: Detalles de pagos para pago mixto
        public List<DetallePagoDTO> DetallesPagos { get; set; } = new List<DetallePagoDTO>();

        // Relaciones
        public List<DetalleFacturaDTO> DetalleFacturaList { get; set; } = new List<DetalleFacturaDTO>();
    }

    public class DetalleFacturaDTO
    {
        public int NumeroFactura { get; set; }
        public string IdEquipo { get; set; }
        public int IdTipo { get; set; }
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
}
