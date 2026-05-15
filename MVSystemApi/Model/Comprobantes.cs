using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Comprobantes
    {
        public int NFCTipoNumero { get; set; }
        public string NFCTipoDescripcion { get; set; }
        public bool Visible_Venta { get; set; }
        public long? NFCTipoCantidad { get; set; }
        public long? NFCTipoSecuencia { get; set; }
    }
}
