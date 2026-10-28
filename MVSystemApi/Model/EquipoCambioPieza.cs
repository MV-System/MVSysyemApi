using System;

namespace MVSystemApi.Model
{
    public class EquipoCambioPieza
    {
        public string Imei { get; set; }
        public string Tipo_Pieza { get; set; }              // RAM | ALMACENAMIENTO
        public long ID_Accesorio_Instalado { get; set; }
        public int Nueva_Capacidad_GB { get; set; }
        public string Nuevo_Tipo { get; set; }
        public long? ID_Accesorio_Retirado { get; set; }
        public string Destino_Retirada { get; set; }        // CUSTODIA | CLIENTE
        public string Tecnico { get; set; }
        public int ID_Almacen { get; set; }
        public int? ID_PreFactura { get; set; }
        public int? ID_Reparacion { get; set; }
        // Obligatorios cuando el costo nuevo supera el precio de venta actual
        public decimal? Nuevo_Precio_Detalle { get; set; }
        public decimal? Nuevo_Precio_Por_Mayor { get; set; }
        public string Usuario { get; set; }
    }
}
