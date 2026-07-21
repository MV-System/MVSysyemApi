using DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
        private readonly ILogger<AccesoDatos> _logger;
        private readonly ISyncService _sync;
        private readonly string _seguridadConnectionString;
        private readonly string _proveedor2ConnectionString;
        public static Int64 codigo;

        // Propiedad pública para acceder a la cadena de conexión
        public string ConnectionString => _connectionStrings;

        public AccesoDatos(IHttpContextAccessor httpContextAccessor, SeguridadService seguridad, IMemoryCache memoryCache, ILogger<AccesoDatos> logger, ISyncService sync, IConfiguration configuration)
        {
            _memoryCache = memoryCache;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sync = sync;
            _seguridadConnectionString = configuration.GetConnectionString("MVSystemSeguridad") ?? string.Empty;
            _proveedor2ConnectionString = configuration.GetConnectionString("Proveedor2MicroStar") ?? string.Empty;
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

        public int GuardarCotizacion(CotizacionModel model)
        {
            cn.Open();
            SqlTransaction tran = cn.BeginTransaction();
            try
            {
                // Insertar cabecera de cotización
                using (SqlCommand cmd = new SqlCommand("Cotizacion_Insert", cn, tran))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Numero_factura", 0);
                    cmd.Parameters.AddWithValue("@ID_Cliente", model.IdCliente);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Factura", model.IdTipoFactura);
                    cmd.Parameters.AddWithValue("@ID_Vendedor", model.IdVendedor);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Pagos", model.IdTipoPago);
                    cmd.Parameters.AddWithValue("@ID_Almacen", model.IdAlmacen);
                    cmd.Parameters.AddWithValue("@SubTotal", model.SubTotal);
                    cmd.Parameters.AddWithValue("@Descuento", model.Descuento);
                    cmd.Parameters.AddWithValue("@Itbis", model.Itbis);
                    cmd.Parameters.AddWithValue("@NFCTipoNumero", model.NcfTipoNumero ?? "");
                    cmd.Parameters.AddWithValue("@RNC", model.Rnc ?? "");
                    cmd.Parameters.AddWithValue("@Abono", model.Abono);
                    cmd.Parameters.AddWithValue("@Tiempo_Credito", string.IsNullOrEmpty(model.FechaLimitePago) ? (object)DBNull.Value : DateTime.Parse(model.FechaLimitePago));
                    cmd.Parameters.AddWithValue("@Total", model.Total);
                    cmd.Parameters.AddWithValue("@Cantidad_Articulos", model.CantidadArticulos);
                    cmd.Parameters.AddWithValue("@Nota", model.Nota ?? "");
                    cmd.Parameters.AddWithValue("@Estado", "A");
                    cmd.Parameters.AddWithValue("@Usuario", model.Usuario ?? "");
                    cmd.Parameters.AddWithValue("@fecha_registro", DateTime.Now);

                    var da = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);

                    int numeroCotizacion = 0;
                    if (dt.Rows.Count > 0)
                        numeroCotizacion = Convert.ToInt32(dt.Rows[0][0]);

                    if (numeroCotizacion == 0)
                    {
                        tran.Rollback();
                        return 0;
                    }

                    foreach (var art in model.Articulos)
                    {
                        using (SqlCommand cmdDet = new SqlCommand("Detalle_Cotizacion_Insert", cn, tran))
                        {
                            cmdDet.CommandType = CommandType.StoredProcedure;
                            cmdDet.Parameters.AddWithValue("@Numero_Factura", numeroCotizacion);
                            cmdDet.Parameters.AddWithValue("@ID_Equipo", art.IdEquipo.ToString());
                            cmdDet.Parameters.AddWithValue("@Descripcion", art.Descripcion ?? "");
                            cmdDet.Parameters.AddWithValue("@ID_Tipo", art.IdTipo);
                            cmdDet.Parameters.AddWithValue("@ID_Garantia", art.IdGarantia);
                            cmdDet.Parameters.AddWithValue("@ID_Tipo_Factura", model.IdTipoFactura);
                            cmdDet.Parameters.AddWithValue("@ID_Vendedor", model.IdVendedor);
                            cmdDet.Parameters.AddWithValue("@SubTotal", art.SubTotal);
                            cmdDet.Parameters.AddWithValue("@cantidad", art.Cantidad);
                            cmdDet.Parameters.AddWithValue("@precio", art.Precio);
                            cmdDet.Parameters.AddWithValue("@Descuento", art.Descuento);
                            cmdDet.Parameters.AddWithValue("@Itbis", art.Itbis);
                            cmdDet.Parameters.AddWithValue("@Total", art.Total);
                            cmdDet.Parameters.AddWithValue("@Estado", "A");
                            cmdDet.Parameters.AddWithValue("@Usuario", model.Usuario ?? "");
                            cmdDet.Parameters.AddWithValue("@fecha_registro", DateTime.Now);
                            cmdDet.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                    _logger.LogInformation("[GuardarCotizacion] Cotización #{Num} guardada con {Art} artículos", numeroCotizacion, model.Articulos.Count);
                    return numeroCotizacion;
                }
            }
            catch (Exception ex)
            {
                tran.Rollback();
                _logger.LogError(ex, "[GuardarCotizacion] Error al guardar cotización");
                throw;
            }
            finally
            {
                if (cn.State == ConnectionState.Open) cn.Close();
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
        public DataTable GetDetalleFacturaConsulta(int numeroFactura, int? sucursal)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Detalle_Factura_Consulta";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@numeroFactura", numeroFactura);
                cmd.Parameters.AddWithValue("@sucursal", sucursal ?? 1);

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
                    ;WITH FacturasPeriodo AS (
                        SELECT
                            F.ID_Tipo_Pagos,
                            COUNT(DISTINCT F.Numero_Factura) AS CantidadFacturas,
                            COUNT(DISTINCT CASE
                                WHEN F.Abono > 0 THEN F.Numero_Factura
                            END) AS CantidadAbonosRecibidosBase,
                            SUM(F.SubTotal) AS SubTotal,
                            SUM(F.Descuento) AS Descuento,
                            SUM(F.Itbis) AS Itbis,
                            SUM(F.Total) AS TotalFacturado,
                            SUM(CASE
                                WHEN F.ID_Tipo_Pagos = 1 THEN F.Total
                                ELSE ISNULL(F.Abono, 0)
                            END) AS EfectivoRealBase,
                            SUM(CASE
                                WHEN F.ID_Tipo_Pagos = 2 THEN F.Total - ISNULL(F.Abono, 0)
                                ELSE 0
                            END) AS SaldoPendiente
                        FROM Facturas F
                        WHERE CAST(F.Fecha_Registro AS DATE) BETWEEN CAST(@Fecha_Inicio AS DATE) AND CAST(@Fecha_Final AS DATE)
                            AND F.ID_Sucursal = @Almacen
                            AND F.Estado = @EstadoFactura
                            AND (@IdVendedor = 0 OR F.ID_Vendedor = @IdVendedor)
                        GROUP BY F.ID_Tipo_Pagos
                    ),
                    AbonosPeriodo AS (
                        SELECT
                            COUNT(DISTINCT TRY_CONVERT(INT, hp.Numero_factura)) AS CantidadAbonosRecibidos,
                            SUM(ISNULL(TRY_CONVERT(DECIMAL(18, 2), hp.Total_Abono), 0)) AS TotalAbonos
                        FROM Historio_Pagos hp
                        INNER JOIN Facturas fRel
                            ON fRel.ID_Sucursal = hp.Id_Sucursal
                            AND fRel.Numero_Factura = TRY_CONVERT(INT, hp.Numero_factura)
                        WHERE CAST(hp.Fecha_registro AS DATE) BETWEEN CAST(@Fecha_Inicio AS DATE) AND CAST(@Fecha_Final AS DATE)
                            AND hp.Id_Sucursal = @Almacen
                            AND (@IdVendedor = 0 OR fRel.ID_Vendedor = @IdVendedor)
                            AND fRel.ID_Tipo_Pagos = 2
                    )
                    SELECT
                        fp.ID_Tipo_Pagos,
                        fp.CantidadFacturas,
                        CASE
                            WHEN fp.ID_Tipo_Pagos = 2 THEN ISNULL(ap.CantidadAbonosRecibidos, 0)
                            ELSE fp.CantidadAbonosRecibidosBase
                        END AS CantidadAbonosRecibidos,
                        fp.SubTotal,
                        fp.Descuento,
                        fp.Itbis,
                        fp.TotalFacturado,
                        CASE
                            WHEN fp.ID_Tipo_Pagos = 2 THEN ISNULL(ap.TotalAbonos, 0)
                            ELSE fp.EfectivoRealBase
                        END AS EfectivoReal,
                        fp.SaldoPendiente
                    FROM FacturasPeriodo fp
                    CROSS JOIN AbonosPeriodo ap

                    UNION ALL

                    SELECT
                        2 AS ID_Tipo_Pagos,
                        0 AS CantidadFacturas,
                        ISNULL(ap.CantidadAbonosRecibidos, 0) AS CantidadAbonosRecibidos,
                        CAST(0 AS DECIMAL(18, 2)) AS SubTotal,
                        CAST(0 AS DECIMAL(18, 2)) AS Descuento,
                        CAST(0 AS DECIMAL(18, 2)) AS Itbis,
                        CAST(0 AS DECIMAL(18, 2)) AS TotalFacturado,
                        ISNULL(ap.TotalAbonos, 0) AS EfectivoReal,
                        CAST(0 AS DECIMAL(18, 2)) AS SaldoPendiente
                    FROM AbonosPeriodo ap
                    WHERE ISNULL(ap.TotalAbonos, 0) > 0
                        AND NOT EXISTS (
                            SELECT 1
                            FROM FacturasPeriodo fp
                            WHERE fp.ID_Tipo_Pagos = 2
                        )

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
                _sync.Ejecutar("Accesorios_Modifica", cmd.Parameters);

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
                _sync.Ejecutar("Accesorio_Insert", cmd.Parameters);

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
            _sync.Ejecutar("Suplidores_Insert", cmd.Parameters);
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
                _sync.Ejecutar("Proc_Equipos_Insert", cmd.Parameters);
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

        public DataTable GetEquiposRegistrados(int numeroRegistro)
        {
            try
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                SqlDataAdapter da = new(cmd);
                DataTable dt = new();

                cmd.CommandText = "Sp_Rpt_Equipos_Registro";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Numero_Registro", numeroRegistro);
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

                    cmd.ExecuteNonQuery();
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
        public Facturas PostFacturaConPagos(Facturas factura, List<DetallePagoDTO> detallesPagos)
        {
            using var connection = new SqlConnection(_connectionStrings);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insertar factura
                using (var cmd = new SqlCommand("facturas_Insert", connection, transaction))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID_Cliente", factura.IdCliente);
                    cmd.Parameters.AddWithValue("@ID_Vendedor", factura.IdVendedor);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Factura", factura.IdTipoFactura);
                    cmd.Parameters.AddWithValue("@ID_Tipo_Pagos", factura.IdTipoPago);
                    cmd.Parameters.AddWithValue("@ID_Almacen", factura.IdAlmacen);
                    cmd.Parameters.AddWithValue("@Cantidad_Articulos", factura.CantidadArticulos);
                    cmd.Parameters.AddWithValue("@Itbis", factura.Itbis);
                    cmd.Parameters.AddWithValue("@NFCTipoNumero", factura.NcfTipoNumero);
                    cmd.Parameters.AddWithValue("@RNC", factura.RNC);
                    cmd.Parameters.AddWithValue("@SubTotal", factura.SubTotal);
                    cmd.Parameters.AddWithValue("@Abono", factura.Abono);
                    cmd.Parameters.AddWithValue("@Tiempo_Credito", factura.fechaLimitePago);
                    cmd.Parameters.AddWithValue("@Total", factura.Total);
                    cmd.Parameters.AddWithValue("@Descuento", factura.Descuento);
                    cmd.Parameters.AddWithValue("@Nota", factura.Nota);
                    cmd.Parameters.AddWithValue("@Estado", "A");
                    cmd.Parameters.AddWithValue("@Usuario", factura.Usuario);
                    cmd.Parameters.AddWithValue("@fecha_registro", factura.FechaRegistro);

                    var numeroFacturaParam = new SqlParameter("@Numero_factura", 0)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(numeroFacturaParam);
                    cmd.ExecuteNonQuery();
                    factura.NumeroFactura = Convert.ToInt32(numeroFacturaParam.Value.ToString());
                }

                // 2. Insertar detalle factura (artículos)
                if (factura.DetalleFacturaList != null && factura.DetalleFacturaList.Count > 0)
                {
                    using var cmdDetalle = new SqlCommand("Detalle_Factura_Insert", connection, transaction);
                    cmdDetalle.CommandType = CommandType.StoredProcedure;

                    foreach (var item in factura.DetalleFacturaList)
                    {
                        cmdDetalle.Parameters.Clear();
                        cmdDetalle.Parameters.AddWithValue("@Numero_Factura", factura.NumeroFactura);
                        cmdDetalle.Parameters.AddWithValue("@ID_Equipo", item.IdEquipo);
                        cmdDetalle.Parameters.AddWithValue("@Descripcion", item.Descripcion);
                        cmdDetalle.Parameters.AddWithValue("@ID_Tipo", item.IdTipo);
                        cmdDetalle.Parameters.AddWithValue("@ID_Garantia", item.IdGarantia);
                        cmdDetalle.Parameters.AddWithValue("@ID_Tipo_Factura", item.IdTipoFactura);
                        cmdDetalle.Parameters.AddWithValue("@ID_Vendedor", item.IdVendedor);
                        cmdDetalle.Parameters.AddWithValue("@SubTotal", item.SubTotal);
                        cmdDetalle.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdDetalle.Parameters.AddWithValue("@precio", item.Precio);
                        cmdDetalle.Parameters.AddWithValue("@Descuento", item.Descuento);
                        cmdDetalle.Parameters.AddWithValue("@Itbis", item.Itbis);
                        cmdDetalle.Parameters.AddWithValue("@Total", item.Total);
                        cmdDetalle.Parameters.AddWithValue("@Estado", "A");
                        cmdDetalle.Parameters.AddWithValue("@Usuario", item.Usuario);
                        cmdDetalle.Parameters.AddWithValue("@fecha_registro", item.FechaRegistro);
                        cmdDetalle.ExecuteNonQuery();
                    }
                }

                // 3. Insertar detalles de pago
                if (detallesPagos != null && detallesPagos.Count > 0)
                {
                    using var cmdPago = new SqlCommand("InsertDetallesPagoFactura", connection, transaction);
                    cmdPago.CommandType = CommandType.StoredProcedure;

                    foreach (var item in detallesPagos)
                    {
                        cmdPago.Parameters.Clear();
                        cmdPago.Parameters.AddWithValue("@Numero_Factura", factura.NumeroFactura);
                        cmdPago.Parameters.AddWithValue("@ID_Tipo_Pago", item.ID_Tipo_Pago);
                        cmdPago.Parameters.AddWithValue("@Monto_Pago", item.Monto_Pago);
                        cmdPago.Parameters.AddWithValue("@Numero_Referencia", item.Numero_Referencia ?? "");
                        cmdPago.Parameters.AddWithValue("@Observacion", item.Observacion ?? "");
                        cmdPago.ExecuteNonQuery();
                    }
                }

                transaction.Commit();

                // Sync: marcar equipos como no disponibles en BD espejo
                if (factura.DetalleFacturaList != null)
                {
                    foreach (var item in factura.DetalleFacturaList)
                    {
                        _sync.EjecutarQuery(
                            "UPDATE Equipos SET Disponible = 'N', Nota_Adicional = 'inverso' WHERE IMEI = @IMEI",
                            new SqlParameter("@IMEI", item.IdEquipo));
                    }
                }

                return factura;
            }
            catch
            {
                transaction.Rollback();
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

                    // Sync: leer equipos de la factura y marcarlos disponibles en BD espejo
                    using (var cmdRead = new SqlCommand(
                        "SELECT ID_Equipo FROM Detalle_Facturas WHERE Numero_Factura = @nf", cn))
                    {
                        cmdRead.Parameters.AddWithValue("@nf", Factura.NumeroFactura);
                        using var reader = cmdRead.ExecuteReader();
                        var imeis = new List<string>();
                        while (reader.Read())
                            imeis.Add(reader["ID_Equipo"].ToString());
                        reader.Close();

                        foreach (var imei in imeis)
                        {
                            _sync.EjecutarQuery(
                                "UPDATE Equipos SET Disponible = 'S', Nota_Adicional = 'inverso' WHERE IMEI = @IMEI",
                                new SqlParameter("@IMEI", imei));
                        }
                    }

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
                    cmd.ExecuteNonQuery();
                    cn.Close();

                    // Sync: IMEI que regresa → disponible
                    _sync.EjecutarQuery(
                        "UPDATE Equipos SET Disponible = 'S', Nota_Adicional = 'inverso' WHERE IMEI = @IMEI",
                        new SqlParameter("@IMEI", EditaDetalleFactura.EquipoActual));

                    // Sync: IMEI nuevo que sale → no disponible
                    _sync.EjecutarQuery(
                        "UPDATE Equipos SET Disponible = 'N', Nota_Adicional = 'inverso' WHERE IMEI = @IMEI",
                        new SqlParameter("@IMEI", EditaDetalleFactura.EquipoNuevo));

                    string mensaje = mensajeParam.Value.ToString();

                    return mensaje;
                }
            }
            catch (Exception ex)
            {
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
                        string mensaje;
                        if (reader.Read())
                            mensaje = reader["mensaje"].ToString();
                        else
                            mensaje = "Equipo rebajado correctamente.";

                        reader.Close();
                        // Sync: marcar equipo como disponible en BD espejo
                        _sync.EjecutarQuery(
                            "UPDATE Equipos SET Disponible = 'S', Nota_Adicional = 'inverso' WHERE IMEI = @IMEI",
                            new SqlParameter("@IMEI", model.imei));
                        return mensaje;
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

                    _sync.Ejecutar("Equipo_Delete", cmd.Parameters);

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
                _sync.Ejecutar("Equipo_Actualiza_Estado_y_precio", cmd.Parameters);
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
                _sync.Ejecutar("Equipos_Actualiza_Disponible", cmd.Parameters);
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
                _sync.Ejecutar("Marcas_Insert", cmd.Parameters);
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
                _logger.LogInformation("[Modelo_Insert] Insertando modelo: Marca={Marca}, Desc={Desc}, Estado={Estado}, Usuario={Usuario}",
                    Modelo.ID_Marca, Modelo.Descripcion, Modelo.Estado, Modelo.Usuario);

                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = "Modelos_Insert";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_Marca", Modelo.ID_Marca);
                cmd.Parameters.AddWithValue("@Descripcion", Modelo.Descripcion);
                cmd.Parameters.AddWithValue("@Estado", Modelo.Estado ?? "A");
                cmd.Parameters.AddWithValue("@Usuario", Modelo.Usuario);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cn.Close();

                _logger.LogInformation("[Modelo_Insert] Modelo insertado en A exitosamente. Filas retornadas: {Rows}", dt.Rows.Count);
                _logger.LogInformation("[Modelo_Insert] Iniciando sync a BD espejo...");

                _sync.Ejecutar("Modelos_Insert", cmd.Parameters);

                _logger.LogInformation("[Modelo_Insert] Sync completado.");
                return dt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Modelo_Insert] Error al insertar modelo");
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

                da.Fill(dt);
                cn.Close();
                _sync.Ejecutar("Proc_Equipo_Transferencia_Trans_Guarda", cmd.Parameters);
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

                // Sync: leer IMEIs con conexión separada ANTES de ejecutar el SP
                var imeisSync = new List<string>();
                try
                {
                    using (var cnSync = new SqlConnection(_connectionStrings))
                    {
                        cnSync.Open();
                        using (var cmdImeis = new SqlCommand(
                            "SELECT ID_Equipo FROM PreDetalle_Facturas WHERE Numero_PreFactura = @nPre AND ID_Tipo = 1 AND Estado = 'P'", cnSync))
                        {
                            cmdImeis.Parameters.AddWithValue("@nPre", numeroPreFactura);
                            using (var reader = cmdImeis.ExecuteReader())
                            {
                                while (reader.Read())
                                    imeisSync.Add(reader["ID_Equipo"].ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }

                cmd.ExecuteNonQuery();

                // Sync: marcar equipos como no disponibles en BD espejo
                int nuevoNumero = nuevoNumeroFacturaParam.Value != DBNull.Value ? (int)nuevoNumeroFacturaParam.Value : 0;
                if (nuevoNumero > 0 && imeisSync.Count > 0)
                {
                    try
                    {
                        foreach (var imei in imeisSync)
                        {
                            _sync.EjecutarQuery(
                                "UPDATE Equipos SET Disponible = 'N', Nota_Adicional = 'inverso' WHERE IMEI = @IMEI",
                                new SqlParameter("@IMEI", imei));
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

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
            using var connection = new SqlConnection(_connectionStrings);
            using var cmd = new SqlCommand("InsertDetallesPagoFactura", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            connection.Open();

            foreach (var item in detallesPagos)
            {
                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("@Numero_Factura", numeroFactura);
                cmd.Parameters.AddWithValue("@ID_Tipo_Pago", item.ID_Tipo_Pago);
                cmd.Parameters.AddWithValue("@Monto_Pago", item.Monto_Pago);
                cmd.Parameters.AddWithValue("@Numero_Referencia", item.Numero_Referencia ?? "");
                cmd.Parameters.AddWithValue("@Observacion", item.Observacion ?? "");

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
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

        #region // Métodos para Facturación Electrónica (LogFacturaElectronica)

        /// <summary>
        /// Inserta un nuevo registro en LogFacturaElectronica usando Stored Procedure
        /// </summary>
        public async Task<bool> InsertLogFacturaElectronica(LogFacturaElectronica log)
        {
            try
            {
                using var connection = new SqlConnection(_connectionStrings);
                using var command = new SqlCommand("Proc_LogFacturaElectronica_Insert", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@Numero_Factura", log.Numero_Factura);
                command.Parameters.AddWithValue("@Literal", log.Literal ?? "A");
                command.Parameters.AddWithValue("@DocumentoId_EasyConexion", log.DocumentoId_EasyConexion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@VoucherNumber_DGII", log.VoucherNumber_DGII ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Estado", log.Estado ?? "PEND");
                command.Parameters.AddWithValue("@Respuesta_JSON", log.Respuesta_JSON ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Fecha_Envio", log.Fecha_Envio);
                command.Parameters.AddWithValue("@Error_Descripcion", log.Error_Descripcion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Usuario", log.Usuario ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@QR_Url", log.QR_Url ?? (object)DBNull.Value);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en InsertLogFacturaElectronica: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un registro en LogFacturaElectronica usando Stored Procedure
        /// </summary>
        public async Task<bool> UpdateLogFacturaElectronica(LogFacturaElectronica log)
        {
            try
            {
                using var connection = new SqlConnection(_connectionStrings);
                using var command = new SqlCommand("Proc_LogFacturaElectronica_Update", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@Numero_Factura", log.Numero_Factura);
                command.Parameters.AddWithValue("@Literal", log.Literal ?? "A");
                command.Parameters.AddWithValue("@DocumentoId_EasyConexion", log.DocumentoId_EasyConexion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@VoucherNumber_DGII", log.VoucherNumber_DGII ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Estado", log.Estado ?? "PEND");
                command.Parameters.AddWithValue("@Respuesta_JSON", log.Respuesta_JSON ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Fecha_Confirmacion", log.Fecha_Confirmacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Error_Descripcion", log.Error_Descripcion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@QR_Url", log.QR_Url ?? (object)DBNull.Value);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en UpdateLogFacturaElectronica: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene el log de una factura específica usando Stored Procedure
        /// </summary>
        public async Task<LogFacturaElectronica> GetLogFacturaElectronica(int numeroFactura, string literal)
        {
            try
            {
                using var connection = new SqlConnection(_connectionStrings);
                using var command = new SqlCommand("Proc_LogFacturaElectronica_GetByNumeroFacturaLiteral", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@Numero_Factura", numeroFactura);
                command.Parameters.AddWithValue("@Literal", literal ?? "A");

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new LogFacturaElectronica
                    {
                        IDLog = reader.GetInt32(reader.GetOrdinal("ID_Log")),
                        Numero_Factura = reader.GetInt32(reader.GetOrdinal("Numero_Factura")),
                        Literal = reader.GetString(reader.GetOrdinal("Literal")),
                        DocumentoId_EasyConexion = reader.IsDBNull(reader.GetOrdinal("DocumentoId_EasyConexion")) ? null : reader.GetInt32(reader.GetOrdinal("DocumentoId_EasyConexion")),
                        VoucherNumber_DGII = reader.IsDBNull(reader.GetOrdinal("VoucherNumber_DGII")) ? null : reader.GetString(reader.GetOrdinal("VoucherNumber_DGII")),
                        Estado = reader.GetString(reader.GetOrdinal("Estado")),
                        Respuesta_JSON = reader.IsDBNull(reader.GetOrdinal("Respuesta_JSON")) ? null : reader.GetString(reader.GetOrdinal("Respuesta_JSON")),
                        Fecha_Envio = reader.GetDateTime(reader.GetOrdinal("Fecha_Envio")),
                        Fecha_Confirmacion = reader.IsDBNull(reader.GetOrdinal("Fecha_Confirmacion")) ? null : reader.GetDateTime(reader.GetOrdinal("Fecha_Confirmacion")),
                        Error_Descripcion = reader.IsDBNull(reader.GetOrdinal("Error_Descripcion")) ? null : reader.GetString(reader.GetOrdinal("Error_Descripcion")),
                        Usuario = reader.IsDBNull(reader.GetOrdinal("Usuario")) ? null : reader.GetString(reader.GetOrdinal("Usuario")),
                        Fecha_Registro = reader.GetDateTime(reader.GetOrdinal("Fecha_Registro")),
                        QR_Url = reader.IsDBNull(reader.GetOrdinal("QR_Url")) ? null : reader.GetString(reader.GetOrdinal("QR_Url"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en GetLogFacturaElectronica: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las facturas con estado PEND usando Stored Procedure
        /// </summary>
        public async Task<List<LogFacturaElectronica>> GetLogFacturasElectronicasPendientes()
        {
            try
            {
                var logs = new List<LogFacturaElectronica>();

                using var connection = new SqlConnection(_connectionStrings);
                using var command = new SqlCommand("Proc_LogFacturaElectronica_GetPendientes", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    logs.Add(new LogFacturaElectronica
                    {
                        IDLog = reader.GetInt32(reader.GetOrdinal("ID_Log")),
                        Numero_Factura = reader.GetInt32(reader.GetOrdinal("Numero_Factura")),
                        Literal = reader.GetString(reader.GetOrdinal("Literal")),
                        DocumentoId_EasyConexion = reader.IsDBNull(reader.GetOrdinal("DocumentoId_EasyConexion")) ? null : reader.GetInt32(reader.GetOrdinal("DocumentoId_EasyConexion")),
                        VoucherNumber_DGII = reader.IsDBNull(reader.GetOrdinal("VoucherNumber_DGII")) ? null : reader.GetString(reader.GetOrdinal("VoucherNumber_DGII")),
                        Estado = reader.GetString(reader.GetOrdinal("Estado")),
                        Respuesta_JSON = reader.IsDBNull(reader.GetOrdinal("Respuesta_JSON")) ? null : reader.GetString(reader.GetOrdinal("Respuesta_JSON")),
                        Fecha_Envio = reader.GetDateTime(reader.GetOrdinal("Fecha_Envio")),
                        Fecha_Confirmacion = reader.IsDBNull(reader.GetOrdinal("Fecha_Confirmacion")) ? null : reader.GetDateTime(reader.GetOrdinal("Fecha_Confirmacion")),
                        Error_Descripcion = reader.IsDBNull(reader.GetOrdinal("Error_Descripcion")) ? null : reader.GetString(reader.GetOrdinal("Error_Descripcion")),
                        Usuario = reader.IsDBNull(reader.GetOrdinal("Usuario")) ? null : reader.GetString(reader.GetOrdinal("Usuario")),
                        Fecha_Registro = reader.GetDateTime(reader.GetOrdinal("Fecha_Registro")),
                        QR_Url = reader.IsDBNull(reader.GetOrdinal("QR_Url")) ? null : reader.GetString(reader.GetOrdinal("QR_Url"))
                    });
                }

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en GetLogFacturasElectronicasPendientes: {ex.Message}");
                throw;
            }
        }

        #endregion

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

        public void ActualizarNumeroFacturaEnDetallesPago(int numeroPreFactura, int nuevoNumeroFactura)
        {
            using var connection = new SqlConnection(_connectionStrings);
            using var cmd = new SqlCommand("UpdateDetallesPagoFactura", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Numero_Factura_Anterior", numeroPreFactura);
            cmd.Parameters.AddWithValue("@Numero_Factura_Nueva", nuevoNumeroFactura);

            connection.Open();
            cmd.ExecuteNonQuery();
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

        public async Task<bool> InsertFacturaProveedor2Dual(Facturas factura, List<DetalleFactura> detalles, string usuario, string rncEmisor)
        {
            if (factura == null || detalles == null || detalles.Count == 0)
                return false;

            if (string.IsNullOrWhiteSpace(factura.NcfNumero) || string.IsNullOrWhiteSpace(rncEmisor))
                return false;

            try
            {
                if (string.IsNullOrWhiteSpace(_seguridadConnectionString) || string.IsNullOrWhiteSpace(_proveedor2ConnectionString))
                {
                    _logger.LogError("No se encontraron las cadenas de conexión para proveedor 2 (seguridad/remota).");
                    return false;
                }

                var okLocal = await InsertFacturaProveedor2EnConexion(_seguridadConnectionString, factura, detalles, usuario, rncEmisor);
                if (!okLocal)
                    return false;

                var okRemoto = await InsertFacturaProveedor2EnConexion(_proveedor2ConnectionString, factura, detalles, usuario, rncEmisor);
                if (!okRemoto)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en InsertFacturaProveedor2Dual: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InsertFacturaProveedor2EnConexion(string connectionString, Facturas factura, List<DetalleFactura> detalles, string usuario, string rncEmisor)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var tx = connection.BeginTransaction();

            try
            {
                foreach (var det in detalles)
                {
                    await using var cmdDet = new SqlCommand(@"
INSERT INTO dbo.PIVOT_FACTURAS_DET
(COMPANIA,SUCURSAL,TIPO_DOCUMENTO_COMPANIA,COD_EMPRESA,COD_SUCURSAL,TIPO_DOCUMENTO,NUM_DOCUMENTO,RNC_EMISOR,ENCF,COD_PRODUCTO,TIPO_PRODUCTO,UND_MEDIDA,DESCRIPCION,CANTIDAD,PRECIO,IND_IMP,IND_EXENTO,TASA_IMPUESTO,PORC_DESCUENTO,MONTO_DESCUENTO,SUB_TOTAL,IMPUESTO,TOTAL)
VALUES
(@COMPANIA,@SUCURSAL,@TIPO_DOCUMENTO_COMPANIA,@COD_EMPRESA,@COD_SUCURSAL,@TIPO_DOCUMENTO,@NUM_DOCUMENTO,@RNC_EMISOR,@ENCF,@COD_PRODUCTO,@TIPO_PRODUCTO,@UND_MEDIDA,@DESCRIPCION,@CANTIDAD,@PRECIO,@IND_IMP,@IND_EXENTO,@TASA_IMPUESTO,@PORC_DESCUENTO,@MONTO_DESCUENTO,@SUB_TOTAL,@IMPUESTO,@TOTAL)", connection, tx);

                    cmdDet.Parameters.AddWithValue("@COMPANIA", "1");
                    cmdDet.Parameters.AddWithValue("@SUCURSAL", factura.IdSucursal ?? "1");
                    cmdDet.Parameters.AddWithValue("@TIPO_DOCUMENTO_COMPANIA", 1);
                    cmdDet.Parameters.AddWithValue("@COD_EMPRESA", int.TryParse(factura.IdEmpresa, out var idEmp) ? idEmp : 1);
                    cmdDet.Parameters.AddWithValue("@COD_SUCURSAL", int.TryParse(factura.IdSucursal, out var idSuc) ? idSuc : (object)DBNull.Value);
                    cmdDet.Parameters.AddWithValue("@TIPO_DOCUMENTO", "FT");
                    cmdDet.Parameters.AddWithValue("@NUM_DOCUMENTO", factura.NumeroFactura.ToString());
                    cmdDet.Parameters.AddWithValue("@RNC_EMISOR", rncEmisor);
                    cmdDet.Parameters.AddWithValue("@ENCF", factura.NcfNumero);
                    cmdDet.Parameters.AddWithValue("@COD_PRODUCTO", det.IdEquipo ?? string.Empty);
                    cmdDet.Parameters.AddWithValue("@TIPO_PRODUCTO", (object)DBNull.Value);
                    cmdDet.Parameters.AddWithValue("@UND_MEDIDA", "43");
                    cmdDet.Parameters.AddWithValue("@DESCRIPCION", det.Descripcion ?? det.IdEquipo ?? "ITEM");
                    cmdDet.Parameters.AddWithValue("@CANTIDAD", det.Cantidad);
                    cmdDet.Parameters.AddWithValue("@PRECIO", det.Precio);
                    cmdDet.Parameters.AddWithValue("@IND_IMP", det.Itbis > 0 ? "S" : "N");
                    cmdDet.Parameters.AddWithValue("@IND_EXENTO", det.Itbis > 0 ? "N" : "S");
                    cmdDet.Parameters.AddWithValue("@TASA_IMPUESTO", det.Itbis > 0 ? 18 : 0);
                    cmdDet.Parameters.AddWithValue("@PORC_DESCUENTO", 0);
                    cmdDet.Parameters.AddWithValue("@MONTO_DESCUENTO", det.Descuento);
                    cmdDet.Parameters.AddWithValue("@SUB_TOTAL", det.SubTotal);
                    cmdDet.Parameters.AddWithValue("@IMPUESTO", det.Itbis);
                    cmdDet.Parameters.AddWithValue("@TOTAL", det.Total);

                    await cmdDet.ExecuteNonQueryAsync();
                }

                var nombreCliente = "CONSUMIDOR FINAL";
                if (factura.NcfTipoNumero == 31)
                {
                    try
                    {
                        var clientes = Cliente_Consulta_Por_Id_Cliente(factura.IdCliente);
                        if (clientes != null && clientes.Rows.Count > 0)
                        {
                            var nombres = clientes.Rows[0]["Nombres"]?.ToString();
                            var apellidos = clientes.Rows[0]["Apellidos"]?.ToString();
                            nombreCliente = !string.IsNullOrWhiteSpace(nombres) ? nombres : "Cliente";
                            if (!string.IsNullOrWhiteSpace(apellidos))
                                nombreCliente += " " + apellidos;
                        }
                        else
                        {
                            nombreCliente = "Cliente";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error obteniendo nombre del cliente para Proveedor2. IdCliente={factura.IdCliente}, Error={ex.Message}");
                        nombreCliente = "Cliente";
                    }
                }

                await using var cmdEnc = new SqlCommand(@"
INSERT INTO dbo.PIVOT_FACTURAS_ENC
(COMPANIA,SUCURSAL,TIPO_DOCUMENTO_COMPANIA,COD_EMPRESA,COD_SUCURSAL,TIPO_DOCUMENTO,NUM_DOCUMENTO,RNC_EMISOR,NUM_RNC,NUM_CLIENTE,NOMBRE_CLIENTE,TIPO_INGRESO,FORMA_PAGO,MONEDA,FECHA_FACTURA,TIPO_NCF,ENCF,SUB_TOTAL,DESCUENTO,IMPUESTO,TOTAL,USUARIO_CREA,FECHA_REGISTRO,Indicador_NotaCredito,DIRECCION)
VALUES
(@COMPANIA,@SUCURSAL,@TIPO_DOCUMENTO_COMPANIA,@COD_EMPRESA,@COD_SUCURSAL,@TIPO_DOCUMENTO,@NUM_DOCUMENTO,@RNC_EMISOR,@NUM_RNC,@NUM_CLIENTE,@NOMBRE_CLIENTE,@TIPO_INGRESO,@FORMA_PAGO,@MONEDA,@FECHA_FACTURA,@TIPO_NCF,@ENCF,@SUB_TOTAL,@DESCUENTO,@IMPUESTO,@TOTAL,@USUARIO_CREA,@FECHA_REGISTRO,@Indicador_NotaCredito,@DIRECCION)", connection, tx);

                cmdEnc.Parameters.AddWithValue("@COMPANIA", "1");
                cmdEnc.Parameters.AddWithValue("@SUCURSAL", factura.IdSucursal ?? "1");
                cmdEnc.Parameters.AddWithValue("@TIPO_DOCUMENTO_COMPANIA", 1);
                cmdEnc.Parameters.AddWithValue("@COD_EMPRESA", int.TryParse(factura.IdEmpresa, out var codEmp) ? codEmp : 1);
                cmdEnc.Parameters.AddWithValue("@COD_SUCURSAL", int.TryParse(factura.IdSucursal, out var codSuc) ? codSuc : (object)DBNull.Value);
                cmdEnc.Parameters.AddWithValue("@TIPO_DOCUMENTO", "FT");
                cmdEnc.Parameters.AddWithValue("@NUM_DOCUMENTO", factura.NumeroFactura.ToString());
                cmdEnc.Parameters.AddWithValue("@RNC_EMISOR", rncEmisor);
                cmdEnc.Parameters.AddWithValue("@NUM_RNC", string.IsNullOrWhiteSpace(factura.RNC) ? (object)DBNull.Value : factura.RNC);
                cmdEnc.Parameters.AddWithValue("@NUM_CLIENTE", factura.IdCliente.ToString());
                cmdEnc.Parameters.AddWithValue("@NOMBRE_CLIENTE", nombreCliente);
                cmdEnc.Parameters.AddWithValue("@TIPO_INGRESO", "01");
                cmdEnc.Parameters.AddWithValue("@FORMA_PAGO", 1);
                cmdEnc.Parameters.AddWithValue("@MONEDA", 1);
                cmdEnc.Parameters.AddWithValue("@FECHA_FACTURA", factura.FechaRegistro);
                cmdEnc.Parameters.AddWithValue("@TIPO_NCF", factura.NcfTipoNumero);
                cmdEnc.Parameters.AddWithValue("@ENCF", factura.NcfNumero);
                cmdEnc.Parameters.AddWithValue("@SUB_TOTAL", factura.SubTotal);
                cmdEnc.Parameters.AddWithValue("@DESCUENTO", factura.Descuento);
                cmdEnc.Parameters.AddWithValue("@IMPUESTO", factura.Itbis);
                cmdEnc.Parameters.AddWithValue("@TOTAL", factura.Total);
                cmdEnc.Parameters.AddWithValue("@USUARIO_CREA", string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario);
                cmdEnc.Parameters.AddWithValue("@FECHA_REGISTRO", DateTime.Now);
                cmdEnc.Parameters.AddWithValue("@Indicador_NotaCredito", factura.NcfTipoNumero == 34 ? 1 : 0);
                cmdEnc.Parameters.AddWithValue("@DIRECCION", "N/A");

                await cmdEnc.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError($"Error insertando Proveedor2 (detalle->encabezado): {ex.Message}");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Obtiene un tipo de pago por su ID, incluyendo el código DGI
        /// </summary>
        public Tipos_Pagos Tipo_Pagos_GetById(int idTipoPago)
        {
            cn.Open();
            try
            {
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        ID_Tipo_Pagos,
                        Descripcion_Tipo_Pagos,
                        ISNULL(Codigo_DGI, '01') AS Codigo_DGI
                    FROM Tipos_Pagos_Cata
                    WHERE ID_Tipo_Pagos = @IdTipoPago";

                cmd.Parameters.AddWithValue("@IdTipoPago", idTipoPago);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Tipos_Pagos
                        {
                            ID_Tipo_Pagos = Convert.ToInt32(reader["ID_Tipo_Pagos"]),
                            Descripcion_Tipo_Pagos = reader["Descripcion_Tipo_Pagos"]?.ToString(),
                            Codigo_DGI = reader["Codigo_DGI"]?.ToString() ?? "01"  // Defecto: Efectivo
                        };
                    }
                }

                // Si no existe, retornar un tipo de pago por defecto
                _logger.LogWarning($"Tipo de pago con ID {idTipoPago} no encontrado. Retornando defecto (Efectivo).");
                return new Tipos_Pagos
                {
                    ID_Tipo_Pagos = idTipoPago,
                    Descripcion_Tipo_Pagos = "Desconocido",
                    Codigo_DGI = "01"  // Defecto: Efectivo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener tipo de pago por ID {idTipoPago}: {ex.Message}");
                throw;
            }
            finally
            {
                cn.Close();
            }
        }

        public ReciboAbonoDTO GetReciboAbono(int idCliente, int numeroFactura, int idSucursal)
        {
            using var cn = new SqlConnection(_connectionStrings);
            const string sql = @"
                DECLARE @NombreExpr NVARCHAR(200) =
                    CASE
                        WHEN COL_LENGTH('Clientes', 'Nombres') IS NOT NULL THEN 'c.Nombres'
                        WHEN COL_LENGTH('Clientes', 'Nombre')  IS NOT NULL THEN 'c.Nombre'
                        ELSE ''''''
                    END;

                DECLARE @ApellidoExpr NVARCHAR(200) =
                    CASE
                        WHEN COL_LENGTH('Clientes', 'Apellidos') IS NOT NULL THEN 'c.Apellidos'
                        WHEN COL_LENGTH('Clientes', 'Apellido')  IS NOT NULL THEN 'c.Apellido'
                        ELSE ''''''
                    END;

                DECLARE @sql NVARCHAR(MAX) = N'
                    SELECT TOP 1
                        hp.Id,
                        TRY_CONVERT(INT, hp.Numero_factura)                AS Numero_factura,
                        TRY_CONVERT(DECIMAL(18,2), hp.Total_Factura)       AS Total_Factura,
                        TRY_CONVERT(DECIMAL(18,2), hp.Total_Abono)         AS Total_Abono,
                        TRY_CONVERT(DECIMAL(18,2), hp.Pendiente)           AS Pendiente,
                        CAST(ISNULL(hp.Tipo_Factura, '''') AS NVARCHAR(50))  AS Tipo_Factura,
                        CAST(ISNULL(hp.Usuario, '''') AS NVARCHAR(100))      AS Usuario,
                        hp.Fecha_registro,
                        CAST(ISNULL(' + @NombreExpr + ', '''') AS NVARCHAR(150))   AS NombreCliente,
                        CAST(ISNULL(' + @ApellidoExpr + ', '''') AS NVARCHAR(150)) AS ApellidoCliente
                    FROM Historio_Pagos hp
                    INNER JOIN Clientes c ON c.Id_Cliente = hp.id_Cliente_Suplidor
                    WHERE hp.id_Cliente_Suplidor = @idCliente
                      AND hp.Id_Sucursal         = @idSucursal
                      AND (@numeroFactura = 0 OR TRY_CONVERT(INT, hp.Numero_factura) = @numeroFactura)
                    ORDER BY hp.Fecha_registro DESC';

                EXEC sp_executesql @sql,
                    N'@idCliente INT, @numeroFactura INT, @idSucursal INT',
                    @idCliente=@idCliente, @numeroFactura=@numeroFactura, @idSucursal=@idSucursal;";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idCliente",      idCliente);
            cmd.Parameters.AddWithValue("@numeroFactura",  numeroFactura);
            cmd.Parameters.AddWithValue("@idSucursal",     idSucursal);

            cn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int idxId = reader.GetOrdinal("Id");
                int idxNombreCliente = reader.GetOrdinal("NombreCliente");
                int idxApellidoCliente = reader.GetOrdinal("ApellidoCliente");
                int idxNumeroFactura = reader.GetOrdinal("Numero_factura");
                int idxTotalFactura = reader.GetOrdinal("Total_Factura");
                int idxTotalAbono = reader.GetOrdinal("Total_Abono");
                int idxPendiente = reader.GetOrdinal("Pendiente");
                int idxTipoFactura = reader.GetOrdinal("Tipo_Factura");
                int idxUsuario = reader.GetOrdinal("Usuario");
                int idxFechaRegistro = reader.GetOrdinal("Fecha_registro");

                decimal ToDecimalSafe(int index)
                {
                    if (reader.IsDBNull(index))
                    {
                        return 0;
                    }

                    var value = reader.GetValue(index);
                    try
                    {
                        return Convert.ToDecimal(value);
                    }
                    catch
                    {
                        return decimal.TryParse(value?.ToString(), out var parsed) ? parsed : 0;
                    }
                }

                DateTime ToDateTimeSafe(int index)
                {
                    if (reader.IsDBNull(index))
                    {
                        return DateTime.Now;
                    }

                    var value = reader.GetValue(index);
                    try
                    {
                        return Convert.ToDateTime(value);
                    }
                    catch
                    {
                        return DateTime.Now;
                    }
                }

                string ToStringSafe(int index)
                {
                    if (reader.IsDBNull(index))
                    {
                        return "";
                    }

                    return reader.GetValue(index)?.ToString() ?? "";
                }

                string GetClienteValueFromRow(DataRow row, string[] candidateColumns)
                {
                    if (row == null || row.Table == null || candidateColumns == null || candidateColumns.Length == 0)
                    {
                        return "";
                    }

                    for (int i = 0; i < row.Table.Columns.Count; i++)
                    {
                        string currentColumn = row.Table.Columns[i].ColumnName;

                        for (int j = 0; j < candidateColumns.Length; j++)
                        {
                            if (!string.Equals(currentColumn, candidateColumns[j], StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var value = row[currentColumn];
                            if (value == null || value == DBNull.Value)
                            {
                                return "";
                            }

                            return value.ToString() ?? "";
                        }
                    }

                    return "";
                }

                string nombreCliente = ToStringSafe(idxNombreCliente);
                string apellidoCliente = ToStringSafe(idxApellidoCliente);

                if (string.IsNullOrWhiteSpace(nombreCliente) && string.IsNullOrWhiteSpace(apellidoCliente))
                {
                    try
                    {
                        DataTable clienteData = Cliente_Consulta_Por_Id_Cliente(idCliente);
                        if (clienteData != null && clienteData.Rows.Count > 0)
                        {
                            DataRow clienteRow = clienteData.Rows[0];
                            nombreCliente = GetClienteValueFromRow(clienteRow, new[] { "Nombres", "Nombre", "NombreCliente" });
                            apellidoCliente = GetClienteValueFromRow(clienteRow, new[] { "Apellidos", "Apellido", "ApellidoCliente" });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo resolver nombre/apellido del cliente por ID en GetReciboAbono. idCliente={IdCliente}", idCliente);
                    }
                }

                return new ReciboAbonoDTO
                {
                    Id             = reader.IsDBNull(idxId) ? 0 : reader.GetInt32(idxId),
                    NombreCliente  = nombreCliente,
                    ApellidoCliente= apellidoCliente,
                    NumeroFactura  = reader.IsDBNull(idxNumeroFactura) ? "" : reader.GetInt32(idxNumeroFactura).ToString(),
                    TotalFactura   = ToDecimalSafe(idxTotalFactura),
                    TotalAbono     = ToDecimalSafe(idxTotalAbono),
                    Pendiente      = ToDecimalSafe(idxPendiente),
                    TipoFactura    = ToStringSafe(idxTipoFactura),
                    Usuario        = ToStringSafe(idxUsuario),
                    FechaRegistro  = ToDateTimeSafe(idxFechaRegistro)
                };
            }
            return null;
        }
    }
}

