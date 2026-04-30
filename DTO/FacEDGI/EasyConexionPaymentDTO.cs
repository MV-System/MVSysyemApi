namespace DTO.FacEDGI
{
    /// <summary>
    /// DTO para cada método de pago enviado a DGI
    /// </summary>
    public class EasyConexionPaymentDTO
    {
        /// <summary>
        /// Código del método de pago (según catálogo DGI)
        /// Origen: Tipo_Pagos.Codigo_DGI (convertido de string a int)
        /// Valores válidos: 1, 2, 3, 4, 5, 6, 7, 8
        /// - 1 = Efectivo
        /// - 2 = Cheque / Transferencia / Depósito
        /// - 3 = Tarjeta Crédito / Débito
        /// - 4 = Venta a Crédito
        /// - 5 = Bonos o Certificados de Regalo
        /// - 6 = Permuta
        /// - 7 = Nota de Crédito
        /// - 8 = Mixto
        /// </summary>
        public int Method { get; set; }

        /// <summary>
        /// Monto pagado por este método
        /// Origen: DetallesPagos_Factura.Monto_Pago o Facturas.Total si es pago único
        /// </summary>
        public decimal Amount { get; set; }
    }
}
