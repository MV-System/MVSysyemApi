using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model_Negocio
{
    public class Marcas_Negocio
    {
        private readonly IAccesoDatos AA;

        public Marcas_Negocio(IAccesoDatos dd)
        {
            AA = dd;
        }

        internal object Marca_Insert(Marcas Marca)
        {
            try
            {
                var solicit = (from dt in AA.Marca_Insert(Marca).AsEnumerable()
                               select new Marcas
                               {
                                   ID_Marca = Convert.ToInt32(dt["ID_Marca"]),
                                   descripcion = Convert.ToString(dt["Descripcion"]),
                                   Mensaje = Convert.ToString(dt["Mensaje"]),
                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object Modelo_Insert(Modelos Modelo)
        {
            try
            {
                var solicit = (from dt in AA.Modelo_Insert(Modelo).AsEnumerable()
                               select new Modelos
                               {
                                   ID_Modelo = Convert.ToInt32(dt["ID_Modelo"]),
                                   Descripcion = Convert.ToString(dt["Descripcion"]),
                                   Mensaje = Convert.ToString(dt["Mensaje"]),
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
