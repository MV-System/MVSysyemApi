using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVSystemApi.Interfaz
{
    /// <summary>
    /// Interfaz para operaciones de tracking de usuarios
    /// </summary>
    public interface ITracking
    {
        /// <summary>
        /// Registra una interacción del usuario con el sistema
        /// </summary>
        Task<int> RegistrarTracking(TrackingLog log);

        /// <summary>
        /// Obtiene el historial de tracking con filtros opcionales
        /// </summary>
        Task<List<TrackingLog>> ObtenerHistorial(
            string usuario = null,
            string modulo = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int top = 100
        );

        /// <summary>
        /// Obtiene estadísticas de uso por módulo
        /// </summary>
        Task<Dictionary<string, int>> ObtenerEstadisticasPorModulo(DateTime? fechaDesde = null, DateTime? fechaHasta = null);

        /// <summary>
        /// Obtiene los usuarios más activos
        /// </summary>
        Task<List<(string Usuario, int Interacciones)>> ObtenerUsuariosMasActivos(int top = 10);

        /// <summary>
        /// Marca un log como notificado
        /// </summary>
        Task<bool> MarcarComoNotificado(int idTracking);

        /// <summary>
        /// Verifica si debe enviar notificación WhatsApp según configuración en BD
        /// </summary>
        Task<bool> DebeNotificar(string modulo, string accion);
    }
}
