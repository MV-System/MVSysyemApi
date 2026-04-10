using System.Data;
using System.Data.SqlClient;

namespace MVSystemApi.Interfaz
{
    public interface ISyncService
    {
        void Ejecutar(string spName, SqlParameterCollection parameters);
        void EjecutarQuery(string sql, params SqlParameter[] parameters);
    }
}
