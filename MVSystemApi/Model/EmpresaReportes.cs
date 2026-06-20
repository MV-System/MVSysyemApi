using DTO;

namespace MVSystemApi.Model
{
    public class EmpresaReportes
    {
        public DatosEmpresa DatosEmpresaFactura { get; set; }
        public List<EquipoVendido> EquipoVendido { get; set; }        
        public List<EquipoDisponibleDTO> EquiposDTO { get; set; }
        public List<EquipoTransferencia> EquipoTransferencia { get; set; }
        public List<ReparacionReporteDTO> Reparaciones { get; set; }
        public List<EquipoRegistrado> EquiposRegistrados { get; set; }
        public List<ReciboAbonoDTO> ReciboAbono { get; set; }

    }
}