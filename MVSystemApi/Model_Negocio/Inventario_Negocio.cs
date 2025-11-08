using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace MVSystemApi.Model_Negocio
{
    /// <summary>
    /// Lógica de negocio para el módulo de Inventario de Equipos
    /// </summary>
    public class Inventario_Negocio
    {
        private readonly IAccesoDatos _accesoDatos;

        public Inventario_Negocio(IAccesoDatos accesoDatos)
        {
            _accesoDatos = accesoDatos;
        }

        /// <summary>
        /// Inicia una nueva sesión de inventario
        /// </summary>
        public IniciarInventarioResponse IniciarSesion(IniciarInventarioDTO dto)
        {
            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    // VALIDACIÓN PREVIA: Verificar si ya existe un inventario activo para este almacén
                    var checkQuery = @"
                        SELECT COUNT(*) 
                        FROM InventarioSesion 
                        WHERE ID_Almacen = @ID_Almacen 
                        AND Estado IN ('EN_PROCESO', 'PAUSADO')";

                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@ID_Almacen", dto.ID_Almacen);
                        int existingCount = (int)checkCommand.ExecuteScalar();
                        
                        if (existingCount > 0)
                        {
                            throw new Exception("Ya existe un inventario activo para este almacén");
                        }
                    }

                    using (var command = new SqlCommand("SP_Inventario_IniciarSesion", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@ID_Almacen", dto.ID_Almacen);
                        command.Parameters.AddWithValue("@ID_Empresa", dto.ID_Empresa);
                        command.Parameters.AddWithValue("@ID_Sucursal", dto.ID_Sucursal);
                        command.Parameters.AddWithValue("@Usuario", dto.Usuario);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new IniciarInventarioResponse
                                {
                                    ID_Sesion = reader.GetInt64(reader.GetOrdinal("ID_Sesion")),
                                    Total_Esperados = reader.GetInt32(reader.GetOrdinal("Total_Esperados"))
                                };
                            }
                        }
                    }
                }

                throw new Exception("No se pudo iniciar la sesión de inventario");
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al iniciar inventario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Registra el escaneo de un equipo
        /// </summary>
        public EscaneoResponse RegistrarEscaneo(RegistrarEscaneoDTO dto)
        {
            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SP_Inventario_RegistrarEscaneo", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@ID_Sesion", dto.ID_Sesion);
                        command.Parameters.AddWithValue("@IMEI", dto.IMEI);
                        command.Parameters.AddWithValue("@Usuario", dto.Usuario);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new EscaneoResponse
                                {
                                    Resultado = reader["Resultado"].ToString(),
                                    Mensaje = reader["Mensaje"].ToString()
                                };
                            }
                        }
                    }
                }

                throw new Exception("No se pudo registrar el escaneo");
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al registrar escaneo: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene equipos pendientes por escanear
        /// </summary>
        public List<InventarioEquipo> ObtenerPendientes(long idSesion)
        {
            var equipos = new List<InventarioEquipo>();

            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SP_Inventario_ObtenerPendientes", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                equipos.Add(new InventarioEquipo
                                {
                                    ID_Registro = reader.GetInt64(reader.GetOrdinal("ID_Registro")),
                                    ID_Equipo = reader.GetInt64(reader.GetOrdinal("ID_Equipo")),
                                    IMEI = reader["IMEI"].ToString(),
                                    Modelo = reader["Modelo"].ToString(),
                                    Marca = reader["Marca"] != DBNull.Value ? reader["Marca"].ToString() : null
                                });
                            }
                        }
                    }
                }

                return equipos;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al obtener equipos pendientes: {ex.Message}", ex);
            }
        }

        public List<InventarioEquipo> ObtenerEquiposFaltantes(long idSesion)
        {
            var equipos = new List<InventarioEquipo>();
            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SP_Inventario_EquiposFaltantes", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    equipos.Add(new InventarioEquipo
                                    {
                                        ID_Registro = reader.GetInt64(reader.GetOrdinal("ID_Registro")),
                                        ID_Sesion = reader.GetInt64(reader.GetOrdinal("ID_Sesion")),
                                        ID_Equipo = reader.IsDBNull(reader.GetOrdinal("ID_Equipo")) ? 0 : reader.GetInt64(reader.GetOrdinal("ID_Equipo")),
                                        IMEI = reader["IMEI"].ToString(),
                                        Fue_Escaneado = reader.GetBoolean(reader.GetOrdinal("Fue_Escaneado")),
                                        Fecha_Escaneo = null,
                                        Usuario_Escaneo = null,
                                        Orden_Escaneo = null,
                                        Tipo_Hallazgo = "FALTANTE",
                                        Estado_Anterior_Equipo = null,
                                        ID_Almacen_Anterior = null,
                                        Modelo = reader["Modelo"].ToString(),
                                        Marca = reader["Marca"].ToString()
                                    });
                                }
                                catch (Exception ex)
                                {

                                    throw ex;
                                }
                              
                            }
                        }
                    }
                }
                return equipos;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al obtener equipos faltantes: {ex.Message}", ex);
            }
        }

              public List<InventarioEquipo> ObtenerEquiposSobrantes(long idSesion)
                            {
                                var equipos = new List<InventarioEquipo>();
                                try
                                {
                                    using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                                    {
                                        connection.Open();
                                        using (var command = new SqlCommand("SP_Inventario_EquiposSobrantes", connection))
                                        {
                                            command.CommandType = CommandType.StoredProcedure;
                                            command.Parameters.AddWithValue("@ID_Sesion", idSesion);
                                            using (var reader = command.ExecuteReader())
                                            {
                                                while (reader.Read())
                                                {
                                                    equipos.Add(new InventarioEquipo
                                                    {
                                                        ID_Registro = 0,
                                                        ID_Sesion = reader.GetInt64(reader.GetOrdinal("ID_Sesion")),
                                                        ID_Equipo = 0,
                                                        IMEI = reader["Descripcion"].ToString(), // O ajusta según columna real
                                                        Fue_Escaneado = true,
                                                        Fecha_Escaneo = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                                                        Usuario_Escaneo = reader["Usuario"].ToString(),
                                                        Orden_Escaneo = null,
                                                        Tipo_Hallazgo = "SOBRANTE",
                                                        Estado_Anterior_Equipo = null,
                                                        ID_Almacen_Anterior = null,
                                                        Modelo = null,
                                                        Marca = null
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    return equipos;
                                }
                                catch (SqlException ex)
                                {
                                    throw new Exception($"Error al obtener equipos sobrantes: {ex.Message}", ex);
                                }
                            }              
        /// <summary>
        /// Obtiene equipos ya escaneados
        /// </summary>
        public List<InventarioEquipo> ObtenerEscaneados(long idSesion)
        {
            var equipos = new List<InventarioEquipo>();

            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SP_Inventario_ObtenerEscaneados", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                equipos.Add(new InventarioEquipo
                                {
                                    ID_Registro = reader.GetInt64(reader.GetOrdinal("ID_Registro")),
                                    ID_Equipo = reader.GetInt64(reader.GetOrdinal("ID_Equipo")),
                                    IMEI = reader["IMEI"].ToString(),
                                    Modelo = reader["Modelo"].ToString(),
                                    Marca = reader["Marca"] != DBNull.Value ? reader["Marca"].ToString() : null,
                                    Fecha_Escaneo = reader["Fecha_Escaneo"] != DBNull.Value
                                        ? reader.GetDateTime(reader.GetOrdinal("Fecha_Escaneo"))
                                        : (DateTime?)null,
                                    Usuario_Escaneo = reader["Usuario_Escaneo"] != DBNull.Value
                                        ? reader["Usuario_Escaneo"].ToString()
                                        : null,
                                    Orden_Escaneo = reader["Orden_Escaneo"] != DBNull.Value
                                        ? reader.GetInt32(reader.GetOrdinal("Orden_Escaneo"))
                                        : (int?)null,
                                    Tipo_Hallazgo = reader["Tipo_Hallazgo"].ToString()
                                });
                            }
                        }

                            /// <summary>
                            /// Obtiene equipos faltantes usando SP
                            /// </summary>
                            

                            /// <summary>
                            /// Obtiene equipos sobrantes usando SP
                            /// </summary>
                            
                    }
                }

                return equipos;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al obtener equipos escaneados: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Finaliza el inventario y actualiza equipos
        /// </summary>
        public FinalizarInventarioResponse FinalizarInventario(FinalizarInventarioDTO dto)
        {
            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SP_Inventario_Finalizar", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 120; // 2 minutos por si hay muchos equipos

                        command.Parameters.AddWithValue("@ID_Sesion", dto.ID_Sesion);
                        command.Parameters.AddWithValue("@Usuario", dto.Usuario);
                        command.Parameters.AddWithValue("@Observaciones", 
                            string.IsNullOrEmpty(dto.Observaciones) ? (object)DBNull.Value : dto.Observaciones);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var resultado = reader["Resultado"].ToString();
                                var mensaje = reader["Mensaje"].ToString();
                                
                                // Leer Total_Faltantes de forma segura
                                int totalFaltantes = 0;
                                if (reader["Total_Faltantes"] != DBNull.Value)
                                {
                                    totalFaltantes = Convert.ToInt32(reader["Total_Faltantes"]);
                                }
                                
                                Console.WriteLine($"[INVENTARIO] Finalizar - Resultado: {resultado}, Mensaje: {mensaje}, Faltantes: {totalFaltantes}");
                                
                                return new FinalizarInventarioResponse
                                {
                                    Resultado = resultado,
                                    Mensaje = mensaje,
                                    Total_Faltantes = totalFaltantes
                                };
                            }
                        }
                    }
                }

                throw new Exception("No se pudo finalizar el inventario");
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al finalizar inventario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene información de una sesión de inventario
        /// </summary>
        public InventarioSesion ObtenerSesion(long idSesion)
        {
            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SP_Inventario_ObtenerSesion", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new InventarioSesion
                                {
                                    ID_Sesion = reader.GetInt64(reader.GetOrdinal("ID_Sesion")),
                                    ID_Almacen = reader.GetInt32(reader.GetOrdinal("ID_Almacen")),
                                    Nombre_Almacen = reader["Nombre_Almacen"].ToString(),
                                    Estado = reader["Estado"].ToString(),
                                    Total_Esperados = reader.GetInt32(reader.GetOrdinal("Total_Esperados")),
                                    Total_Escaneados = reader.GetInt32(reader.GetOrdinal("Total_Escaneados")),
                                    Total_Faltantes = reader.GetInt32(reader.GetOrdinal("Total_Faltantes")),
                                    Total_Sobrantes = reader.GetInt32(reader.GetOrdinal("Total_Sobrantes")),
                                    Porcentaje_Completado = reader["Porcentaje_Completado"] != DBNull.Value 
                                        ? reader.GetDecimal(reader.GetOrdinal("Porcentaje_Completado")) 
                                        : (decimal?)null,
                                    Fecha_Inicio = reader.GetDateTime(reader.GetOrdinal("Fecha_Inicio")),
                                    Fecha_Finalizacion = reader["Fecha_Finalizacion"] != DBNull.Value 
                                        ? reader.GetDateTime(reader.GetOrdinal("Fecha_Finalizacion")) 
                                        : (DateTime?)null,
                                    Minutos_Transcurridos = reader["Minutos_Transcurridos"] != DBNull.Value 
                                        ? reader.GetInt32(reader.GetOrdinal("Minutos_Transcurridos")) 
                                        : (int?)null,
                                    Usuario_Responsable = reader["Usuario_Responsable"].ToString(),
                                    Usuario_Finalizo = reader["Usuario_Finalizo"] != DBNull.Value 
                                        ? reader["Usuario_Finalizo"].ToString() 
                                        : null,
                                    Observaciones = reader["Observaciones"] != DBNull.Value 
                                        ? reader["Observaciones"].ToString() 
                                        : null
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al obtener sesión de inventario: {ex.Message}", ex);
            }
        }





        /// <summary>
        /// Obtiene lista de inventarios activos por almacén
        /// </summary>
        public List<InventarioSesion> ObtenerInventariosActivos(int? idAlmacen = null)
        {
            var inventarios = new List<InventarioSesion>();

            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    var query = @"
                        SELECT 
                            s.ID_Sesion,
                            s.ID_Almacen,
                            CONCAT('Almacén ', s.ID_Almacen) AS Nombre_Almacen,
                            s.Estado,
                            s.Total_Esperados,
                            s.Total_Escaneados,
                            s.Porcentaje_Completado,
                            s.Fecha_Inicio,
                            s.Usuario_Responsable
                        FROM InventarioSesion s
                        WHERE s.Estado IN ('EN_PROCESO', 'PAUSADO')";

                    if (idAlmacen.HasValue)
                    {
                        query += " AND s.ID_Almacen = @ID_Almacen";
                    }

                    query += " ORDER BY s.Fecha_Inicio DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (idAlmacen.HasValue)
                        {
                            command.Parameters.AddWithValue("@ID_Almacen", idAlmacen.Value);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                inventarios.Add(new InventarioSesion
                                {
                                    ID_Sesion = reader.GetInt64(reader.GetOrdinal("ID_Sesion")),
                                    ID_Almacen = reader.GetInt32(reader.GetOrdinal("ID_Almacen")),
                                    Nombre_Almacen = reader["Nombre_Almacen"].ToString(),
                                    Estado = reader["Estado"].ToString(),
                                    Total_Esperados = reader.GetInt32(reader.GetOrdinal("Total_Esperados")),
                                    Total_Escaneados = reader.GetInt32(reader.GetOrdinal("Total_Escaneados")),
                                    Porcentaje_Completado = reader["Porcentaje_Completado"] != DBNull.Value 
                                        ? reader.GetDecimal(reader.GetOrdinal("Porcentaje_Completado")) 
                                        : (decimal?)null,
                                    Fecha_Inicio = reader.GetDateTime(reader.GetOrdinal("Fecha_Inicio")),
                                    Usuario_Responsable = reader["Usuario_Responsable"].ToString()
                                });
                            }
                        }
                    }
                }

                return inventarios;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al obtener inventarios activos: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reanuda un inventario pausado (cambia estado de PAUSADO a EN_PROCESO)
        /// </summary>
        public bool ReanudarInventario(long idSesion, string usuario)
        {
            try
            {
                // Truncar usuario a 50 caracteres para compatibilidad con SP
                string usuarioTruncado = usuario.Length > 50 ? usuario.Substring(0, 50) : usuario;

                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    // Usar UPDATE directo en lugar de SP que puede tener problemas
                    var query = @"
                        UPDATE InventarioSesion 
                        SET Estado = 'EN_PROCESO',
                            Fecha_Reanudacion = DATEADD(HOUR, -4, GETDATE())
                        WHERE ID_Sesion = @ID_Sesion 
                        AND Estado = 'PAUSADO'";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al reanudar inventario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Pausa un inventario en proceso (cambia estado de EN_PROCESO a PAUSADO)
        /// </summary>
        public bool PausarInventario(long idSesion, string usuario)
        {
            try
            {
                // Truncar usuario a 50 caracteres para compatibilidad
                string usuarioTruncado = usuario.Length > 50 ? usuario.Substring(0, 50) : usuario;

                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    var query = @"
                        UPDATE InventarioSesion 
                        SET Estado = 'PAUSADO',
                            Fecha_Pausa = DATEADD(HOUR, -4, GETDATE())
                        WHERE ID_Sesion = @ID_Sesion 
                        AND Estado = 'EN_PROCESO'";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al pausar inventario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// TEMPORAL: Cancela un inventario (cambia estado a CANCELADO)
        /// </summary>
        public void CancelarInventario(long idSesion)
        {
            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    var query = @"
                        UPDATE InventarioSesion 
                        SET Estado = 'CANCELADO',
                            Fecha_Finalizacion = DATEADD(HOUR, -4, GETDATE())
                        WHERE ID_Sesion = @ID_Sesion";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al cancelar inventario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene equipos por tipo de hallazgo (FALTANTE o SOBRANTE)
        /// </summary>
        public List<InventarioEquipo> ObtenerEquiposPorTipo(long idSesion, string tipoHallazgo)
        {
            var equipos = new List<InventarioEquipo>();

            try
            {
                using (var connection = new SqlConnection(_accesoDatos.ConnectionString))
                {
                    connection.Open();

                    var query = @"
                        SELECT 
                            ie.ID_Registro,
                            ie.ID_Sesion,
                            ie.ID_Equipo,
                            ie.IMEI,
                            ie.Fue_Escaneado,
                            ie.Fecha_Escaneo,
                            ie.Usuario_Escaneo,
                            ie.Orden_Escaneo,
                            ie.Tipo_Hallazgo,
                            ie.Estado_Anterior_Equipo,
                            ie.ID_Almacen_Anterior,
                            e.Modelo,
                            e.Marca
                        FROM InventarioEquipo ie
                        LEFT JOIN Equipos e ON ie.ID_Equipo = e.ID_Equipo
                        WHERE ie.ID_Sesion = @ID_Sesion
                        AND ie.Tipo_Hallazgo = @TipoHallazgo
                        ORDER BY ie.IMEI";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID_Sesion", idSesion);
                        command.Parameters.AddWithValue("@TipoHallazgo", tipoHallazgo);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                equipos.Add(new InventarioEquipo
                                {
                                    ID_Registro = reader.GetInt64(reader.GetOrdinal("ID_Registro")),
                                    ID_Sesion = reader.GetInt64(reader.GetOrdinal("ID_Sesion")),
                                    ID_Equipo = reader.IsDBNull(reader.GetOrdinal("ID_Equipo")) ? 
                                        0 : reader.GetInt32(reader.GetOrdinal("ID_Equipo")),
                                    IMEI = reader["IMEI"].ToString(),
                                    Fue_Escaneado = reader.GetBoolean(reader.GetOrdinal("Fue_Escaneado")),
                                    Fecha_Escaneo = reader.IsDBNull(reader.GetOrdinal("Fecha_Escaneo")) ? 
                                        (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Fecha_Escaneo")),
                                    Usuario_Escaneo = reader.IsDBNull(reader.GetOrdinal("Usuario_Escaneo")) ? 
                                        null : reader["Usuario_Escaneo"].ToString(),
                                    Orden_Escaneo = reader.IsDBNull(reader.GetOrdinal("Orden_Escaneo")) ? 
                                        (int?)null : reader.GetInt32(reader.GetOrdinal("Orden_Escaneo")),
                                    Tipo_Hallazgo = reader["Tipo_Hallazgo"].ToString(),
                                    Estado_Anterior_Equipo = reader.IsDBNull(reader.GetOrdinal("Estado_Anterior_Equipo")) ? 
                                        null : reader["Estado_Anterior_Equipo"].ToString(),
                                    ID_Almacen_Anterior = reader.IsDBNull(reader.GetOrdinal("ID_Almacen_Anterior")) ? 
                                        (int?)null : reader.GetInt32(reader.GetOrdinal("ID_Almacen_Anterior")),
                                    Modelo = reader.IsDBNull(reader.GetOrdinal("Modelo")) ? 
                                        null : reader["Modelo"].ToString(),
                                    Marca = reader.IsDBNull(reader.GetOrdinal("Marca")) ? 
                                        null : reader["Marca"].ToString()
                                });
                            }
                        }
                    }
                }

                return equipos;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al obtener equipos {tipoHallazgo.ToLower()}s: {ex.Message}", ex);
            }
        }
    }
}
