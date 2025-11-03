namespace MVSystemApi.Model
{
    public class ReparacionConsultaFiltro
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int ID_Reparacion { get; set; } = 0;
        public string Criterio { get; set; } = "";
        public int Estatus { get; set; } = 0;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }

    }
}
