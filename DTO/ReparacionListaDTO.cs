namespace DTO
{
    public class ReparacionListaDTO
    {
        public int IdReparacion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string NombreCliente { get; set; }
        public string TelefonoCliente { get; set; }
        public string Detalle_Reparacion { get; set; }
        public string Observaciones { get; set; }
        public string Estatus { get; set; }
        public decimal Precio { get; set; }
        public decimal Costo { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string Usuario { get; set; }

        // Propiedades de paginaci�n
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Line { get; set; }
        public int LastLine { get; set; }
        public int TotalRecords { get; set; }
    }
}