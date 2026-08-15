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
        public Clientes Cliente_Insert(Clientes Cliente)
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
                                   ID_Canal = Convert.ToInt32(dt["ID_Canal"]),
                                   Canal_Nombre = Convert.ToString(dt["Canal_Nombre"]),
                                   Email_Detalle = Convert.ToString(dt["Email_Detalle"]),
                                   Direccion_Detalle = Convert.ToString(dt["Direccion_Detalle"]),
                                   Mensaje = Convert.ToString(dt["Mensaje"]),
                                   ID_Cliente = Convert.ToInt32(dt["ID_Cliente"]),
                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Clientes Cliente_Update(Clientes Cliente)
        {
            try
            {
                var solicit = (from dt in AA.Cliente_Update(Cliente).AsEnumerable()
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
                                   ID_Cliente = Convert.ToInt32(dt["ID_Cliente"]),
                               }).ToList().FirstOrDefault();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object AbonoAfactura(AbonoFacturaParam param)
        {
            try
            {
                // Llamamos al procedimiento almacenado que devuelve el mensaje
                var mensaje = AA.AbonoAfactura(param, out Guid idLoteAbono);

                // Creación de un objeto de tipo EquipoEliminado con el mensaje
                var result = new AbonoFacturaParamRetorn
                {
                    Mensaje = mensaje.ToString(),  // Asignar el mensaje al objeto
                    IdLoteAbono = idLoteAbono
                };

                return result;  // Retornamos el objeto con el mensaje
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo.", ex);
            }
        }
        internal object AbonoGeneral(AbonoFacturaParam param)
        {
            try
            {
                // Llamamos al procedimiento almacenado que devuelve el mensaje
                var mensaje = AA.AbonoGeneral(param, out Guid idLoteAbono);

                // Creación de un objeto de tipo EquipoEliminado con el mensaje
                var result = new AbonoFacturaParamRetorn
                {
                    Mensaje = mensaje.ToString(),  // Asignar el mensaje al objeto
                    IdLoteAbono = idLoteAbono
                };

                return result;  // Retornamos el objeto con el mensaje
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo.", ex);
            }
        }
        internal object Cliente_Consulta(string criterio)
        {
            try
            {
                var solicit = (from dt in AA.Cliente_Consulta(criterio).AsEnumerable()
                               select new Clientes_consulta
                               {
                                   Id_Cliente = Convert.ToInt32(dt["ID_Cliente"]),
                                   ID_Tipo_Cliente = Convert.ToInt32(dt["ID_Tipo_Cliente"]),
                                   Tipo_de_Cliente = Convert.ToString(dt["Tipo de Cliente"]),
                                   Nombres = Convert.ToString(dt["Nombres"]),
                                   Apellidos = Convert.ToString(dt["Apellidos"]),
                                   Numero_de_telefono = Convert.ToInt64(dt["Numero de telefono"]),
                                   ID_Canal = dt["ID_Canal"] == DBNull.Value ? (int?)null : Convert.ToInt32(dt["ID_Canal"]),
                                   Canal_Nombre = Convert.ToString(dt["Canal_Nombre"]),
                                   Limite_Credito = Convert.ToString(dt["Limite_Credito"]),
                                   Extencion = dt["Extencion"] == DBNull.Value ? (int?)null : Convert.ToInt32(dt["Extencion"]),
                                   Email = Convert.ToString(dt["Email"]),
                                   Direccion = Convert.ToString(dt["Direccion"]),
                                   Mensaje = Convert.ToString(dt["Mensaje"]),
                               }).ToList();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object Cliente_FacturasPendientes(string FechaInicio, string FechaFin, string Criterio)
        {
            try
            {
                var solicit = (from dt in AA.Cliente_FacturasPendientes(FechaInicio,FechaFin,Criterio).AsEnumerable()
                               select new ObtenerClientesConFacturasPendientes
                               {
                                   Id_Cliente = Convert.ToInt32(dt["ID"]),
                                   Cliente = Convert.ToString(dt["Cliente"]),
                                   MontoPendiente = Convert.ToDecimal(dt["MontoPendiente"]),
                               }).ToList();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal object Cliente_FacturasPendientesDetalle(int cliente)
        {
            try
            {
                var solicit = (from dt in AA.Cliente_FacturasPendientesDetalle(cliente).AsEnumerable()
                               select new ObtenerFacturasPendientesPorCliente
                               {
                                   NumeroFactura = Convert.ToInt32(dt["Numero_Factura"]),
                                   TotalFactura = Convert.ToDecimal(dt["Total"]),
                                   MontoPendiente = Convert.ToDecimal(dt["MontoPendiente"]),
                                   FechaFactura = Convert.ToString(dt["fecha_registro"]),
                                   FechaLimitePagoFactura = Convert.ToString(dt["Tiempo_Credito"]),
                               }).ToList();

                return solicit;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal Clientes_consulta Cliente_Consulta_Por_Id_Cliente(int id_Cliente)
        {
            try
            {
                var solicit = (from dt in AA.Cliente_Consulta_Por_Id_Cliente(id_Cliente).AsEnumerable()
                               select new Clientes_consulta
                               {
                                   Id_Cliente = Convert.ToInt32(dt["Id_Cliente"]),
                                   Tipo_de_Cliente = dt["Tipo de Cliente"] != DBNull.Value ? Convert.ToString(dt["Tipo de Cliente"]) : "",
                                   Nombres = Convert.ToString(dt["Nombres"]),
                                   Apellidos = Convert.ToString(dt["Apellidos"]),
                                   Numero_de_telefono = Convert.ToInt64(dt["Numero de telefono"]),
                                   Limite_Credito = dt["Limite_Credito"] != DBNull.Value ? Convert.ToString(dt["Limite_Credito"]) : "",
                                   Canal_Nombre = dt["Canal_Nombre"] != DBNull.Value ? Convert.ToString(dt["Canal_Nombre"]) : "",
                                   Extencion = dt["Extencion"] == DBNull.Value ? (int?)null : Convert.ToInt32(dt["Extencion"]),
                                   Email = dt["Email"] != DBNull.Value ? Convert.ToString(dt["Email"]) : "",
                                   Direccion = dt["Direccion"] != DBNull.Value ? Convert.ToString(dt["Direccion"]) : "",
                                   Mensaje = Convert.ToString(dt["Mensaje"]),
                               }).ToList();

                return solicit.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Obtiene el historial de abonos/pagos de un cliente
        /// </summary>
        internal List<DTO.HistorialAbonoDTO> ObtenerHistorialAbonos(int idCliente, string fechaInicio = null, string fechaFinal = null)
        {
            try
            {
                var solicitud = (from dt in AA.ObtenerHistorialAbonos(idCliente, fechaInicio, fechaFinal).AsEnumerable()
                                select new DTO.HistorialAbonoDTO
                                {
                                    Id = dt["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt["Id"]),
                                    IdCliente = dt["IdCliente"] == DBNull.Value ? 0 : Convert.ToInt32(dt["IdCliente"]),
                                    NumeroFactura = dt["NumeroFactura"] == DBNull.Value ? 0 : Convert.ToInt32(dt["NumeroFactura"]),
                                    TotalFactura = dt["TotalFactura"] == DBNull.Value ? 0m : Convert.ToDecimal(dt["TotalFactura"]),
                                    TotalAbono = dt["TotalAbono"] == DBNull.Value ? 0m : Convert.ToDecimal(dt["TotalAbono"]),
                                    MontoPendiente = dt["MontoPendiente"] == DBNull.Value ? 0m : Convert.ToDecimal(dt["MontoPendiente"]),
                                    IdTipoFactura = dt.Table.Columns.Contains("IdTipoFactura") && dt["IdTipoFactura"] != DBNull.Value ? Convert.ToInt32(dt["IdTipoFactura"]) : 0,
                                    TipoFactura = dt.Table.Columns.Contains("TipoFactura") && dt["TipoFactura"] != DBNull.Value ? Convert.ToString(dt["TipoFactura"]) : "",
                                    FechaVencimiento = dt.Table.Columns.Contains("FechaVencimiento") && dt["FechaVencimiento"] != DBNull.Value ? Convert.ToDateTime(dt["FechaVencimiento"]) : DateTime.Now,
                                    Estado = dt.Table.Columns.Contains("Estado") && dt["Estado"] != DBNull.Value ? Convert.ToString(dt["Estado"]) : "",
                                    Usuario = dt.Table.Columns.Contains("Usuario") && dt["Usuario"] != DBNull.Value ? Convert.ToString(dt["Usuario"]) : "",
                                    FechaRegistro = dt.Table.Columns.Contains("FechaRegistro") && dt["FechaRegistro"] != DBNull.Value ? Convert.ToDateTime(dt["FechaRegistro"]) : DateTime.Now,
                                    IdTipoPago = dt.Table.Columns.Contains("IdTipoPago") && dt["IdTipoPago"] != DBNull.Value ? Convert.ToInt32(dt["IdTipoPago"]) : 0,
                                    TipoPago = dt.Table.Columns.Contains("TipoPago") && dt["TipoPago"] != DBNull.Value ? Convert.ToString(dt["TipoPago"]) : "",
                                    Nota = dt.Table.Columns.Contains("Nota") && dt["Nota"] != DBNull.Value ? Convert.ToString(dt["Nota"]) : "",
                                    IdLoteAbono = dt.Table.Columns.Contains("IdLoteAbono") && dt["IdLoteAbono"] != DBNull.Value ? (Guid)dt["IdLoteAbono"] : (Guid?)null
                                }).ToList();

                return solicitud;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
