namespace MVSystemApi.Model
{
    /// <summary>
    /// Datos para cancelar una reparación abierta (Pendiente / En Proceso).
    /// </summary>
    public class ReparacionCancelar
    {
        public long ID_Reparacion { get; set; }
        public string Motivo { get; set; }
        public string Usuario { get; set; }
    }
}
