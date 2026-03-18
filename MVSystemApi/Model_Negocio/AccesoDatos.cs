using DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MVSystemApi.Interfaz;
using MVSystemApi.Model.Seguridad;
using MVSystemApi.Model_Negocio.Seguridad;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;

namespace MVSystemApi.Model
{
    public class AccesoDatos : IAccesoDatos
    {
        private readonly SqlConnection cn;
        public static int Numero_Registro;
        private readonly string _connectionStrings;
        private readonly IMemoryCache _memoryCache;
        public static Int64 codigo;

        // Propiedad p�blica para acceder a la cadena de conexi�n
        public string ConnectionString => _connectionStrings;

        public AccesoDatos(IHttpContextAccessor httpContextAccessor, SeguridadService seguridad, IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            var keyConnString = $"ConnString:{httpContextAccessor.HttpContext.User.Identity.Name}";

            if (_memoryCache.TryGetValue<string>(keyConnString, out var value))
                _connectionStrings = value;
            else
            {
                var conn = seguridad.GetConnStringByUser(httpContextAccessor.HttpContext.User.Identity.Name);
                _memoryCache.Set(keyConnString, conn, TimeSpan.FromMinutes(13));
                _connectionStrings = conn;
            }

            cn = new SqlConnection(_connectionStrings);
        }
        public DataTable GetCanalesLista(int Id_Canal)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Proc_Canales_Consulta_Combo";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id_Canal", Id_Canal);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public List<ReparacionReporteDTO> ObtenerReparaciones(ReparacionReporteFiltro filtro)
        {
            try
            {
                var lista = new List<ReparacionReporteDTO>();
                using var cn = new SqlConnection(_connectionStrings);
                using var cmd = new SqlCommand("SP_Reparaciones_consulta_facturacion", cn); // Debes tener este SP en tu BD
                cmd.CommandType = CommandType.StoredProcedure;

                // Agrega los par�metros seg�n tu filtro
                cmd.Parameters.AddWithValue("@IdReparacion", filtro.IdReparacion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IdCliente", filtro.IdCliente ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaInicio", filtro.FechaInicio ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaFin", filtro.FechaFin ?? (object)DBNull.Value);

                cn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new ReparacionReporteDTO
                    {
                        IdReparacion = reader.GetInt64(reader.GetOrdinal("ID_Reparacion")),
                        Imei = reader["Imei"]?.ToString(),
                        // NombreCliente = reader["NombreCliente"]?.ToString(),
                        // ApellidoCliente = reader["ApellidoCliente"]?.ToString(),
                        Detalle_Reparacion = reader["Detalle_Reparacion"]?.ToString(),
                        Observaciones = reader["Observaciones"]?.ToString(),
                        descripcion = reader["Detalle_Reparacion"]?.ToString(),
                        // Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        Precio = reader.GetDecimal(reader.GetOrdinal("Precio")),
                        // Fecha_Entrega = reader.GetDateTime(reader.GetOrdinal("Fecha_Entrega")),
                        // Usuario = reader["Usuario"]?.ToString(),
                        // Modelo = reader["Modelo"]?.ToString()
                    });
                }
                return lista;

            }
            catch (Exception)
            {

                throw;
            }
           
        }
        /// <summary>
        /// Author: Xavier Mejia
        /// Date: 21.FEB.2023
        /// </summary>
        /// <param name="numeroFactura">The receipt number</param>
        /// <param name="sucursal">The sucursal id</param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable GetDetalleCotizacionConsulta(int numeroFactura, int sucursal)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Detalle_Cotizacion_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@numeroFactura", numeroFactura);
                cmd.Parameters.AddWithValue("@sucursal", sucursal);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Author: Xavier Mejia
        /// Date: 21.FEB.2023
        /// </summary>
        /// <param name="telefono">The customer phone number </param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable GetCotizacionFacturaConsulta(string telefono)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Cotizacion_Facturar_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_Telefono", telefono);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Author: Xavier Mejia
        /// Date: 26.ENE.2023
        /// </summary>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable GetFacturaNumeroUltimo()
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Facturas_Consulta_ultimo";
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }      
        /// <summary>
        /// Author: Xavier Mejia
        /// Date: 8.Oct.2022
        /// </summary>
        /// <param name="numeroFactura">The receipt number</param>
        /// <param name="sucursal">The sucursal id</param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable GetDetalleFacturaConsulta(int numeroFactura, int sucursal)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Detalle_Factura_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@numeroFactura", numeroFactura);
                cmd.Parameters.AddWithValue("@sucursal", sucursal);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Author: Xavier Mejia
        /// Date: 8.Oct.2022
        /// </summary>
        /// <param name="consulta">The factura model</param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable GetFacturas(FacturaFilter consulta)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Proc_Facturas_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Almacen", consulta.Almacen);
                cmd.Parameters.AddWithValue("@Vendedor", consulta.IdVendedor);
                cmd.Parameters.AddWithValue("@Comprobante", consulta.ComprobanteCheck);
                cmd.Parameters.AddWithValue("@Criterio", consulta.Criterio);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", consulta.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", consulta.FechaFinal); 
                cmd.Parameters.AddWithValue("@PageIndex", consulta.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", consulta.PageSize);
                cmd.Parameters.AddWithValue("@EstadoFactura", consulta.EstadoFactura);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable GetFacturasReporte(FacturaFilter consulta)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Proc_Facturas_Consulta_reporte";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Almacen", consulta.Almacen);
                cmd.Parameters.AddWithValue("@Vendedor", consulta.IdVendedor);
                cmd.Parameters.AddWithValue("@Numero_factura", consulta.NumeroFactura);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", consulta.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", consulta.FechaFinal);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable GetCierreCajaReport(FacturaFilter filtro)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        F.ID_Tipo_Pagos,
                        COUNT(DISTINCT F.Numero_Factura) as CantidadFacturas,
                        COUNT(DISTINCT CASE
                            WHEN F.Abono > 0
                            THEN F.Numero_Factura
                        END) as CantidadAbonosRecibidos,
                        SUM(F.SubTotal) as SubTotal,
                        SUM(F.Descuento) as Descuento,
                        SUM(F.Itbis) as Itbis,
                        SUM(F.Total) as TotalFacturado,
                        SUM(CASE 
                            WHEN F.ID_Tipo_Pagos = 1 THEN F.Total
                            ELSE ISNULL(F.Abono, 0)
                        END) as EfectivoReal,
                        SUM(CASE
                            WHEN F.ID_Tipo_Pagos = 2 THEN F.Total - ISNULL(F.Abono, 0)
                            ELSE 0
                        END) as SaldoPendiente
                    FROM Facturas F
                    WHERE CAST(F.Fecha_Registro AS DATE) BETWEEN CAST(@Fecha_Inicio AS DATE) AND CAST(@Fecha_Final AS DATE)
                        AND F.ID_Sucursal = @Almacen
                        AND F.Estado = @EstadoFactura
                        AND (@IdVendedor = 0 OR F.ID_Vendedor = @IdVendedor)
                    GROUP BY F.ID_Tipo_Pagos
                    ORDER BY TotalFacturado DESC";
                
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@Fecha_Inicio", filtro.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", filtro.FechaFinal);
                cmd.Parameters.AddWithValue("@Almacen", filtro.Almacen);
                cmd.Parameters.AddWithValue("@EstadoFactura", filtro.EstadoFactura ?? "");
                cmd.Parameters.AddWithValue("@IdVendedor", filtro.IdVendedor);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las facturas MIXTO (ID_Tipo_Pagos = 6) en el período especificado
        /// </summary>
        public DataTable GetFacturasMixtoEnPeriodo(FacturaFilter filtro)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        F.Numero_Factura,
                        F.Total,
                        F.ID_Sucursal,
                        F.ID_Vendedor
                    FROM Facturas F
                    WHERE CAST(F.Fecha_Registro AS DATE) BETWEEN CAST(@Fecha_Inicio AS DATE) AND CAST(@Fecha_Final AS DATE)
                        AND F.ID_Sucursal = @Almacen
                        AND F.Estado = @EstadoFactura
                        AND F.ID_Tipo_Pagos = 6
                        AND (@IdVendedor = 0 OR F.ID_Vendedor = @IdVendedor)
                    ORDER BY F.Numero_Factura DESC";
                
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@Fecha_Inicio", filtro.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", filtro.FechaFinal);
                cmd.Parameters.AddWithValue("@Almacen", filtro.Almacen);
                cmd.Parameters.AddWithValue("@EstadoFactura", filtro.EstadoFactura ?? "");
                cmd.Parameters.AddWithValue("@IdVendedor", filtro.IdVendedor);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region Accesorios
        /// <summary>
        /// Return a equipo by Imei    
        /// Author: Xavier Mejia
        /// Date: 6.Oct.2022
        /// </summary>
        /// <param name="id">The id accesorio</param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable GetAccesorioById(int id)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Accesorios_Consulta_Id", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id", id);
              
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// 
        /// Author: Xavier Mejia
        /// Date: 8.Oct.2022
        /// </summary>
        /// <param name="accesorio">The accesorio model</param>
        /// <returns>
        /// DataTable
        /// </returns>
        public DataTable UpdateAccesorio(AccesorioDTO accesorio)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Accesorios_Modifica", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Accesorio", accesorio.ID);
                cmd.Parameters.AddWithValue("@Codigo", accesorio.Codigo);
                cmd.Parameters.AddWithValue("@Descripcion_Accesorio", accesorio.DescripcionAccesorio);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", accesorio.PrecioPorMayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", accesorio.PrecioDetalle);
                cmd.Parameters.AddWithValue("@Estado", accesorio.Estado);
                cmd.Parameters.AddWithValue("@Cantidad", accesorio.Cantidad);
                cmd.Parameters.AddWithValue("@Costo_Equipo", accesorio.CostoEquipo); 
                cmd.Parameters.AddWithValue("@Usuario", accesorio.Usuario);

                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable GetAccesorios(Paginate paginate,string accesorio, int almacen)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Accesorios_Consulta", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PageIndex", paginate.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", paginate.PageSize);
                cmd.Parameters.AddWithValue("@filter", accesorio);
                cmd.Parameters.AddWithValue("@Criterio", almacen);

                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable GetAccesoriosVendidos(Paginate paginate, string filter, int almacen)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Accesorios_Vendidos_Consulta", cn);
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_sucursal", almacen);
                cmd.Parameters.AddWithValue("@Filter", filter);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", paginate.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", paginate.FechaFinal);
                cmd.Parameters.AddWithValue("@PageIndex", paginate.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", paginate.PageSize);
                

                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Accesorio_Insert(Accesorio Accesorio)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Accesorio_Insert";

                cmd.Parameters.AddWithValue("@Imei_Equipo", Accesorio.Imei_Equipo);
                cmd.Parameters.AddWithValue("@ID_Modelo", Accesorio.ID_Modelo);
                cmd.Parameters.AddWithValue("@Numero_de_Factura", Accesorio.codigo);
                cmd.Parameters.AddWithValue("@Descripcion_Accesorio", Accesorio.Descripcion_Accesorio);
                cmd.Parameters.AddWithValue("@ID_Almacen", Accesorio.ID_Almacen);
                cmd.Parameters.AddWithValue("@Cantidad", Accesorio.Cantidad);
                cmd.Parameters.AddWithValue("@Can_Max", Accesorio.Can_Max);
                cmd.Parameters.AddWithValue("@Can_Min", Accesorio.Can_Min);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", Accesorio.Precio_Por_Mayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", Accesorio.Precio_Detalle);
                cmd.Parameters.AddWithValue("@Costo_Equipo", Accesorio.Costo_Equipo);
                cmd.Parameters.AddWithValue("@Codigo_Barra", Accesorio.Cod_Barra);
                cmd.Parameters.AddWithValue("@Disponible", Accesorio.Disponible);
                cmd.Parameters.AddWithValue("@Disponible_Detalle", Accesorio.Disponible_Detalle);
                cmd.Parameters.AddWithValue("@Usuario", Accesorio.Usuario);

                //cmd.ExecuteNonQuery();
                da.SelectCommand = cmd;
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception)
            {
                throw;
            }

        }
        public DataTable ConsultaAccesorio(int Codigo)
        {
            try
            {
                cn.Open();

                Accesorio Accesorio = new Accesorio();
                SqlCommand comando = cn.CreateCommand();
                comando.CommandText = "Accesorios_Consulta_pk";
                comando.Parameters.AddWithValue("Codigo", Codigo);
                comando.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = comando;
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();

                return dt;
            }
            catch (Exception)
            {
                throw;
            }

        }

        #endregion
        public DataTable Suplidor_Insert(Suplidor Suplidor)
        {
            cn.Open();

            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "Suplidores_Insert";

            cmd.Parameters.AddWithValue("@Nombres", Suplidor.Nombres);
            cmd.Parameters.AddWithValue("@Apellidos", Suplidor.Apellidos);
            cmd.Parameters.AddWithValue("@Numero_Telefono", Suplidor.Numero_Telefono);
            cmd.Parameters.AddWithValue("@Extencion", Suplidor.Extencion);
            cmd.Parameters.AddWithValue("@Email_Detalle", Suplidor.Email_Detalle);
            cmd.Parameters.AddWithValue("@Direccion_Detalle", Suplidor.Direccion_Detalle);
            cmd.Parameters.AddWithValue("@ID_Tipo_Telefono", Suplidor.ID_Tipo_Telefono);
            cmd.Parameters.AddWithValue("@ID_Tipo_Suplidor", Suplidor.ID_Tipo_Suplidor);
            cmd.Parameters.AddWithValue("@Usuario", Suplidor.Usuario);

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Vendedor_Insert(Vendedor Vendedor)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Vendedor_Insert";

                cmd.Parameters.AddWithValue("@Nombres", Vendedor.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", Vendedor.Apellidos);
                cmd.Parameters.AddWithValue("@Numero_Telefono", Vendedor.Numero_Telefono);
                cmd.Parameters.AddWithValue("@Extencion", Vendedor.Extencion);
                cmd.Parameters.AddWithValue("@Email_Detalle", Vendedor.Email_Detalle);
                cmd.Parameters.AddWithValue("@Direccion_Detalle", Vendedor.Direccion_Detalle);
                cmd.Parameters.AddWithValue("@ID_Tipo_Telefono", Vendedor.ID_Tipo_Telefono);
                cmd.Parameters.AddWithValue("@Usuario", Vendedor.Usuario);

                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Cliente_Insert(Clientes clientes)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Cliente_Insert";

                cmd.Parameters.AddWithValue("@Nombres", clientes.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", clientes.Apellidos);
                cmd.Parameters.AddWithValue("@ID_Tipo_Cliente", clientes.ID_Tipo_Cliente);
                cmd.Parameters.AddWithValue("@ID_Canal", clientes.ID_Canal);
                cmd.Parameters.AddWithValue("@Numero_Telefono", clientes.Numero_Telefono);
                cmd.Parameters.AddWithValue("@Limite_Credito", clientes.Limite_Credito);
                cmd.Parameters.AddWithValue("@Extencion", clientes.Extencion);
                cmd.Parameters.AddWithValue("@Email_Detalle", clientes.Email_Detalle);
                cmd.Parameters.AddWithValue("@Direccion_Detalle", clientes.Direccion_Detalle);
                cmd.Parameters.AddWithValue("@ID_Tipo_Telefono", clientes.ID_Tipo_Telefono);
                cmd.Parameters.AddWithValue("@Usuario", clientes.Usuario);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable Cliente_Update(Clientes clientes)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Cliente_Update";

                cmd.Parameters.AddWithValue("@ID_Cliente", clientes.ID_Cliente);
                cmd.Parameters.AddWithValue("@Nombres", clientes.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", clientes.Apellidos);
                cmd.Parameters.AddWithValue("@ID_Tipo_Cliente", clientes.ID_Tipo_Cliente);
                cmd.Parameters.AddWithValue("@ID_Canal", clientes.ID_Canal);
                cmd.Parameters.AddWithValue("@Numero_Telefono", clientes.Numero_Telefono);
                cmd.Parameters.AddWithValue("@Limite_Credito", clientes.Limite_Credito);
                cmd.Parameters.AddWithValue("@Extencion", clientes.Extencion);
                cmd.Parameters.AddWithValue("@Email_Detalle", clientes.Email_Detalle);
                cmd.Parameters.AddWithValue("@Direccion_Detalle", clientes.Direccion_Detalle);
                cmd.Parameters.AddWithValue("@ID_Tipo_Telefono", clientes.ID_Tipo_Telefono);
                cmd.Parameters.AddWithValue("@Usuario", clientes.Usuario);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Cliente_Consulta_Por_Id_Cliente(int id_Cliente)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Cliente_Consulta_Por_Id_Cliente";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdCliente", id_Cliente);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable Cliente_Consulta(string criterio)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Proc_Cliente_Consulta";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Criterio", criterio);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Cliente_FacturasPendientes(string FechaInicio, string FechaFin, string Criterio)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "ObtenerClientesConFacturasPendientes";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FechaInicio", FechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", FechaFin);
                cmd.Parameters.AddWithValue("@Criterio", Criterio);
                //cmd.Parameters.AddWithValue("@NombreCliente", criterio.NombreCliente);
                //cmd.Parameters.AddWithValue("@NumeroTelefono", criterio.NumeroTelefono);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Cliente_FacturasPendientesDetalle(int cliente)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "ObtenerFacturasPendientesPorCliente";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id_Cliente", cliente);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable GetTipo_Telefono_Lista()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                DataTable dt = new DataTable();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipo_Telefonos_Cata_Consulta_Combo";
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);

                da.Fill(dt);
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Tipos_Pagos_Cata_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipos_Pagos_Cata_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable GetTipos_Factura_Lista()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipos_Facturas_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Accesorios_Consulta_Facturacion(int? Id_accesorio, int? id_sucu)
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Proc_Accesorios_Consulta_Facturacion";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Accesorio", Id_accesorio);
                cmd.Parameters.AddWithValue("@id_sucu", id_sucu);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Vendedor_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Proc_Vendedor_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Get_Marcas_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Marcas_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Tipo_Cliente_Cata_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipo_Cliente_Cata_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Tipo_Suplidor_Cata_Consulta_Combo()
        {

            ////SqlConnection cn = new SqlConnection(con);
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Tipos_Suplidor_Cata_Consulta_Combo";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Get_Tegnologia_Combo()
        {
            cn.Open();

            SqlCommand cmd = cn.CreateCommand();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Tecnologias_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Garantia_Equipo_Combo()
        {
            cn.Open();
            SqlCommand cmd = cn.CreateCommand();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Garantias_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Desbloqueado_Equipo_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Estados_Bloqueos_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;
            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Condicion_Equipo_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Condicion_equipos_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Almacen_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Almacen_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Sucursal_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Sucursales_cata_Combo";
            cmd.CommandType = CommandType.StoredProcedure;
            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_StatusTaller_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Estados_Taller_Cata_Combo";
            cmd.CommandType = CommandType.StoredProcedure;
            
            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Modelos_Combo(int? ID_Marca)
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Modelos_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@criterio", ID_Marca);

            da.Fill(dt);
            cn.Close();
            return dt;
        }
        public DataTable Get_Suplidor_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Suplidor_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }

        #region Equipos

        public DataTable Equipo_Insert(Equipos Equipo)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                cmd.CommandText = "Proc_Equipos_Insert";
                cmd.CommandType = CommandType.StoredProcedure;

               cmd.Parameters.Add("@Imei", SqlDbType.VarChar, 30).Value = Equipo.Imei;
                cmd.Parameters.AddWithValue("@ID_Modelo", Equipo.ID_Modelo);
                cmd.Parameters.AddWithValue("@ID_Tecnologia", Equipo.ID_Tecnologia);
                cmd.Parameters.AddWithValue("@ID_Garantia", Equipo.ID_Garantia);
                cmd.Parameters.AddWithValue("@ID_Condicion", Equipo.ID_Condicion);
                cmd.Parameters.AddWithValue("@Numero_de_Factura", Equipo.Numero_de_Factura);
                cmd.Parameters.AddWithValue("@ID_Estado_Bloqueo", Equipo.ID_Estado_Bloqueo);
                cmd.Parameters.AddWithValue("@ID_Almacen", Equipo.ID_Almacen);
                cmd.Parameters.AddWithValue("@ID_Suplidor", Equipo.ID_Suplidor);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", Equipo.Precio_Por_Mayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", Equipo.Precio_Detalle);
                cmd.Parameters.AddWithValue("@Costo_Equipo", Equipo.Costo_Equipo);
                cmd.Parameters.AddWithValue("@Desbloqueado", Equipo.Desbloqueado);
                cmd.Parameters.AddWithValue("@Disponible", Equipo.Disponible);
                cmd.Parameters.AddWithValue("@Nota_Adicional", Equipo.Nota_Adicional);
                cmd.Parameters.AddWithValue("@Usuario", Equipo.Usuario);
                cmd.Parameters.AddWithValue("@Fecha_registro", DateTime.Now);
                cmd.Parameters.AddWithValue("@Numero_Registro", Equipo.Numero_Registro);
                cmd.Parameters.AddWithValue("@comision_Detalle", Equipo.Comision_Detalle);
                cmd.Parameters.AddWithValue("@comision_Por_Mayor", Equipo.Comision_Por_Mayor);

                // Execute the query and fill the DataTable
                da.Fill(dt);

                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable GetEquiposDisponible(EquipoDisponibleFilterDTO equipoDisponibleFilterDTO)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Proc_Equipo_Disponible_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Almacen", equipoDisponibleFilterDTO.Almacen);
                cmd.Parameters.AddWithValue("@Suplidor", equipoDisponibleFilterDTO.Suplidor);
                cmd.Parameters.AddWithValue("@Imei", equipoDisponibleFilterDTO.Imei);
                cmd.Parameters.AddWithValue("@ID_Modelo", equipoDisponibleFilterDTO.Modelo);
                cmd.Parameters.AddWithValue("@PageIndex", equipoDisponibleFilterDTO.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", equipoDisponibleFilterDTO.PageSize);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", equipoDisponibleFilterDTO.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", equipoDisponibleFilterDTO.FechaFinal);

                //cmd.Parameters.AddWithValue("@criterio", criterio);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }

        }  
        public DataTable GetEquiposTranferidos(int? idTransferencia)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new(cmd);
                DataTable dt = new();

                cmd.CommandText = "Proc_Equipo_Ultimos_Transferidos_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_Transferencia", idTransferencia);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }

        }
        
        public DataTable GetEquiposRecibidos(CriterioFilters filterData)
        {
            try
            {
                if (filterData.criterio != null) filterData.criterio = filterData.criterio.Trim();
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new(cmd);
                DataTable dt = new();

                cmd.CommandText = "Recepcion_equipos_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Criterio", filterData.criterio);
                cmd.Parameters.AddWithValue("@PageIndex", filterData.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", filterData.PageSize);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", filterData.FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", filterData.FechaFinal);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }

        }

        //public DataTable GetEquiposVendidos(EquipoVendidoFilter equipoVendidoFilter)
        //{
        //    try
        //    {
        //        cn.Open();

        //        SqlCommand cmd = cn.CreateCommand();
        //        SqlDataAdapter da = new SqlDataAdapter(cmd);
        //        DataTable dt = new DataTable();


        //        cmd.CommandText = "Proc_Equipo_Consulta_Vendidos";
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.AddWithValue("@Almacen", equipoVendidoFilter.Almacen);
        //        cmd.Parameters.AddWithValue("@Imei", equipoVendidoFilter.Imei);
        //        cmd.Parameters.AddWithValue("@Suplidor", equipoVendidoFilter.Suplidor);
        //        cmd.Parameters.AddWithValue("@Vendedor", equipoVendidoFilter.Vendedor);
        //        cmd.Parameters.addWithValue("@Telefono", equipoVendidoFilter.Telefono);
        //        cmd.Parameters.AddWithValue("@ID_Modelo", equipoVendidoFilter.Modelo);
        //        cmd.Parameters.AddWithValue("@PageIndex", equipoVendidoFilter.PageIndex);
        //        cmd.Parameters.AddWithValue("@PageSize", equipoVendidoFilter.PageSize);
        //        //cmd.Parameters.AddWithValue("@Fecha_Registro", equipoVendidoFilter.FechaRegistro);
        //        cmd.Parameters.AddWithValue("@Fecha_Inicio", equipoVendidoFilter.FechaInicio);
        //        cmd.Parameters.AddWithValue("@Fecha_Final", equipoVendidoFilter.FechaFinal);
        //        cmd.Parameters.AddWithValue("@Fecha_factura_Inicial", equipoVendidoFilter.FechaInicioFactura);
        //        cmd.Parameters.AddWithValue("@Fecha_factura_Final", equipoVendidoFilter.FechaFinalFactura);

        //        //cmd.Parameters.AddWithValue("@criterio", criterio);
        //        cmd.ExecuteNonQuery();
        //        da.Fill(dt);
        //        cn.Close();
        //        return dt;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }

        //}
        public DataTable GetEquiposVendidos(EquipoVendidoFilter f)
        {
            DataTable dt = new DataTable();

            using (SqlCommand cmd = new SqlCommand("Proc_Equipo_Consulta_Vendidos", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Almacen", SqlDbType.Int).Value = f.Almacen;
                cmd.Parameters.Add("@Imei", SqlDbType.VarChar, 30).Value = f.Imei ?? "";
                cmd.Parameters.Add("@Telefono", SqlDbType.BigInt).Value = f.Telefono;
                cmd.Parameters.Add("@Suplidor", SqlDbType.VarChar, 150).Value = f.Suplidor ?? "";
                cmd.Parameters.Add("@Vendedor", SqlDbType.VarChar, 150).Value = f.Vendedor ?? "";
                cmd.Parameters.Add("@ID_Modelo", SqlDbType.Int).Value = f.Modelo;
                cmd.Parameters.Add("@PageIndex", SqlDbType.Int).Value = f.PageIndex;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = f.PageSize;
                cmd.Parameters.Add("@Fecha_Inicio", SqlDbType.VarChar, 50).Value = f.FechaInicio ?? "";
                cmd.Parameters.Add("@Fecha_Final", SqlDbType.VarChar, 50).Value = f.FechaFinal ?? "";
                cmd.Parameters.Add("@Fecha_factura_Inicial", SqlDbType.VarChar, 50).Value = f.FechaInicioFactura ?? "";
                cmd.Parameters.Add("@Fecha_factura_Final", SqlDbType.VarChar, 50).Value = f.FechaFinalFactura ?? "";

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    cn.Open();
                    da.Fill(dt);
                    cn.Close();
                }
            }

            return dt;
        }

        public DataTable Equipo_Busca_Disponible(string Equipo, int Id_Almacen)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Equipo_Busca_Disponible";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Equipo", Equipo);
                cmd.Parameters.AddWithValue("@ID_Almacen", Id_Almacen);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }

        }


        public SqlDataReader GetEquipoByImei(string imei)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Equipo_Imei_Transfiere_Consulta", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@imei", imei);
                SqlDataReader dr = cmd.ExecuteReader();

                return dr;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public SqlDataReader GetEquipoUltimoIdTransferencia()
        {
            try
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Proc_Equipo_Transferencia_Trans_Ultimo_Consulta", cn);

                SqlDataReader dr = cmd.ExecuteReader();

                cmd.CommandType = CommandType.StoredProcedure;
                return dr;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable GetTransferenciaEquipos(int? Sucursal_Salida, int? Sucursal_Destino, string FechaInicio, string FechaFinal)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Proc_Equipo_Transferencias_trans_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Sucursal_Salida", Sucursal_Salida);
                cmd.Parameters.AddWithValue("@Sucursal_Destino", Sucursal_Destino);
                cmd.Parameters.AddWithValue("@Fecha_Inicio", FechaInicio);
                cmd.Parameters.AddWithValue("@Fecha_Final", FechaFinal);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable GetDetalleTransferenciaEquipos(int IdTransferencia)
        {
            try
            {
                cn.Open();

                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();


                cmd.CommandText = "Proc_Equipo_Transferencias_Detalle_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdTransferencia", IdTransferencia);
                cmd.ExecuteNonQuery();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
        public DataTable GetComprobantes_Combo()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "TIPO_RNC_Cata_Consulta_Combo";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }

        public DataTable AsumeITBIS()
        {
            cn.Open();
            DataTable dt = new DataTable();
            SqlCommand cmd = cn.CreateCommand();
            SqlDataAdapter da = new SqlDataAdapter(cmd);

            cmd.CommandText = "Asume_ITBIS_Consulta";
            cmd.CommandType = CommandType.StoredProcedure;

            da.Fill(dt);
            cn.Close();
            return dt;
        }

        public int UpdateAsumeITBIS(string asumeITBIS, string usuario)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Asume_ITBIS_Actualizar";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@AsumeITBIS", asumeITBIS);
                cmd.Parameters.AddWithValue("@registro_Usuario", usuario);

                int result = cmd.ExecuteNonQuery();
                cn.Close();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al actualizar AsumeITBIS: " + ex.Message);
            }
        }

        public DataTable PostDetalleFactura(List<DetalleFactura> detalleFactura, int numeroFactura)
        {          

            DataTable dt = new DataTable();
            using (cn)
            {
                SqlCommand cmd = new SqlCommand("Detalle_Factura_Insert", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cn.Open();

                foreach (var item in detalleFactura)
                {
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@Numero_Factura", numeroFactura);
                    cmd.Parameters.AddWithValue("@ID_Equipo", item.IdEquipo);
                    cmd.Parameters.AddWithValue("@Descripcion", item.Descripcion);
                    cmd.Parameters.AddWithValue("@ID_Tipo", item.IdTipo);
                    cmd.Parameters.AddWithValue("@ID_Garantia", item.IdGarantia);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Factura", item.IdTipoFactura);
                    cmd.Parameters.AddWithValue("@ID_Vendedor", item.IdVendedor);
                    cmd.Parameters.AddWithValue("@SubTotal", item.SubTotal);
                    cmd.Parameters.AddWithValue("@cantidad", item.Cantidad);
                    cmd.Parameters.AddWithValue("@precio", item.Precio);
                    cmd.Parameters.AddWithValue("@Descuento", item.Descuento);
                    cmd.Parameters.AddWithValue("@Itbis", item.Itbis);
                    cmd.Parameters.AddWithValue("@Total", item.Total);
                    cmd.Parameters.AddWithValue("@Estado", "A");
                    //cmd.Parameters.AddWithValue("@Usuario", StaticClass.UsuarioLogin);
                    cmd.Parameters.AddWithValue("@Usuario", item.Usuario);
                    cmd.Parameters.AddWithValue("@fecha_registro", item.FechaRegistro);

                    //cmd.ExecuteNonQuery();
                    da.Fill(dt);
                }

                cn.Close();

            }
            return dt;

        }

        public static List<T> BindList<T>(DataTable dt) where T : new()
        {
            var props = typeof(T).GetProperties();
            var l = new List<T>();

            foreach (DataRow dr in dt.Rows)
            {
                var ob = new T();

                foreach (var p in props)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        // Matching the columns with fields
                        if (p.Name == dc.ColumnName)
                        {
                            // Get the value from the datatable cell
                            object value = dr[dc.ColumnName];

                            if (value is DBNull)
                                break;

                            // Set the value into the object
                            p.SetValue(ob, value);
                            break;
                        }
                    }
                }

                l.Add(ob);
            }

            return l;
        }

        public Facturas PostFactura(Facturas Factura)
        {
            try
            {
                using (cn)
                {
                    SqlCommand cmd = new SqlCommand("facturas_Insert", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    cmd.Parameters.AddWithValue("@ID_Cliente", Factura.IdCliente);
                    cmd.Parameters.AddWithValue("@ID_Vendedor", Factura.IdVendedor);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Factura", Factura.IdTipoFactura);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Pagos", Factura.IdTipoPago);
                    cmd.Parameters.AddWithValue("@ID_Almacen", Factura.IdAlmacen);
                    cmd.Parameters.AddWithValue("@Cantidad_Articulos", Factura.CantidadArticulos);
                    cmd.Parameters.AddWithValue("@Itbis", Factura.Itbis);
                    cmd.Parameters.AddWithValue("@NFCTipoNumero", Factura.NcfTipoNumero);
                    cmd.Parameters.AddWithValue("@RNC", Factura.RNC);
                    cmd.Parameters.AddWithValue("@SubTotal", Factura.SubTotal);
                    cmd.Parameters.AddWithValue("@Abono", Factura.Abono);
                    cmd.Parameters.AddWithValue("@Tiempo_Credito", Factura.fechaLimitePago);
                    cmd.Parameters.AddWithValue("@Total", Factura.Total);
                    cmd.Parameters.AddWithValue("@Descuento", Factura.Descuento);
                    cmd.Parameters.AddWithValue("@Nota", Factura.Nota);
                    cmd.Parameters.AddWithValue("@Estado", "A");
                    cmd.Parameters.AddWithValue("@Usuario", Factura.Usuario);
                    cmd.Parameters.AddWithValue("@fecha_registro", Factura.FechaRegistro);
                    cn.Open();

                    SqlParameter numero_Factura = new SqlParameter("@Numero_factura", 0);
                    numero_Factura.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(numero_Factura);
                    cmd.ExecuteNonQuery();
                    Factura.NumeroFactura = Convert.ToInt32(cmd.Parameters["@Numero_factura"].Value.ToString());
                    cn.Close();
                    
                    // Solo guardar detalles si hay artículos en la factura
                    if (Factura.DetalleFacturaList != null && Factura.DetalleFacturaList.Count > 0)
                    {
                        PostDetalleFactura(Factura.DetalleFacturaList, Factura.NumeroFactura);
                    }

                    return Factura;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public FilterAnulaFactura PostAnulaFactura(FilterAnulaFactura Factura)
        {
            try
            {
                using (cn)
                {
                    SqlCommand cmd = new SqlCommand("Proc_Anular_Factura", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    cmd.Parameters.AddWithValue("@NumeroFactura", Factura.NumeroFactura);
                    cmd.Parameters.AddWithValue("@IdSucursal", Factura.IDsucursal);
                    cmd.Parameters.AddWithValue("@Usuario", Factura.Usuario);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    cn.Close();

                    return Factura;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string EditaDetalleFactura(EditaDetalleFactura EditaDetalleFactura)
        {
            try
            {
                // Crear comando SQL
                using (SqlCommand cmd = new SqlCommand("Detalle_Factura_Cambia_IMEI", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar solo el par�metro de entrada @imei
                    cmd.Parameters.AddWithValue("@IMEI", EditaDetalleFactura.EquipoActual);
                    cmd.Parameters.AddWithValue("@IMEI_Nuevo", EditaDetalleFactura.EquipoNuevo);
                    cmd.Parameters.AddWithValue("@Numero_Factura", EditaDetalleFactura.NumeroFactura);
                    cmd.Parameters.AddWithValue("@Usuario", EditaDetalleFactura.Usuario);

                    // Crear el par�metro de salida para el mensaje
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output  // Definir como par�metro de salida
                    };
                    cmd.Parameters.Add(mensajeParam);  // Solo agregar el par�metro de salida

                    // Abrir la conexi�n
                    cn.Open();
                    cmd.ExecuteNonQuery();  // Ejecutar el procedimiento almacenado
                    cn.Close();

                    // Obtener el valor del mensaje de salida
                    string mensaje = mensajeParam.Value.ToString();

                    return mensaje;  // Retornar el mensaje que devuelve el procedimiento
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo", ex);
            }
        }
        public string RebajaEquipo(RebajaEquipoModel model)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("Proc_Detalle_Factura_Elimina_Equipo", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@imei", model.imei);
                    cmd.Parameters.AddWithValue("@numero_Factura", model.NumeroFactura);
                    cmd.Parameters.AddWithValue("@IdSucursal", model.IDsucursal);
                    cmd.Parameters.AddWithValue("@Usuario", model.Usuario);

                    cn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["mensaje"].ToString();
                        }
                        else
                        {
                            return "Equipo rebajado correctamente.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al rebajar el equipo: " + ex.Message, ex);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
        }

        public DataTable Equipo_Consulta_Ultimo_Registro()
        {
            cn.Open();

            try
            {
                DataTable dt = new DataTable();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Equipo_Consulta_Ultimo_Registro";
                cmd.CommandType = CommandType.StoredProcedure;

                da.Fill(dt);
                cn.Close();
                return dt;

            }
            catch (Exception)
            {
                throw;
            }


        }

        public DataTable GetEquipoPreciosEstatusByImei(string imei)
        {
            cn.Open();

            try
            {
                DataTable dt = new DataTable();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Proc_Equipo_Consulta_Imei";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@criterio", imei);
                da.Fill(dt);
                cn.Close();
                return dt;

            }
            catch (Exception)
            {
                throw;
            }
        }
        public string Equipo_Delete(string imei, string Usuario)
        {
            try
            {
                // Crear comando SQL
                using (SqlCommand cmd = new SqlCommand("Equipo_Delete", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar solo el par�metro de entrada @imei
                    cmd.Parameters.AddWithValue("@imei", imei);
                    cmd.Parameters.AddWithValue("@Usuario", Usuario);

                    // Crear el par�metro de salida para el mensaje
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output  // Definir como par�metro de salida
                    };
                    cmd.Parameters.Add(mensajeParam);  // Solo agregar el par�metro de salida

                    // Abrir la conexi�n
                    cn.Open();
                    cmd.ExecuteNonQuery();  // Ejecutar el procedimiento almacenado
                    cn.Close();

                    // Obtener el valor del mensaje de salida
                    string mensaje = mensajeParam.Value.ToString();

                    return mensaje;  // Retornar el mensaje que devuelve el procedimiento
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo", ex);
            }
        }
        public String Actualizar_CostoPrecio_Reparacion(ReparacionPendienteUpdateDTO data)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("Reparaciones_Pendiente_Update", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ID_Reparacion", data.ID_Reparacion);
                    cmd.Parameters.AddWithValue("@Costo", data.Costo);
                    cmd.Parameters.AddWithValue("@Precio", data.Precio);
                    cmd.Parameters.AddWithValue("@Usuario", data.Usuario);

                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(mensajeParam);
                    cn.Open();
                    cmd.ExecuteNonQuery();

                    return mensajeParam.Value.ToString(); // Retornar el mensaje que devuelve el procedimiento

                }

            }
            catch (Exception)
            {
                throw;
            }
        }
        public string AbonoAfactura(AbonoFacturaParam param)
        {
            try
            {
                // Crear comando SQL
                using (SqlCommand cmd = new SqlCommand("Proc_AbonoFactura", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar solo el par�metro de entrada @imei
                    cmd.Parameters.AddWithValue("@IdCliente", param.Id_Cliente);
                    cmd.Parameters.AddWithValue("@Numero_Factura", param.NumeroFactura);
                    cmd.Parameters.AddWithValue("@MontoAbono", param.MontoAbono);
                    cmd.Parameters.AddWithValue("@Id_Sucursal", param.IdSucursal);
                    cmd.Parameters.AddWithValue("@Usuario", param.Usuario);

                    // Crear el par�metro de salida para el mensaje
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output  // Definir como par�metro de salida
                    };
                    cmd.Parameters.Add(mensajeParam);  // Solo agregar el par�metro de salida

                    // Abrir la conexi�n
                    cn.Open();
                    cmd.ExecuteNonQuery();  // Ejecutar el procedimiento almacenado
                    cn.Close();

                    // Obtener el valor del mensaje de salida
                   // string mensaje = mensajeParam.Value.ToString();

                    return mensajeParam.Value.ToString(); // Retornar el mensaje que devuelve el procedimiento
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo", ex);
            }
        }
        public string AbonoGeneral(AbonoFacturaParam param)
        {
            try
            {
                // Crear comando SQL
                using (SqlCommand cmd = new SqlCommand("Proc_AbonoPorCliente", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Agregar solo el par�metro de entrada @imei
                    cmd.Parameters.AddWithValue("@IdCliente", param.Id_Cliente);
                    cmd.Parameters.AddWithValue("@MontoAbono", param.MontoAbono);
                    cmd.Parameters.AddWithValue("@Usuario", param.Usuario);

                    // Crear el par�metro de salida para el mensaje
                    SqlParameter mensajeParam = new SqlParameter("@mensaje", SqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output  // Definir como par�metro de salida
                    };
                    cmd.Parameters.Add(mensajeParam);  // Solo agregar el par�metro de salida

                    // Abrir la conexi�n
                    cn.Open();
                    cmd.ExecuteNonQuery();  // Ejecutar el procedimiento almacenado
                    cn.Close();

                    // Obtener el valor del mensaje de salida
                    // string mensaje = mensajeParam.Value.ToString();

                    return mensajeParam.Value.ToString(); // Retornar el mensaje que devuelve el procedimiento
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                throw new Exception("Error al intentar eliminar el equipo", ex);
            }
        }
        public void ModificarEquipo(Equipo_return_Imei equipo)
        {
            cn.Open();

            try
            {
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Equipo_Actualiza_Estado_y_precio";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Imei", equipo.Imei);
                cmd.Parameters.AddWithValue("@Id_Condicion", equipo.CondicionId);
                cmd.Parameters.AddWithValue("@Costo_Equipo", equipo.CostoEquipo);
                cmd.Parameters.AddWithValue("@Precio_Por_Mayor", equipo.PrecioPorMayor);
                cmd.Parameters.AddWithValue("@Precio_Detalle", equipo.PrecioDetalle);
                cmd.Parameters.AddWithValue("@Usuario",equipo.Usuario);
                cmd.Parameters.AddWithValue("@Comision_Detalle", equipo.ComisionDetalle);
                cmd.Parameters.AddWithValue("@Comision_Por_Mayor", equipo.ComisionMayor);
                cmd.Parameters.AddWithValue("@Id_estado_Bloqueo", equipo.EstadoBloqueoId);
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void InsertarEquipoRecibido(EquipoRecepcion equipo)
        {
            cn.Open();

            try
            {
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Recepcion_Equipos_Insert";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Nombres", equipo.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", equipo.Apellidos);
                cmd.Parameters.AddWithValue("@Cedula", equipo.Cedula);
                cmd.Parameters.AddWithValue("@Telefono", equipo.Telefono);
                cmd.Parameters.AddWithValue("@Imei_Entrada", equipo.ImeiEntrada);
                cmd.Parameters.AddWithValue("@Precio_Imei_Entra", equipo.PrecioImeiEntra);
                cmd.Parameters.AddWithValue("@ID_Modelo", equipo.IDModelo);
                cmd.Parameters.AddWithValue("@ID_Condicion", equipo.IDCondicion);
                cmd.Parameters.AddWithValue("@Imei_Sale", equipo.ImeiSale);
                cmd.Parameters.AddWithValue("@Nota", equipo.Nota);
                cmd.Parameters.AddWithValue("@Usuario", equipo.Usuario);
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void EquipoEstadoActualiza(EquipoEstadoUpdate dataActualizar)
        {
            cn.Open();

            try
            {
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Equipos_Actualiza_Disponible";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Imei", dataActualizar.Imei);
                cmd.Parameters.AddWithValue("@Disponible", dataActualizar.Disponible);
                cmd.Parameters.AddWithValue("@Nota_Adicional", dataActualizar.NotaAdicional);
                cmd.Parameters.AddWithValue("@Usuario",dataActualizar.Usuario) ;
                cmd.ExecuteNonQuery();
                cn.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable Marca_Insert(Marcas Marca)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Marcas_Insert";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Descripcion", Marca.descripcion);
                cmd.Parameters.AddWithValue("@Usuario", Marca.Usuario);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Modelo_Insert(Modelos Modelo)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Modelos_Insert";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Marca", Modelo.ID_Marca);
                cmd.Parameters.AddWithValue("@Descripcion", Modelo.Descripcion);
                cmd.Parameters.AddWithValue("@Estado", Modelo.Estado);
                cmd.Parameters.AddWithValue("@Usuario", Modelo.Usuario);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<FacturaReporte> ObtenerFacturaReporte(int codigoFactura, int sucursal)
        {
            using var cn = new SqlConnection(_connectionStrings);
            using var cmd = new SqlCommand("SP_Reporte_Factura", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            //cmd.CommandType = CommandType.StoredProcedure;
            //cmd.CommandText = "facturas_Insert";

            cmd.Parameters.AddWithValue("@Cod_Factura", codigoFactura);
            cmd.Parameters.AddWithValue("@sucursal", sucursal);
            cn.Open();

            var dt = new DataTable();
            da.Fill(dt);
            cn.Close();

            return BindList<FacturaReporte>(dt);
        }

        public DataTable GetUtilidades(int? sucursal, DateTime FI, DateTime FF)
        {
            try
            {
                using var cn = new SqlConnection(_connectionStrings);
                if (sucursal == 4)
                {
                    sucursal = null;
                }
                Utilidades gana = new Utilidades();
                cn.Open();
                //-------------Cargar ComboBox Vendedor------------------------------//

                SqlCommand comando = cn.CreateCommand();
                comando.CommandText = "Ver_Utilidades_Precio_Detalle";
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.AddWithValue("@Sucursal", sucursal);
                comando.Parameters.AddWithValue("@Fecha_Inicio", FI);
                comando.Parameters.AddWithValue("@Fecha_Final", FF);

                SqlDataAdapter da = new SqlDataAdapter(comando);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;

              
            }
            catch (Exception)
            {
                throw;

            }
        }

        public DataTable PostEquipoTransferencia(EquipoTransferencia tranferencia)
        {
            try
            {
                cn.Open();


                DataTable dt = new DataTable();
                SqlCommand cmd = new SqlCommand("Proc_Equipo_Transferencia_Trans_Guarda", cn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id_Transferencia", tranferencia.id);
                cmd.Parameters.AddWithValue("@Imei", tranferencia.Imei);
                cmd.Parameters.AddWithValue("@Almacen_Salida", tranferencia.AlmacenSalida);
                cmd.Parameters.AddWithValue("@Almacen_Destino", tranferencia.AlmacenDestino);
                cmd.Parameters.AddWithValue("@Modelo", tranferencia.Modelo);
                cmd.Parameters.AddWithValue("@Cantidad_Equipos", tranferencia.CantidadEquipos);
                cmd.Parameters.AddWithValue("@Usuario", tranferencia.Usuario);
                //cmd.Parameters.AddWithValue("@Usuario", tranferencia.Usuario);

                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public DataTable Reparaciones_Insert(ReparacionesInsert Reparaciones)
        {
            DataTable dt = new DataTable();
            try
            {
                if (cn.State != ConnectionState.Open)
                    cn.Open();

                using (var cmd = new SqlCommand("Proc_Reparaciones_Insert", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Imei", Reparaciones.IMEI);
                    cmd.Parameters.AddWithValue("@ID_Cliente", Reparaciones.ID_Cliente);
                    cmd.Parameters.AddWithValue("@Detalle_Reparacion", Reparaciones.Detalle_Reparacion); 
                    cmd.Parameters.AddWithValue("@Observaciones", Reparaciones.Observaciones); 
                    cmd.Parameters.AddWithValue("@Costo", Reparaciones.Costo);
                    cmd.Parameters.AddWithValue("@Precio", Reparaciones.Precio);
                    cmd.Parameters.AddWithValue("@Fecha_Entrega", DateTime.Now);
                    cmd.Parameters.AddWithValue("@modelo", Reparaciones.ID_Modelo);
                    cmd.Parameters.AddWithValue("@Usuario", Reparaciones.Usuario);

                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return dt;
        }

        public DataTable GetReparacionesLista(ReparacionConsultaFiltro Filtros)
        {
            cn.Open();

            try
            {
                DataTable dt = new DataTable();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                cmd.CommandText = "Reparaciones_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Reparacion", Filtros.ID_Reparacion);
                cmd.Parameters.AddWithValue("@Criterio", Filtros.Criterio ?? "");
                cmd.Parameters.AddWithValue("@Estatus", Filtros.Estatus);
                cmd.Parameters.AddWithValue("@FechaInicio", (object)Filtros.FechaInicio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaFinal", (object)Filtros.FechaFinal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PageIndex", Filtros.PageIndex);
                cmd.Parameters.AddWithValue("@PageSize", Filtros.PageSize);
                da.Fill(dt);
                cn.Close();
                return dt;

            }
            catch (Exception)
            {
                throw;
            }
        }

        public ReparacionReporteDTO ObtenerReparacionReporte(int idReparacion)
        {
            using var cn = new SqlConnection(_connectionStrings);
            using var cmd = new SqlCommand("SP_Reporte_Reparacion", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdReparacion", idReparacion);

            cn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new ReparacionReporteDTO
                {
                    IdReparacion = reader.GetInt64(reader.GetOrdinal("ID_Reparacion")),
                    Imei = reader.IsDBNull(reader.GetOrdinal("Imei")) ? null : reader.GetString(reader.GetOrdinal("Imei")),
                    NombreCliente = reader.GetString(reader.GetOrdinal("NombreCliente")),
                    ApellidoCliente = reader.GetString(reader.GetOrdinal("ApellidoCliente")),
                    Detalle_Reparacion = reader.GetString(reader.GetOrdinal("Detalle_Reparacion")),
                    Observaciones = reader.GetString(reader.GetOrdinal("Observaciones")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Costo")),
                    Precio = reader.GetDecimal(reader.GetOrdinal("Precio")),
                    Fecha_Entrega = reader.GetDateTime(reader.GetOrdinal("Fecha_Entrega")),
                    Usuario = reader.GetString(reader.GetOrdinal("Usuario")),
                    Modelo = reader.GetString(reader.GetOrdinal("Modelo"))
                };
            }
            return null;
        }

        public DataTable GetPreFacturasConsulta(PreFacturaFiltro filtro)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "PreFacturas_Consultar";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Estado", (object)filtro.Estado ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Sucursal", (object)filtro.ID_Sucursal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Cliente", (object)filtro.ID_Cliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Criterio", (object)filtro.Criterio ?? DBNull.Value);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable GetPreDetalleFacturaConsulta(int numeroPreFactura)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "PreDetalle_Factura_Consultar";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_PreFactura", numeroPreFactura);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable InsertarPreDetalleFactura(PreDetalleFactura detalle)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "PreDetalle_Factura_Insert";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_PreFactura", detalle.Numero_PreFactura);
                cmd.Parameters.AddWithValue("@ID_Equipo", detalle.ID_Equipo);
                cmd.Parameters.AddWithValue("@Descripcion", detalle.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Tipo", detalle.ID_Tipo);
                cmd.Parameters.AddWithValue("@ID_Garantia", detalle.ID_Garantia);
                cmd.Parameters.AddWithValue("@cantidad", detalle.cantidad ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@precio", detalle.precio ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SubTotal", detalle.SubTotal);
                cmd.Parameters.AddWithValue("@Descuento", detalle.Descuento ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Itbis", detalle.Itbis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Total", detalle.Total);
                cmd.Parameters.AddWithValue("@Usuario", detalle.Usuario);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public object ProcesarPreFactura(int numeroPreFactura, string usuarioProcesamiento)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Procesar_PreFactura";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_PreFactura", numeroPreFactura);
                cmd.Parameters.AddWithValue("@Usuario_Procesamiento", usuarioProcesamiento);
                var nuevoNumeroFacturaParam = new SqlParameter("@Nuevo_Numero_Factura", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(nuevoNumeroFacturaParam);
                var mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(mensajeParam);
                cmd.ExecuteNonQuery();
                cn.Close();
                return new {
                    Cod_Factura = nuevoNumeroFacturaParam.Value,
                    Mensaje = mensajeParam.Value
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable AnularPreFactura(int numeroPreFactura)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "PreFacturas_Anular";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_PreFactura", numeroPreFactura);
                cmd.Parameters.AddWithValue("@Usuario_Anulacion", "suser_sname()" /* Puedes cambiar por usuario real */);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable InsertarPreFactura(PreFactura prefactura)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "PreFacturas_Insert";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_PreFactura", prefactura.Numero_PreFactura);
                cmd.Parameters.AddWithValue("@ID_Cliente", prefactura.ID_Cliente);
                cmd.Parameters.AddWithValue("@ID_Tipo_Factura", prefactura.ID_Tipo_Factura);
                cmd.Parameters.AddWithValue("@ID_Vendedor", prefactura.ID_Vendedor ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Tipo_Pagos", prefactura.ID_Tipo_Pagos ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SubTotal", prefactura.SubTotal);
                cmd.Parameters.AddWithValue("@Descuento", prefactura.Descuento ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Itbis", prefactura.Itbis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Abono", prefactura.Abono ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Tiempo_Credito", prefactura.Tiempo_Credito ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Total", prefactura.Total);
                cmd.Parameters.AddWithValue("@Cantidad_Articulos", prefactura.Cantidad_Articulos);
                cmd.Parameters.AddWithValue("@Nota", prefactura.Nota ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Estado", prefactura.Estado ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Usuario", prefactura.Usuario ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ID_Sucursal", prefactura.ID_Sucursal);
                cmd.Parameters.AddWithValue("@NFCTipoNumero", prefactura.NFCTipoNumero ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NFCFISCAL", prefactura.NFCFISCAL ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NCF_NUMERO", prefactura.NCF_NUMERO ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@RNC", prefactura.RNC ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Literal", prefactura.Literal ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Cotizacion_Numero", prefactura.Cotizacion_Numero);
                cmd.Parameters.AddWithValue("@Fecha_Registro", prefactura.Fecha_Registro ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Fecha_Procesamiento", prefactura.Fecha_Procesamiento ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Usuario_Procesamiento", prefactura.Usuario_Procesamiento ?? (object)DBNull.Value);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();
                return dt;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public object RebajarItemPreFactura(int numeroPreFactura, int idEquipo)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "PreDetalle_Factura_Eliminar";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_PreFactura", numeroPreFactura);
                cmd.Parameters.AddWithValue("@ID_Equipo", idEquipo);
                var mensajeParam = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 200)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(mensajeParam);
                cmd.ExecuteNonQuery();
                cn.Close();
                return new { Mensaje = mensajeParam.Value };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void PostDetallesPagoFactura(int numeroFactura, List<DetallePagoDTO> detallesPagos)
        {
            Console.WriteLine($"🗄️ AccesoDatos.PostDetallesPagoFactura - Factura: {numeroFactura}, Detalles: {detallesPagos?.Count ?? 0}");
            
            using var connection = new SqlConnection(_connectionStrings);
            using var cmd = new SqlCommand("InsertDetallesPagoFactura", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            connection.Open();
            Console.WriteLine($"📡 Conexión abierta a la BD");

            foreach (var item in detallesPagos)
            {
                Console.WriteLine($"   Insertando: TipoPago={item.ID_Tipo_Pago}, Monto={item.Monto_Pago}, Ref={item.Numero_Referencia}");
                
                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("@Numero_Factura", numeroFactura);
                cmd.Parameters.AddWithValue("@ID_Tipo_Pago", item.ID_Tipo_Pago);
                cmd.Parameters.AddWithValue("@Monto_Pago", item.Monto_Pago);
                cmd.Parameters.AddWithValue("@Numero_Referencia", item.Numero_Referencia ?? "");
                cmd.Parameters.AddWithValue("@Observacion", item.Observacion ?? "");

                try
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"   ✅ Detalle insertado");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error: {ex.Message}");
                    throw;
                }
            }
            Console.WriteLine($"✅ PostDetallesPagoFactura completado");
        }

        public List<DetallePagoDTO> GetDetallesPagoFactura(int numeroFactura)
        {
            var lista = new List<DetallePagoDTO>();

            using var connection = new SqlConnection(_connectionStrings);
            using var cmd = new SqlCommand("GetDetallesPagoFactura", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NumeroFactura", numeroFactura);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new DetallePagoDTO
                {
                    ID_Tipo_Pago = reader.GetInt32(reader.GetOrdinal("ID_Tipo_Pago")),
                    Descripcion_Tipo_Pago = reader["Descripcion_Tipo_Pagos"]?.ToString(),
                    Monto_Pago = reader.GetDecimal(reader.GetOrdinal("Monto_Pago")),
                    Numero_Referencia = reader["Numero_Referencia"]?.ToString(),
                    Observacion = reader["Observacion"]?.ToString()
                });
            }

            return lista;
        }

        #region M�todos As�ncronos para Tracking

        public async Task<T> ExecuteScalarAsync<T>(string query, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionStrings);
            using var command = new SqlCommand(query, connection);
            if (parameters != null) command.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? default(T) : (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<DataTable> ExecuteDataTableAsync(string query, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionStrings);
            using var command = new SqlCommand(query, connection);
            if (parameters != null) command.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            using var adapter = new SqlDataAdapter(command);
            var dataTable = new DataTable();
            await Task.Run(() => adapter.Fill(dataTable));
            return dataTable;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionStrings);
            using var command = new SqlCommand(query, connection);
            if (parameters != null) command.Parameters.AddRange(parameters);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        #endregion
    }
}

