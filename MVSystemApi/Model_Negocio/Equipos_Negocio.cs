using MVSystemApi.Interfaz;
using System;
using System.Data;
using System.Linq;

namespace MVSystemApi.Model
{
    public class Equipos_Negocio
    {
        private readonly IAccesoDatos Ac;

        public Equipos_Negocio(IAccesoDatos Ad)
        {
            Ac = Ad;
        }
        internal object Equipo_Insert(Equipos Equipo)
        {
            var result = (from dt in Ac.Equipo_Insert(Equipo).AsEnumerable()
                          select new Equipo_return
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              Marca = Convert.ToString(dt["Marca"]),
                              Mensaje = Convert.ToString(dt["Mensaje"])

                          }).ToList();
                 

            return result;
        }
        internal object Equipo_Busca_Disponible(string Equipo, int Id_Almacen)
        {
            var result = (from dt in Ac.Equipo_Busca_Disponible(Equipo,Id_Almacen).AsEnumerable()
                          select new Equipo_return
                          {
                              Imei = Convert.ToString(dt["Equipo"]),
                              Marca = Convert.ToString(dt["Marca"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              precioPorMayor = Convert.ToDecimal(dt["precioPorMayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["PrecioDetalle"]),
                              Disponible_Detalle = Convert.ToString(dt["Nota_Adicional"]),
                              Mensaje = Convert.ToString(dt["Mensaje"])

                          }).ToList();


            return result;
        }
        internal object Numero_Registro()
        {
            var result = (from dt in Ac.Equipo_Consulta_Ultimo_Registro().AsEnumerable()
                          select new Numero_registro
                          {
                              Numero_Registro = Convert.ToInt32(dt["ID"])+1,

                          }).ToList();

            return result;
        }
    }
}
