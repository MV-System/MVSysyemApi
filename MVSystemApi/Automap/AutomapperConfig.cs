using AutoMapper;
using DTO;
using MVSystemApi.Model;

namespace MVSystemApi.Automap
{
    public class AutomapperConfig: Profile
    {
        public AutomapperConfig()
        {
            EquipoDisponible();
        }

        private void EquipoDisponible()
        {
            CreateMap<Equipos, EquipoDisponibleDTO>().ReverseMap()
            .ForMember(x => x.ID_Tecnologia, opt => opt.Ignore())
            .ForMember(x => x.ID_Condicion, opt => opt.Ignore())
            .ForMember(x => x.ID_Garantia, opt => opt.Ignore())
            .ForMember(x => x.ID_Estado_Bloqueo, opt => opt.Ignore())
            .ForMember(x => x.Numero_de_Factura, opt => opt.Ignore())
            .ForMember(x => x.Usuario, opt => opt.Ignore());
        }
    }
}
