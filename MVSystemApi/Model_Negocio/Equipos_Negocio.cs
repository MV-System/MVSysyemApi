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
                              Costo_Equipo = Convert.ToDecimal(dt["costo_equipo"]),
                              precioPorMayor = Convert.ToDecimal(dt["Precio_Por_Mayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["Precio_Detalle"]),
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
                              Costo_Equipo = Convert.ToDecimal(dt["precioCosto"]),
                              precioPorMayor = Convert.ToDecimal(dt["precioPorMayor"]),
                              PrecioDetalle = Convert.ToDecimal(dt["PrecioDetalle"]),
                              Disponible = Convert.ToString(dt["Disponible"]),
                              Disponible_Detalle = Convert.ToString(dt["Nota_Adicional"]),
                              Mensaje = Convert.ToString(dt["Mensaje"]),
                              Configuracion = dt.Table.Columns.Contains("Configuracion") && dt["Configuracion"] != DBNull.Value ? Convert.ToString(dt["Configuracion"]) : null

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
                              TotalInventario = Convert.ToDecimal(dt["TotalInventario"]),
                              IdGrupo = dt.Table.Columns.Contains("ID_Grupo") && dt["ID_Grupo"] != DBNull.Value ? Convert.ToInt32(dt["ID_Grupo"]) : 0,
                              Grupo = dt.Table.Columns.Contains("Grupo") && dt["Grupo"] != DBNull.Value ? Convert.ToString(dt["Grupo"]) : "",
                              Configuracion = dt.Table.Columns.Contains("Configuracion") && dt["Configuracion"] != DBNull.Value ? Convert.ToString(dt["Configuracion"]) : null
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
                              TotalItbis = Convert.ToDecimal(dt["TotalItbis"]),
                              Configuracion = dt.Table.Columns.Contains("Configuracion") && dt["Configuracion"] != DBNull.Value ? Convert.ToString(dt["Configuracion"]) : null

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
        internal object ValidarEliminacionEquipo(string imei)
        {
            var criterio = imei?.Trim();
            var resultado = new EquipoValidacionEliminacion
            {
                Existe = false,
                Tablas = new List<string>(),
                Detalle = new Dictionary<string, bool>()
            };

            if (string.IsNullOrWhiteSpace(criterio))
            {
                resultado.Mensaje = "Debe indicar un IMEI válido.";
                return resultado;
            }

            var data = Ac.ValidarEliminacionEquipo(criterio);
            if (data == null || data.Tables.Count == 0)
            {
                resultado.Mensaje = "No se pudo validar el IMEI.";
                return resultado;
            }

            resultado.ServidorConsultado = Convert.ToString(data.ExtendedProperties["ServidorConsultado"]);
            resultado.BaseDatosConsultada = Convert.ToString(data.ExtendedProperties["BaseDatosConsultada"]);

            var nombresTablas = new[] { "Equipos", "InventarioEquipo", "Detalle_Facturas", "Equipos_Historico" };
            foreach (var nombreTabla in nombresTablas)
            {
                resultado.Registros[nombreTabla] = new List<Dictionary<string, object>>();
                var tabla = data.Tables.Contains(nombreTabla) ? data.Tables[nombreTabla] : null;
                if (tabla == null)
                {
                    resultado.Detalle[nombreTabla] = false;
                    continue;
                }

                foreach (DataRow fila in tabla.Rows)
                {
                    var registro = new Dictionary<string, object>();
                    foreach (DataColumn columna in tabla.Columns)
                    {
                        registro[columna.ColumnName] = fila.IsNull(columna) ? null : fila[columna];
                    }
                    resultado.Registros[nombreTabla].Add(registro);
                }

                resultado.Detalle[nombreTabla] = resultado.Registros[nombreTabla].Count > 0;
            }

            var tieneEquipos = resultado.Detalle["Equipos"];
            var tieneInventario = resultado.Detalle["InventarioEquipo"];
            var tieneDetalleFactura = resultado.Detalle["Detalle_Facturas"];
            var tieneEquiposHistorico = resultado.Detalle["Equipos_Historico"];

            if (tieneEquipos) resultado.Tablas.Add("Equipos");
            if (tieneInventario) resultado.Tablas.Add("InventarioEquipo");
            if (tieneDetalleFactura) resultado.Tablas.Add("Detalle_Facturas");
            if (tieneEquiposHistorico) resultado.Tablas.Add("Equipos_Historico");

            resultado.Existe = tieneEquipos || tieneInventario || tieneDetalleFactura || tieneEquiposHistorico;

            if (resultado.Existe)
            {
                resultado.Mensaje = $"El IMEI '{criterio}' existe en: {string.Join(", ", resultado.Tablas)}.";
            }
            else
            {
                resultado.Mensaje = $"No existe ningún registro con el IMEI '{criterio}'.";
            }

            return resultado;
        }
        internal ConsultaSqlResultado EjecutarConsultaSql(string consulta, int maxFilas)
        {
            return Ac.EjecutarConsultaSql(consulta, maxFilas);
        }

        internal ConsultaSqlConexion ObtenerConexionActualGestionarEquipo()
        {
            return Ac.ObtenerConexionActualGestionarEquipo();
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
                throw new Exception(
                    $"Error al intentar eliminar el equipo: {ex.GetBaseException().Message}",
                    ex);
            }
        }
        internal EquipoEliminado EliminarRegistroEquipoDeTabla(string imei, string tabla, string usuario)
        {
            if (string.IsNullOrWhiteSpace(imei))
            {
                throw new ArgumentException("Debe indicar un IMEI válido.", nameof(imei));
            }

            if (string.Equals(tabla?.Trim(), "Equipos_Historico", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Equipos_Historico es de solo lectura; no se permite eliminar su contenido.", nameof(tabla));
            }

            string mensaje = string.Equals(tabla?.Trim(), "Equipos", StringComparison.OrdinalIgnoreCase)
                ? Ac.Equipo_Delete(imei.Trim(), usuario ?? string.Empty)
                : Ac.EliminarRegistrosEquipoDeTabla(imei.Trim(), tabla, usuario);

            return new EquipoEliminado { mensaje = mensaje };
        }
        internal object ActualizarCostoPrecioReparacion(ReparacionPendienteUpdateDTO data)
        {
            try
            {
                // Llamamos al procedimiento almacenado que devuelve el mensaje
                var mensaje = Ac.Actualizar_CostoPrecio_Reparacion(data);  // Este método debería devolver un string (mensaje)
                                                                           // Creación de un objeto de tipo EquipoEliminado con el mensaje
                var result = new ReparacionUpdate_return
                {
                    Mensaje = mensaje.ToString()  // Asignar el mensaje al objeto
                };

                return result;  // Retornamos el objeto con el mensaje
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar Actualizar la reparacion.", ex);
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

        internal List<EquipoRegistrado> GetEquiposRegistrados(int numeroRegistro)
        {
            var result = (from dt in Ac.GetEquiposRegistrados(numeroRegistro).AsEnumerable()
                          select new EquipoRegistrado
                          {
                              Imei = Convert.ToString(dt["Imei"]),
                              Modelo = Convert.ToString(dt["modelo"]),
                              Almacen_Registro = Convert.ToString(dt["Almacen_Registro"]),
                              Usuario = Convert.ToString(dt["Usuario"]),
                              Fecha_Registro = Convert.ToDateTime(dt["Fecha_registro"]),
                              Numero_Registro = Convert.ToInt32(dt["Numero_Registro"]),
                              Total_Equipos = Convert.ToInt32(dt["total_equipos"])
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
                              IdReparacion = Convert.ToString(dt["IDReparacion"])

                          }).ToList();

            return result;
        }

        internal object GetReparacionesLista(ReparacionConsultaFiltro filterData)
        {
            var result = (from dt in Ac.GetReparacionesLista(filterData).AsEnumerable()
                          select new ReparacionListaDTO
                          {
                              IdReparacion = Convert.ToInt32(dt["idReparacion"]),
                              FechaRegistro = Convert.ToDateTime(dt["fechaRegistro"]),
                              NombreCliente = Convert.ToString(dt["nombreCliente"]),
                              TelefonoCliente = Convert.ToString(dt["telefonoCliente"]),
                              Estatus = Convert.ToString(dt["estatus"]),
                              Precio = Convert.ToDecimal(dt["precio"]),
                              Costo = Convert.ToDecimal(dt["costo"]),
                              FechaEntrega = dt["fechaEntrega"] != DBNull.Value ? 
                                  Convert.ToDateTime(dt["fechaEntrega"]) : (DateTime?)null,
                              // Propiedades de paginación
                              TotalRecords = (dt.Table.Columns.Contains("totalRecords") && dt["totalRecords"] != DBNull.Value) ?
                                  Convert.ToInt32(dt["totalRecords"]) : 0,
                              Line = (dt.Table.Columns.Contains("line") && dt["line"] != DBNull.Value) ?
                                  Convert.ToInt32(dt["line"]) : 0,
                              LastLine = (dt.Table.Columns.Contains("lastLine") && dt["lastLine"] != DBNull.Value) ?
                                  Convert.ToInt32(dt["lastLine"]) : 0
                          }).ToList();

            return result;
        }

    }
}
