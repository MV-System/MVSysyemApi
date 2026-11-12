namespace MVSystemApi.Model
{
    /// <summary>Registro del listado de RNC de la DGII (tabla dbo.RNC_DGII en SEGURIDAD).</summary>
    public class RncDgii
    {
        public string Rnc { get; set; }
        public string RazonSocial { get; set; }
        public string NombreComercial { get; set; }
        public string Estado { get; set; }
        /// <summary>Solo un RNC ACTIVO puede usarse para facturar con comprobante.</summary>
        public bool Activo => string.Equals(Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase);
    }
}
