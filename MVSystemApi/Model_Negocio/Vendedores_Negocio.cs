using MVSystemApi.Interfaz;
using System;
using System.Linq;
using System.Data;

namespace MVSystemApi.Model
{
    public class Vendedores_Negocio
    {
        private readonly IAccesoDatos Ac;

        public Vendedores_Negocio(IAccesoDatos ad)
        {
            Ac = ad;
        }

        internal object Vendedor_Insert(Vendedor Vendedor)
        {
            try
            {
                var result = (from dt in Ac.Vendedor_Insert(Vendedor).AsEnumerable()
                              select new Vendedor
                              {
                                  ID_Vendedor = Convert.ToInt32(dt["ID_Vendedor"]),
                                  Nombres = Convert.ToString(dt["Nombres"]),
                                  Apellidos = Convert.ToString(dt["Apellidos"]),
                                  Numero_Telefono = Convert.ToInt64(dt["Numero_Telefono"]),
                                  Email_Detalle = Convert.ToString(dt["Email_Detalle"]),
                                  Direccion_Detalle = Convert.ToString(dt["Direccion_Detalle"]),
                                  Mensaje = Convert.ToString(dt["Mensaje"]),
                              }).ToList().FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
