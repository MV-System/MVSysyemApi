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
            Accesorios();
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
        public void Accesorios()
        {
            CreateMap<Accesorio, AccesorioDTO>().ReverseMap()
                .ForMember(x => x.Can_Min, opt => opt.Ignore())
                .ForMember(x => x.Can_Max, opt => opt.Ignore())
                .ForMember(x => x.Marca, opt => opt.Ignore())
                .ForMember(x => x.Imei_Equipo, opt => opt.Ignore())
                .ForMember(x => x.ID_Modelo, opt => opt.Ignore())
                .ForMember(x => x.ID_Almacen, opt => opt.Ignore())
                .ForMember(x => x.Usuario, opt => opt.Ignore());
        }
    }
}
