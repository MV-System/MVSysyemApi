namespace MVSystemApi.Model
{
    /// <summary>
    /// Datos para cancelar una cotización (no facturada). No requiere motivo.
    /// </summary>
    public class CotizacionCancelar
    {
        public int Codigo { get; set; }
        public string Usuario { get; set; }
    }
}
