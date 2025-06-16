namespace MVSystemApi.Model
{
    public class ReparacionReporteFiltro
    {
        public int? IdReparacion { get; set; }
        public int? IdCliente { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}
