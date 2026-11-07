using System;

namespace MVSystemApi.Model
{
    /// <summary>Anulación de un cambio de pieza (revierte stock, costo y configuración). El motivo es obligatorio.</summary>
    public class EquipoCambioAnulacion
    {
        public int ID_Cambio { get; set; }
        public string Motivo { get; set; }
        public string Usuario { get; set; }
    }
}
