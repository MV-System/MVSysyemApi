using MVSystemApi.Interfaz;
using System;
using System.Linq;
using System.Data;

namespace MVSystemApi.Model
{
    public class Clientes_Negocio
    {
        private readonly IAccesoDatos AA;

        public Clientes_Negocio(IAccesoDatos dd)
        {
            AA = dd;
        }
        internal object Cliente_Insert(Clientes Cliente)
        {
            try
            {
                var solicit = (from dt in AA.Cliente_Insert(Cliente).AsEnumerable()
                               select new Clientes
                               {
                                   IDContacto = Convert.ToInt32(dt["ID_Cliente"]),
                                   Nombres = Convert.ToString(dt["Nombres"]),
                                   Apellidos = Convert.ToString(dt["Apellidos"]),
                                   Numero_Telefono = Convert.ToInt64(dt["Numero_Telefono"]),
                                   Limite_Credito = Convert.ToString(dt["Limite_Credito"]),
                                   Email_Detalle = Convert.ToString(dt["Email_Detalle"]),
                                   Direccion_Detalle = Convert.ToString(dt["Direccion_Detalle"]),
                                   Mensaje = Convert.ToString(dt["Mensaje"]),
                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object Cliente_Consulta(string criterio)
        {
            try
            {
                var solicit = (from dt in AA.Cliente_Consulta(criterio).AsEnumerable()
                               select new Clientes_consulta
                               {
                                   Id_Cliente = Convert.ToInt32(dt["Id_Cliente"]),
                                   Nombres = Convert.ToString(dt["Nombres"]),
                                   Apellidos = Convert.ToString(dt["Apellidos"]),
                                   Numero_Telefono = Convert.ToInt64(dt["Numero de telefono"]),
                                   Mensaje = Convert.ToString(dt["Mensaje"]),
                               }).ToList();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
