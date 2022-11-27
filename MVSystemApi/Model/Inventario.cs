using System.ComponentModel.DataAnnotations;

namespace MVSystemApi.Model
{
    public class ArticuloInventario
    {
        public int IdInventario { get; set; }
        public int IdEquipo { get; set; }
        public string Imei { get; set; }
        public char Escaneado { get; set; }
        public string Descripcion { get; set; }
        public string Usuario { get; set; }
        public string Fecha { get; set; }
    } 
    public class ArticuloInventarioUpdate
    {
        public int IdInventario { get; set; }
        [MaxLength(30)]
        public string Imei { get; set; }
    }
}
