using System;

namespace MVSystemApi.Model
{
    /// <summary>Datos del ticket que se imprime cuando una pieza retirada queda en custodia del técnico.</summary>
    public class ComprobanteCustodiaDTO
    {
        public int IdCambio { get; set; }
        public DateTime FechaRegistro { get; set; }
        /// <summary>Fecha y hora límite para entregar la pieza (fecha del cambio + Laptops:HorasMaxCustodia).</summary>
        public DateTime FechaLimite { get; set; }
        public int HorasMax { get; set; }
        public string Imei { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string TipoPieza { get; set; }
        public string PiezaRetirada { get; set; }
        public string ModeloRetirada { get; set; }
        public string PiezaInstalada { get; set; }
        public string ModeloInstalada { get; set; }
        public string ConfigAnterior { get; set; }
        public string ConfigNueva { get; set; }
        public decimal CostoRetirado { get; set; }
        public string Tecnico { get; set; }
        public string TecnicoNombre { get; set; }
        public string UsuarioRegistro { get; set; }
        public string EstadoCustodia { get; set; }
        public string Almacen { get; set; }
    }
}
