namespace DTO
{
    public class ReciboAbonoDTO
    {
        public int Id { get; set; }
        public string NombreCliente { get; set; }
        public string ApellidoCliente { get; set; }
        public string NumeroFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal TotalAbono { get; set; }
        public decimal Pendiente { get; set; }

        // Calculado: lo que se debia de esta factura ANTES de aplicar
        // este abono. Se usa para que el cliente pueda verificar el
        // calculo cuando el balance pendiente no coincide con
        // Total Factura - Monto Abonado (por ejemplo, si ya existian
        // abonos previos aplicados a la misma factura).
        public decimal PendienteAntes => Pendiente + TotalAbono;
        public string TipoFactura { get; set; }
        public string TipoPago { get; set; }
        public string Nota { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Logo { get; set; }
    }
}
