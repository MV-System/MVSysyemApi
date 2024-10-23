using DTO;

namespace MVSystemApi.Model
{
    public class EmpresaReportes
    {
        public DatosEmpresa DatosEmpresaFactura { get; set; }
        public List<EquipoVendido> EquipoVendido { get; set; }        
        public List<EquipoDisponibleDTO> EquiposDTO { get; set; }
        public List<EquipoTransferencia> EquipoTransferencia { get; set; }
        
    }
}