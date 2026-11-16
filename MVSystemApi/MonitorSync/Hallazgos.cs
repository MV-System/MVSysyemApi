namespace MVSystemApi.MonitorSync
{
    /// <summary>Que revision detecta el hallazgo. La completa (nocturna) incluye ademas costos y precios.</summary>
    public enum AlcanceRevision
    {
        Liviana = 0,
        Completa = 1,
    }

    /// <summary>Una diferencia entre la base principal y su espejo.</summary>
    public class Hallazgo
    {
        /// <summary>Identificador estable (empresa|tipo|id): sirve para saber si la misma diferencia sigue ahi.</summary>
        public string Clave { get; set; }
        public string Empresa { get; set; }
        /// <summary>Tipo corto, por ejemplo DISPONIBLE_DISTINTO, FALTA_EN_ESPEJO, MODELO_OTRO_ID, COSTO_DISTINTO.</summary>
        public string Categoria { get; set; }
        public string Detalle { get; set; }
        public AlcanceRevision Alcance { get; set; }
    }

    /// <summary>Resultado de una pasada de revision.</summary>
    public class ResultadoRevision
    {
        public AlcanceRevision Alcance { get; set; }
        public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
        public List<Hallazgo> Hallazgos { get; } = new List<Hallazgo>();
        /// <summary>Lineas de resumen por empresa (cantidades disponibles de cada base).</summary>
        public List<string> Resumen { get; } = new List<string>();
        /// <summary>Cantidades por empresa (para la tabla del correo).</summary>
        public List<ResumenEmpresa> Empresas { get; } = new List<ResumenEmpresa>();

        public ResumenEmpresa EmpresaResumen(string nombre)
        {
            var r = Empresas.FirstOrDefault(e => e.Nombre == nombre);
            if (r == null) { r = new ResumenEmpresa { Nombre = nombre }; Empresas.Add(r); }
            return r;
        }
        /// <summary>Empresas que no se pudieron revisar (conexion, permisos...).</summary>
        public List<string> Errores { get; } = new List<string>();
        /// <summary>Nombres de las empresas con error: sus hallazgos anteriores no se dan por resueltos.</summary>
        public HashSet<string> EmpresasConError { get; } = new HashSet<string>();
    }

    /// <summary>Cantidades de una empresa en la principal y en el espejo.</summary>
    public class ResumenEmpresa
    {
        public string Nombre { get; set; }
        public int EquiposPrincipal { get; set; }
        public int EquiposEspejo { get; set; }
        public int AccesoriosPrincipal { get; set; }
        public int AccesoriosEspejo { get; set; }
        public long ExistenciaPrincipal { get; set; }
        public long ExistenciaEspejo { get; set; }
    }
}
