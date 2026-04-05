namespace DTO.FacEDGI
{
    /// <summary>
    /// DTO para cada método de pago enviado a DGI
    /// </summary>
    public class EasyConexionPaymentDTO
    {
        /// <summary>
        /// Código del método de pago
        /// Origen: DetallesPagos_Factura.ID_Tipo_Pago
        /// Valor: 1=Efectivo, 2=Tarjeta, 3=Cheque, etc.
        /// </summary>
        public int Method { get; set; }

        /// <summary>
        /// Monto pagado por este método
        /// Origen: DetallesPagos_Factura.Monto_Pago o Facturas.Total si es pago único
        /// </summary>
        public decimal Amount { get; set; }
    }
}
