using System;

namespace DTO
{
    public class DetallePagoDTO
    {
        public int ID_Tipo_Pago { get; set; }
        public string Descripcion_Tipo_Pago { get; set; }
        public decimal Monto_Pago { get; set; }
        public string Numero_Referencia { get; set; }
        public string Observacion { get; set; }
    }
}
