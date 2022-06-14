using MVSystemApi.Interfaz;
using System;
using System.Linq;
using System.Data;

namespace MVSystemApi.Model
{
    public class Proveedores_Negocio
    {
        private IAccesoDatos Ac;

        public Proveedores_Negocio(IAccesoDatos _ad)
        {
            Ac = _ad;
        }
        internal object Suplidor_Insert(Suplidor Suplidor)
        {
            try
            {
                var result = (from dt in Ac.Suplidor_Insert(Suplidor).AsEnumerable()
                              select new Suplidor
                              {
                                  ID_Suplidor = Convert.ToInt32(dt["ID_Suplidor"]),
                                  Nombres = Convert.ToString(dt["Nombres"]),
                                  Apellidos = Convert.ToString(dt["Apellidos"]),
                                  Numero_Telefono = Convert.ToInt64(dt["Numero_Telefono"]),
                                  Email_Detalle = Convert.ToString(dt["Email_Detalle"]),
                                  Direccion_Detalle = Convert.ToString(dt["Direccion_Detalle"]),
                                  Mensaje = Convert.ToString(dt["Mensaje"])

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
