namespace MVSystemApi.Model
{
    public class ReparacionReporteDTO
    {
        public Int64 IdReparacion { get; set; }
        public string Imei { get; set; }
        public string NombreCliente { get; set; }
        public string ApellidoCliente { get; set; }
        public string Detalle_Reparacion { get; set; }
        public string Observaciones { get; set; }
        public string descripcion { get; set; }
        public decimal Total { get; set; }
        public decimal Precio { get; set; }
        public DateTime Fecha_Entrega { get; set; }
        public string Usuario { get; set; }
        public string Modelo { get; set; }
        public string Logo { get; set; }

        // Cancelación (se llena solo cuando la reparación fue cancelada)
        public bool Cancelada { get; set; }
        public string MotivoCancelacion { get; set; }
        public string UsuarioCancelacion { get; set; }
        public DateTime? FechaCancelacion { get; set; }
    }
}