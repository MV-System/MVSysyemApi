using System;

namespace MVSystemApi.Model
{
    /// <summary>
    /// Acción sobre una pieza retirada en custodia (recibir / entregar al cliente).
    /// </summary>
    public class EquipoCustodiaAccion
    {
        public int ID_Cambio { get; set; }
        public string Usuario { get; set; }
        public string Nota { get; set; }
        // Solo para "entregar al cliente": obligatorios si el costo resultante supera el precio de venta
        public decimal? Nuevo_Precio_Detalle { get; set; }
        public decimal? Nuevo_Precio_Por_Mayor { get; set; }
    }
}
