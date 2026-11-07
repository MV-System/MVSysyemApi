using System;
using System.Collections.Generic;

namespace MVSystemApi.Model
{
    public class EquipoConfiguracion
    {
        public int? ID_Configuracion { get; set; }
        public string Imei { get; set; }
        public int? Ram_GB { get; set; }
        public string Ram_Tipo { get; set; }
        public int? Almacenamiento_GB { get; set; }
        public string Almacenamiento_Tipo { get; set; }
        public string Notas { get; set; }
        public string Usuario { get; set; }
        public string Configuracion { get; set; }
        public string Mensaje { get; set; }
        /// <summary>Memorias y discos de la laptop. Si viene, tiene prioridad sobre Ram_GB / Almacenamiento_GB.</summary>
        public List<EquipoModuloConfiguracion> Modulos { get; set; }
    }

    /// <summary>Una memoria o un disco de la laptop (modelo de componente + capacidad).</summary>
    public class EquipoModuloConfiguracion
    {
        public int? IdModulo { get; set; }
        public string TipoPieza { get; set; }
        public int CapacidadGB { get; set; }
        public int IdModeloComponente { get; set; }
    }

    /// <summary>Corrección manual de un módulo (modelo y/o capacidad), con motivo.</summary>
    public class EquipoModuloCorreccion
    {
        public int ID_Modulo { get; set; }
        public int ID_Modelo_Componente { get; set; }
        public int Capacidad_GB { get; set; }
        public string Motivo { get; set; }
        public string Usuario { get; set; }
    }
}
