using Microsoft.Extensions.Configuration;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MVSystemApi.Model_Negocio
{
    /// <summary>
    /// Implementaci�n de l�gica de negocio para Tracking usando connection string SEGURIDAD
    /// </summary>
    public class Tracking_Negocio : ITracking
    {
        private readonly string _connectionString;

        public Tracking_Negocio(IConfiguration configuration)
        {
            // Usar siempre el connection string de SEGURIDAD para tracking
            _connectionString = configuration.GetConnectionString("MVSystemSeguridad");
        }

        /// <summary>
        /// Registra una interacci�n del usuario
        /// </summary>
        public async Task<int> RegistrarTracking(TrackingLog log)
        {
            try
            {
                // Validar campos requeridos
                if (string.IsNullOrEmpty(log.Usuario))
                {
                    throw new ArgumentException("Usuario es requerido");
                }
                if (string.IsNullOrEmpty(log.Modulo))
                {
                    throw new ArgumentException("Modulo es requerido");
                }
                if (string.IsNullOrEmpty(log.Accion))
                {
                    throw new ArgumentException("Accion es requerida");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        INSERT INTO [dbo].[TrackingLogs]
                        (Usuario, Modulo, Accion, FechaHora, Metadata, DireccionIP, UserAgent, NotificacionEnviada)
                        VALUES
                        (@Usuario, @Modulo, @Accion, DATEADD(HOUR, -4, GETDATE()), @Metadata, @DireccionIP, @UserAgent, @NotificacionEnviada);
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Usuario", log.Usuario);
                        command.Parameters.AddWithValue("@Modulo", log.Modulo);
                        command.Parameters.AddWithValue("@Accion", log.Accion);
                        // @FechaHora eliminado - se calcula directo en SQL con DATEADD(HOUR, -4, GETDATE())
                        command.Parameters.AddWithValue("@Metadata", log.Metadata ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DireccionIP", log.DireccionIP ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UserAgent", log.UserAgent ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@NotificacionEnviada", log.NotificacionEnviada);

                        var result = await command.ExecuteScalarAsync();
                        var idTracking = Convert.ToInt32(result);
                        
                        return idTracking;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al registrar tracking: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene historial de tracking con filtros
        /// </summary>
        public async Task<List<TrackingLog>> ObtenerHistorial(
            string usuario = null,
            string modulo = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int top = 100)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var whereClauses = new List<string>();
                    var query = $@"
                        SELECT TOP {top}
                            IdTracking,
                            Usuario,
                            Modulo,
                            Accion,
                            FechaHora,
                            Metadata,
                            DireccionIP,
                            UserAgent,
                            NotificacionEnviada,
                            FechaNotificacion
                        FROM [dbo].[TrackingLogs]";

                    using (var command = new SqlCommand())
                    {
                        if (!string.IsNullOrEmpty(usuario))
                        {
                            whereClauses.Add("Usuario = @Usuario");
                            command.Parameters.AddWithValue("@Usuario", usuario);
                        }

                        if (!string.IsNullOrEmpty(modulo))
                        {
                            whereClauses.Add("Modulo = @Modulo");
                            command.Parameters.AddWithValue("@Modulo", modulo);
                        }

                        if (fechaDesde.HasValue)
                        {
                            whereClauses.Add("FechaHora >= @FechaDesde");
                            command.Parameters.AddWithValue("@FechaDesde", fechaDesde.Value);
                        }

                        if (fechaHasta.HasValue)
                        {
                            whereClauses.Add("FechaHora <= @FechaHasta");
                            command.Parameters.AddWithValue("@FechaHasta", fechaHasta.Value);
                        }

                        if (whereClauses.Count > 0)
                        {
                            query += " WHERE " + string.Join(" AND ", whereClauses);
                        }

                        query += " ORDER BY FechaHora DESC";

                        command.CommandText = query;
                        command.Connection = connection;

                        var logs = new List<TrackingLog>();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                logs.Add(new TrackingLog
                                {
                                    IdTracking = reader.GetInt32(0),
                                    Usuario = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    Modulo = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Accion = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    FechaHora = reader.GetDateTime(4),
                                    Metadata = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    DireccionIP = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    UserAgent = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    NotificacionEnviada = reader.GetBoolean(8),
                                    FechaNotificacion = reader.IsDBNull(9) ? null : (DateTime?)reader.GetDateTime(9)
                                });
                            }
                        }

                        return logs;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener historial: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene estad�sticas de uso por m�dulo
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerEstadisticasPorModulo(DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT Modulo, COUNT(*) as Total
                        FROM [dbo].[TrackingLogs]";

                    using (var command = new SqlCommand())
                    {
                        var whereClauses = new List<string>();

                        if (fechaDesde.HasValue)
                        {
                            whereClauses.Add("FechaHora >= @FechaDesde");
                            command.Parameters.AddWithValue("@FechaDesde", fechaDesde.Value);
                        }

                        if (fechaHasta.HasValue)
                        {
                            whereClauses.Add("FechaHora <= @FechaHasta");
                            command.Parameters.AddWithValue("@FechaHasta", fechaHasta.Value);
                        }

                        if (whereClauses.Count > 0)
                        {
                            query += " WHERE " + string.Join(" AND ", whereClauses);
                        }

                        query += " GROUP BY Modulo ORDER BY Total DESC";

                        command.CommandText = query;
                        command.Connection = connection;

                        var stats = new Dictionary<string, int>();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats[reader.GetString(0)] = reader.GetInt32(1);
                            }
                        }

                        return stats;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener estad�sticas: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene usuarios m�s activos
        /// </summary>
        public async Task<List<(string Usuario, int Interacciones)>> ObtenerUsuariosMasActivos(int top = 10)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = $@"
                        SELECT TOP {top} Usuario, COUNT(*) as Total
                        FROM [dbo].[TrackingLogs]
                        GROUP BY Usuario
                        ORDER BY Total DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        var stats = new List<(string Usuario, int Interacciones)>();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats.Add((reader.GetString(0), reader.GetInt32(1)));
                            }
                        }

                        return stats;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener usuarios activos: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Marca tracking como notificado
        /// </summary>
        public async Task<bool> MarcarComoNotificado(int idTracking)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        UPDATE [dbo].[TrackingLogs]
                        SET NotificacionEnviada = 1,
                            FechaNotificacion = DATEADD(HOUR, -4, GETDATE())
                        WHERE IdTracking = @IdTracking";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdTracking", idTracking);

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al marcar como notificado: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifica si debe enviar notificaci�n WhatsApp seg�n configuraci�n en BD
        /// </summary>
        public async Task<bool> DebeNotificar(string modulo, string accion)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Buscar configuraci�n espec�fica: Modulo + Accion
                    var query = @"
                        SELECT TOP 1 NotificarWhatsApp
                        FROM [dbo].[TrackingNotificacionConfig]
                        WHERE Activo = 1
                        AND (
                            -- Coincidencia exacta: Modulo + Accion
                            (Modulo = @Modulo AND Accion = @Accion)
                            OR
                            -- Coincidencia: Modulo + cualquier Accion
                            (Modulo = @Modulo AND Accion IS NULL)
                            OR
                            -- Coincidencia: cualquier Modulo + Accion espec�fica
                            (Modulo IS NULL AND Accion = @Accion)
                            OR
                            -- Coincidencia: TODO (Modulo y Accion NULL)
                            (Modulo IS NULL AND Accion IS NULL)
                        )
                        ORDER BY 
                            -- Prioridad: m�s espec�fico primero
                            CASE 
                                WHEN Modulo = @Modulo AND Accion = @Accion THEN 1
                                WHEN Modulo = @Modulo AND Accion IS NULL THEN 2
                                WHEN Modulo IS NULL AND Accion = @Accion THEN 3
                                ELSE 4
                            END";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Modulo", modulo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Accion", accion ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();
                        
                        // Si no hay configuraci�n, por defecto NO notifica
                        return result != null && Convert.ToBoolean(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // En caso de error, no notificar para evitar spam

                return false;
            }
        }
    }
}
