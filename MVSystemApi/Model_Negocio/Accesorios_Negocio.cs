using MVSystemApi.Interfaz;
using System;
using System.Linq;
using System.Data;

namespace MVSystemApi.Model
{
    public class Accesorios_Negocio
    {
        private readonly IAccesoDatos Ac;

  
        public Accesorios_Negocio(IAccesoDatos db)
        {
            Ac = db;
        }
        internal object Accesorio_Insert(Accesorio accesorio)
        {
            try
            {
                var solicit = (from dt in Ac.Accesorio_Insert(accesorio).AsEnumerable()
                               select new Accesorio_Respuesta
                               {
                                   Descripcion_Accesorio = Convert.ToString(dt["Descripcion_Accesorio"]),
                                   Cantidad = Convert.ToInt32(dt["Cantidad"]),
                                   Precio_Por_Mayor = Convert.ToDecimal(dt["Precio_Por_Mayor"]),
                                   Precio_Detalle = Convert.ToDecimal(dt["Precio_Detalle"]),
                                 
                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal object ConsultaAccesorio(int Codigo)
        {
            try
            {
                var solicit = (from dt in Ac.ConsultaAccesorio(Codigo).AsEnumerable()
                               select new Accesorio_Respuesta
                               {
                                   Descripcion_Accesorio = Convert.ToString(dt["Descripcion_Accesorio"]),
                                   Cantidad = Convert.ToInt32(dt["Cantidad"]),
                                   Precio_Por_Mayor = Convert.ToDecimal(dt["Precio_Por_Mayor"]),
                                   Precio_Detalle = Convert.ToDecimal(dt["Precio_Detalle"]),

                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
