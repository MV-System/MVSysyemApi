using System;

namespace MVSystemApi.Model
{
    public class DetallesPagos_Factura
    {
        public int ID_Detalle_Pago { get; set; }
        public int Numero_Factura { get; set; }
        public int ID_Tipo_Pago { get; set; }
        public decimal Monto_Pago { get; set; }
        public string Numero_Referencia { get; set; }
        public string Observacion { get; set; }
        public char Estado { get; set; }
        public DateTime Fecha_Registro { get; set; }

        // Relaciones
        public Tipos_Pagos TipoPago { get; set; }
    }
}
