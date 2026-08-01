using System;
using System.Collections.Generic;

namespace DTO
{
    public class DetalleAbonoFacturaDTO
    {
        public string NumeroFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal TotalAbono { get; set; }
        public decimal Pendiente { get; set; }
        public bool Saldada { get; set; }
        public string TipoFactura { get; set; }
    }

    public class ReciboAbonoGeneralDTO
    {
        public string NombreCliente { get; set; }
        public string ApellidoCliente { get; set; }
        public string TipoPago { get; set; }
        public string Nota { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public decimal TotalAbonado { get; set; }
        public string Logo { get; set; }
        public List<DetalleAbonoFacturaDTO> Facturas { get; set; } = new List<DetalleAbonoFacturaDTO>();
    }
}
