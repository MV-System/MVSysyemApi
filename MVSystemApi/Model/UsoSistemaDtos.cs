namespace MVSystemApi.Model
{
    /// <summary>Evento de uso: una pantalla abierta (P) o un boton pulsado (B)</summary>
    public class UsoEventoDto
    {
        public string Tipo { get; set; }
        public string Ruta { get; set; }
        public string Detalle { get; set; }
    }

    /// <summary>Lo que el front envia en cada aviso: pantalla actual, eventos pendientes y si es solo un latido</summary>
    public class UsoRegistroDto
    {
        public Guid SesionId { get; set; }
        public string Ruta { get; set; }
        public List<UsoEventoDto> Eventos { get; set; } = new List<UsoEventoDto>();
    }

    public class UsoSalidaDto
    {
        public Guid SesionId { get; set; }
    }

    public class UsoConectadoDto
    {
        public string Usuario { get; set; }
        public string NombreUsuario { get; set; }
        public string Empresa { get; set; }
        public string Ruta { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime UltimaActividad { get; set; }
        public string DireccionIP { get; set; }
    }

    public class UsoDetalleUsuarioDto
    {
        public string Usuario { get; set; }
        public string NombreUsuario { get; set; }
        public string Empresa { get; set; }
        public int Veces { get; set; }
        public DateTime Ultima { get; set; }
    }

    public class UsoResumenDto
    {
        public string Tipo { get; set; }
        public string Ruta { get; set; }
        public string Detalle { get; set; }
        public int Visitas { get; set; }
        public int Usuarios { get; set; }
        public DateTime Ultima { get; set; }
    }
}
