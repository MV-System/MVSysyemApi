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
        public string TipoFactura { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Logo { get; set; }
    }
}
