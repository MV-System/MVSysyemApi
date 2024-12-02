using DTO;
using MVSystemApi.Interfaz;
using System.Data;

namespace MVSystemApi.Model
{
    public class Equipos_Negocio
    {
         private readonly IAccesoDatos Ac;
        // private readonly AccesoDatos Ac;

        public Equipos_Negocio(IAccesoDatos Ad)
        {
            Ac = Ad;
        }
        internal object Equipo_Insert(Equipos Equipo)
        {
            // Llamada al procedimiento almacenado
            var result = (from dt in Ac.Equipo_Insert(Equipo).AsEnumerable()
                          select new Equipo_return
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              Marca = Convert.ToString(dt["Marca"]),
                              Existe = Convert.ToString(dt["Existe"]),
                              Mensaje = Convert.ToString(dt["Mensaje"])
                          }).ToList(); // Usamos FirstOrDefault() ya que esperamos un único registro

            // Verificamos el valor de Result
            if (result != null)
            {
               return  result;
            }

            // En caso de no recibir datos o un resultado inesperado, manejamos el error
            return new { success = false, Mensaje = "Error desconocido" };
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
                              Disponible = Convert.ToString(dt["Disponible"]),
                              Disponible_Detalle = Convert.ToString(dt["Nota_Adicional"]),
                              Mensaje = Convert.ToString(dt["Mensaje"])

                          }).ToList().FirstOrDefault();


            return result;
        }
     
        internal object GetEquiposDisponible(EquipoDisponibleFilterDTO equipoDisponibleFilterDTO)
        {


            var result = (from dt in Ac.GetEquiposDisponible(equipoDisponibleFilterDTO).AsEnumerable()
                          select new EquipoDisponibleDTO
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Descripcion = Convert.ToString(dt["Descripcion"]),
                              Tecnologia = Convert.ToString(dt["Tecnologia"]),
                              Garantia = Convert.ToString(dt["Garantia"]),
                              EstadoBloqueo = Convert.ToString(dt["Estado Bloqueo"]),
                              PrecioPorMayor = Convert.ToDecimal(dt["Por Mayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["Al Detalle"]),
                              ComisionDetalle = Convert.ToDecimal(dt["Comision Detalle"]),
                              ComisionPorMayor = Convert.ToDecimal(dt["Comision Por Mayor"]),
                              DescripcionAlmacen = Convert.ToString(dt["Descripcion_Almacen"]),
                              Suplidor = Convert.ToString(dt["Suplidor"]),
                              CostoEquipo = Convert.ToDecimal(dt["Costo"]),
                              IsDesbloqueado = Convert.ToString(dt["Desbloqueado"]),
                              IsDisponible = Convert.ToString(dt["Disponible"]),
                              NotaAdicional = Convert.ToString(dt["Nota"]),
                              IdAlmacen = Convert.ToInt32(dt["ID_Almacen"]),
                              IdModelo = Convert.ToInt32(dt["ID_Modelo"]),
                              IdSuplidor = Convert.ToInt32(dt["ID_Suplidor"]),
                              Fecharegistro = Convert.ToString(dt["Fecha_registro"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                              TotalInventario = Convert.ToDecimal(dt["TotalInventario"])
                          }).ToList();
            return result;
        }
        internal object GetEquiposVendidos(EquipoVendidoFilter equipoVendidoFilter)
        {


            var result = (from dt in Ac.GetEquiposVendidos(equipoVendidoFilter).AsEnumerable()
                          select new EquipoVendido
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Descripcion = Convert.ToString(dt["Descripcion"]),
                              PrecioVendido = Convert.ToDecimal(dt["Precio Vendido"]),
                              TipoFactura = Convert.ToString(dt["Tipo Factura"]),
                              Factura = Convert.ToString(dt["Factura"]),
                              PrecioPorMayor = Convert.ToDecimal(dt["Por Mayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["Al Detalle"]),
                              ComisionDetalle = Convert.ToDecimal(dt["Comision Detalle"]),
                              ComisionPorMayor = Convert.ToDecimal(dt["Comision Por Mayor"]),
                              DescripcionAlmacen = Convert.ToString(dt["Descripcion_Almacen"]),
                              Suplidor = Convert.ToString(dt["Suplidor"]),
                              Vendedor = Convert.ToString(dt["Vendedor"]),
                              Cliente = Convert.ToString(dt["Cliente"]),
                              CostoEquipo = Convert.ToDecimal(dt["Costo"]),
                              Itbis = Convert.ToDecimal(dt["Itbis"]),
                              IsDesbloqueado = Convert.ToString(dt["Desb"]),
                              NotaAdicional = Convert.ToString(dt["Nota"]),
                              //IdAlmacen = Convert.ToInt32(dt["ID_Almacen"]),
                              //IdModelo = Convert.ToInt32(dt["ID_Modelo"]),
                              //IdSuplidor = Convert.ToInt32(dt["ID_Suplidor"]),
                              FechaRegistro = Convert.ToString(dt["Fecha_registro"]),
                              FechaFacturado = Convert.ToString(dt["Fecha facturado"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                              TotalInventario = Convert.ToDecimal(dt["TotalInventario"]),
                              TotalFacturado = Convert.ToDecimal(dt["TotalFacturado"]),
                              TotalGanancia = Convert.ToDecimal(dt["TotalGanancia"]),
                              TotalComisionDetalle = Convert.ToDecimal(dt["TotalComisionDetalle"]),
                              TotalComisionXMayor = Convert.ToDecimal(dt["TotalComisionXMayor"]),
                              TotalItbis = Convert.ToDecimal(dt["TotalItbis"])

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
        /// <summary>
        /// Return a equipo by Imei
        /// Author: Xavier Mejia
        /// </summary>
        /// <param name="imei"></param>
        /// <returns></returns>
        internal object GetEquipoByImei(string imei)
        {
            EquipoImei equipo = new EquipoImei();
            var dr = Ac.GetEquipoByImei(imei);
            while (dr.Read())
            {
                equipo.Id = Convert.ToInt32(dr["Id"]);
                equipo.Imei = Convert.ToString(dr["imei"]);
                equipo.Modelo = Convert.ToString(dr["Modelo"]);
                equipo.Mensaje = Convert.ToString(dr["Mensaje"]);
                equipo.AlmacenSalida = Convert.ToString(dr["Almacen"]);
                equipo.AlmacenSalidaId = Convert.ToInt32(dr["ID_Almacen"]);
            }

            return equipo;
        }    
        internal object GetEquiposTranferidos(int? idTransferencia)
        {
            var result = (from dt in Ac.GetEquiposTranferidos(idTransferencia).AsEnumerable()
                          select new EquipoTransferencia
                          {
                              IdTransferencia = Convert.ToInt32(dt["Id_Transferencia"]),
                              Imei = Convert.ToString(dt["Imei"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              AlmacenSalida = Convert.ToString(dt["Almacen_Salida"]),
                              AlmacenDestino = Convert.ToString(dt["Almacen_Destino"]),
                              CantidadEquipos = Convert.ToInt32(dt["Cantidad_Equipos"]),
                              FechaTransferencia = Convert.ToString(dt["Fecha_registro"]),

                          }).ToList();

            return result;
        }
        internal object GetEquipoUltimoIdTransferencia()
        {
            int id = 0;
            var dataReader = Ac.GetEquipoUltimoIdTransferencia();
            while (dataReader.Read())
            {
                id = Convert.ToInt32(dataReader["Ultimo_Id"]);
            }


            return id;
        }
        internal object GetTransferenciaEquipos(int? Sucursal_Salida, int? Sucursal_Destino, string FechaInicio, string FechaFinal)
        {
            var result = (from dt in  Ac.GetTransferenciaEquipos(Sucursal_Salida, Sucursal_Destino, FechaInicio, FechaFinal).AsEnumerable()
                          select new EquipoTransferencia
                          {
                              IdTransferencia = Convert.ToInt32(dt["Id_Transferencia"]),
                              //Imei = Convert.ToString(dt["Imei"]),
                              //Modelo = Convert.ToString(dt["Modelo"]),
                              AlmacenSalida = Convert.ToString(dt["Almacen_Salida"]),
                              AlmacenDestino = Convert.ToString(dt["Almacen_Destino"]),
                              CantidadEquipos = Convert.ToInt32(dt["Cantidad_Equipos"]),
                              FechaTransferencia = Convert.ToString(dt["Fecha_registro"]),
                              //LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              //Line = Convert.ToInt32(dt["Linea"]),
                              //TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"]),
                             

                          }).ToList();

            return result;
        }
        internal object GetDetalleTransferenciaEquipos(int IdTransferencia)
        {
            var result = (from dt in Ac.GetDetalleTransferenciaEquipos(IdTransferencia).AsEnumerable()
                          select new EquipoTransferencia
                          {
                              IdTransferencia = Convert.ToInt32(dt["Id_Transferencia"]),
                              Imei = Convert.ToString(dt["IDEquipo"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              FechaTransferencia = Convert.ToString(dt["Fecha_registro"]),

                          }).ToList();

            return result;
        }


        internal object GetEquipoPreciosEstatusByImei(string imei)
        {
            var result = (from dt in Ac.GetEquipoPreciosEstatusByImei(imei).AsEnumerable()
                          select new Equipo_return_Imei
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              CondicionId = Convert.ToInt32(dt["Condicion Id"]),
                              CondicionDescripcion = Convert.ToString(dt["Condicion Descripcion"]),
                              CostoEquipo = Convert.ToDecimal(dt["Costo"]),
                              PrecioDetalle = Convert.ToDecimal(dt["Al Detalle"]),
                              PrecioPorMayor = Convert.ToDecimal(dt["Por Mayor"]),
                              ComisionDetalle = Convert.ToDecimal(dt["Comision Detalle"]),
                              ComisionMayor = Convert.ToDecimal(dt["Comision Por Mayor"]),
                              EstadoBloqueoId = Convert.ToInt32(dt["Estado Bloqueo Id"]),
                              EstadoBloqueoDescripcion = Convert.ToString(dt["Descripcion_Estado_Bloqueo"])
                          }).ToList().FirstOrDefault();
            return result;
        }
        internal object Equipo_Delete(string imei, string Usuario)
        {
            try
            {
                // Llamamos al procedimiento almacenado que devuelve el mensaje
                var mensaje = Ac.Equipo_Delete(imei,Usuario);  // Este método debería devolver un string (mensaje)

                // Creación de un objeto de tipo EquipoEliminado con el mensaje
                var result = new EquipoEliminado
                {
                    mensaje = mensaje.ToString()  // Asignar el mensaje al objeto
                };

                return result;  // Retornamos el objeto con el mensaje
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo.", ex);
            }
        }
       
        internal void ModificarEquipo(Equipo_return_Imei equipo)
        {
            Ac.ModificarEquipo(equipo);
        }

        internal void InsertarEquipoRecibido(EquipoRecepcion equipo)
        {
            Ac.InsertarEquipoRecibido(equipo);
        }
        internal void EquipoEstadoActualiza(EquipoEstadoUpdate dataActualizar)
        {
            Ac.EquipoEstadoActualiza(dataActualizar);
        }

        internal object GetEquiposRecibidos(CriterioFilters filterData)
        {
            var result = (from dt in Ac.GetEquiposRecibidos(filterData).AsEnumerable()
                          select new EquipoRecepcionGet
                          {
                              RecepcionNumero = Convert.ToInt32(dt["Recepcion_Numero"]),
                              Cliente = Convert.ToString(dt["Cliente"]),
                              Cedula = Convert.ToString(dt["Cedula"]),
                              Telefono = Convert.ToString(dt["Telefono"]),
                              ImeiEntrada = Convert.ToString(dt["Imei_Entrada"]),
                              PrecioImeiEntra = Convert.ToDecimal(dt["Precio_Imei_Entra"]),
                              Modelo = Convert.ToString(dt["Modelo"]),
                              ImeiSale = Convert.ToString(dt["Imei_Sale"]),
                              Nota = Convert.ToString(dt["Nota"]),
                              Usuario = Convert.ToString(dt["Usuario"]),
                              FechaRegistro = Convert.ToString(dt["Fecha_registro"]),
                              Line = Convert.ToInt32(dt["Linea"]),
                              LastLine = Convert.ToInt32(dt["Ultima_Linea"]),
                              TotalRecord = Convert.ToInt32(dt["Cantidad_Registros"])

                          }).ToList();

            return result;
        }


        internal object PostEquipoTransferencia(EquipoTransferencia tranferencia)
        {
            try
            {

                var result = (from dt in Ac.PostEquipoTransferencia(tranferencia).AsEnumerable()
                              select new EquipoTransferencia
                              {
                                  id = Convert.ToInt32(dt["Id_Transferencia"]),
                                  Imei = Convert.ToString(dt["Imei"]),
                                  Modelo = Convert.ToString(dt["Modelo"]),
                                  AlmacenSalida = Convert.ToString(dt["AlmacenSalida"]),
                                  AlmacenDestino = Convert.ToString(dt["AlmacenDestino"]),
                              }).ToList().FirstOrDefault();
                return result;

               
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        internal object EquiposReparacionInsert(ReparacionesInsert reparacion)
        {
            var result = (from dt in Ac.Reparaciones_Insert(reparacion).AsEnumerable()
                          select new ReparacionesInsert
                          {
                              MensajeSalida = Convert.ToString(dt["MensajeSalida"]),
                              IdReparacion = Convert.ToString(dt["IdReparacion"])

                          }).ToList();

            return result;
        }
    }
}
