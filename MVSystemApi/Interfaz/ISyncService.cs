using System.Data;
using System.Data.SqlClient;

namespace MVSystemApi.Interfaz
{
    public interface ISyncService
    {
        /// <summary>
        /// Ejecuta un SP en la BD espejo. Devuelve true si se aplicó o si el usuario no tiene espejo
        /// configurado; false solo si debía aplicarse y falló (el fallo queda en el log con [SYNC-FALLO]).
        /// </summary>
        bool Ejecutar(string spName, SqlParameterCollection parameters);

        /// <summary>Ejecuta una consulta de escritura (UPDATE) en la BD espejo. Mismo significado de retorno.</summary>
        bool EjecutarQuery(string sql, params SqlParameter[] parameters);

        /// <summary>
        /// Ejecuta varias consultas de escritura en UNA conexión y UNA transacción (todo o nada).
        /// Pensado para facturas con varios equipos. Mismo significado de retorno.
        /// </summary>
        bool EjecutarQueries(IReadOnlyList<(string Sql, SqlParameter[] Parametros)> consultas);
    }
}
