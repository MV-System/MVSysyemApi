using APIPartNet.DTOs.Auth;
using DTO;
using Microsoft.Data.SqlClient;
using MVSystemApi.Model;
using MVSystemApi.Model.Seguridad;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class SeguridadService
    {
        private readonly string ConnectionStrings;
        private readonly JwtService jwtService;
        private readonly IConfiguration _configuration;
        public SeguridadService(IConfiguration configuration, JwtService jwtService)
        {
            _configuration = configuration;
            ConnectionStrings = configuration.GetConnectionString("MVSystemSeguridad");
            this.jwtService = jwtService;

        }
        public DataTable ChangePassword(Usuario usuario)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionStrings);
                conn.Open();

                using var cmd = new SqlCommand("UPDATE [Usuarios] SET [Password] = @ClaveNueva WHERE [Codigo] = @CodUsuario", conn);
                cmd.Parameters.AddWithValue("@CodUsuario", usuario.Codigo);
                cmd.Parameters.AddWithValue("@ClaveNueva", AES.Encrypt(usuario.Password));

                int filasActualizadas = cmd.ExecuteNonQuery();
                
                // Retornar un DataTable vacío (mantener compatibilidad con la firma existente)
                DataTable dt = new();
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<CredentialsDTO> Login(UsuarioDTO usuarioIn)
        {
            using var dataTable = new DataTable();

            using var conn = new SqlConnection(ConnectionStrings);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT TOP (1) * FROM Usuarios WHERE [Login] = @userName AND [Estado] = 1", conn);
            cmd.Parameters.AddWithValue("@userName", usuarioIn.UserName);

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.Fill(dataTable);

            var usuarioDb = AccesoDatos.BindList<Usuario>(dataTable).FirstOrDefault();

            if (usuarioDb != null)
            {
                var empresa = await GetUserEmpresa(usuarioDb.Id_Empresa); // Obtener la empresa de manera asíncrona
                usuarioDb.Empresa = empresa.NombreEmpresa;
                usuarioDb.EmpresaMostrar = empresa.NombreEmpresaMostrar;
                
                // Obtener el Perfil de la tabla seg.Usuarios de SEGURIDAD (por Login)
                usuarioDb.Perfil = await GetPerfilUsuarioSeguridad(usuarioDb.Login);
            }

            if (usuarioDb == null || usuarioIn.Password != AES.Decrypt(usuarioDb.Password))
                return default;

            // Obtener roles desde SEGURIDAD
            var roles = await GetRolesSeguridad(usuarioDb.Login);
            return jwtService.BuildToken(usuarioDb, roles);
        }

        private async Task<int> GetPerfilUsuario(string login, string connectionString)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SELECT [Perfil] FROM [Usuarios] WHERE [Login] = @Login", conn);
                cmd.Parameters.AddWithValue("@Login", login);

                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? (int)result : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo perfil del usuario: {ex.Message}");
                return 0;
            }
        }

        public async Task<EmpresaDTO> GetUserEmpresa(int idEmpresa)
        {
            using var dataTable = new DataTable();
            var empresa = new EmpresaDTO(); // Crear una instancia de EmpresaDTO

            using var conn = new SqlConnection(ConnectionStrings);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
        SELECT TOP (1) 
            E.ID_Empresa, 
            E.Nombre_Empresa, 
            E.Nombre_Empresa_Mostrar 
        FROM Empresas E 
        JOIN Usuarios U ON U.Id_Empresa = E.ID_Empresa 
        WHERE U.Id_Empresa = @idEmpresa", conn);

            cmd.Parameters.AddWithValue("@idEmpresa", idEmpresa);

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.Fill(dataTable);

            var firstRow = dataTable.AsEnumerable().FirstOrDefault();
            if (firstRow != null)
            {
                // Asignar valores a la instancia de EmpresaDTO
                empresa = new EmpresaDTO
                {
                    IdEmpresa = firstRow.Field<int>("ID_Empresa"),
                    NombreEmpresa = firstRow.Field<string>("Nombre_Empresa"),
                    NombreEmpresaMostrar = firstRow.Field<string>("Nombre_Empresa_Mostrar")
                };
            }

            return empresa; // Retornar la instancia de EmpresaDTO
        }
        public DatosEmpresa GetDatosEmpresa(int idEmpresa, int ID_Sucursal)
        {
            try
            {
                

                // Creación del objeto DatosEmpresa
                DatosEmpresa DEF = new DatosEmpresa();
                DataTable dataTableEmpresa = new DataTable();
                DataTable dataTableSucursal = new DataTable();

                using (SqlConnection conn = new SqlConnection(ConnectionStrings))
                {
                    conn.Open();

                    // Primera consulta: Obtener datos de la empresa
                    string queryEmpresa = @"SELECT TOP (1) 
                               E.ID_Empresa,
                               E.Nombre_Empresa,
                               E.RNC,
                               E.StringCon
                            FROM [SEGURIDAD].[dbo].[Empresas] E
                            JOIN [SEGURIDAD].[dbo].Usuarios U ON U.Id_Empresa = E.ID_Empresa
                            WHERE U.Id_Empresa = @idEmpresa";

                    using (SqlCommand cmdEmpresa = new SqlCommand(queryEmpresa, conn))
                    {
                        cmdEmpresa.Parameters.AddWithValue("@idEmpresa", idEmpresa);
                        SqlDataAdapter dataAdapterEmpresa = new SqlDataAdapter(cmdEmpresa);
                        dataAdapterEmpresa.Fill(dataTableEmpresa);
                    }
                   
                    // Verificar si se encontraron datos de la empresa
                    if (dataTableEmpresa != null && dataTableEmpresa.Rows.Count > 0)
                    {
                        var row = dataTableEmpresa.Rows[0];
                        DEF.IdEmpresa = Convert.ToInt32(row["ID_Empresa"]);
                        DEF.Empresa = row["Nombre_Empresa"].ToString();
                        DEF.RNC = row["RNC"].ToString();
                        DEF.StringCon = row["StringCon"].ToString();
                    }
                    SqlConnection connection = new SqlConnection(DEF.StringCon);
                    // Segunda consulta: Obtener datos de la sucursal
                    string querySucursal = @"SELECT TOP (1)  
                                SC.Sucursal_Direccion,
                                SC.Sucursal_Telefonos,
                                SC.Sucursal_Encabezado,
                                SC.Sucursal_Condiciones_Garantia,
                                SC.Id_Empresa,
                                SC.Logo
                             FROM dbo.Sucursales_Cata SC
                             WHERE SC.Id_Empresa = @idEmpresa And ID_Sucursal =@ID_Sucursal ";

                    using (SqlCommand cmdSucursal = new SqlCommand(querySucursal, connection))
                    {
                        cmdSucursal.Parameters.AddWithValue("@idEmpresa", idEmpresa);
                        cmdSucursal.Parameters.AddWithValue("@ID_Sucursal", ID_Sucursal);
                        SqlDataAdapter dataAdapterSucursal = new SqlDataAdapter(cmdSucursal);
                        dataAdapterSucursal.Fill(dataTableSucursal);
                    }
                }

                // Verificar si se encontraron datos de la sucursal
                if (dataTableSucursal != null && dataTableSucursal.Rows.Count > 0)
                {
                    var row = dataTableSucursal.Rows[0];
                    DEF.Direccion = row["Sucursal_Direccion"].ToString();
                    DEF.Telefonos = row["Sucursal_Telefonos"].ToString();
                    DEF.Encabezado = row["Sucursal_Encabezado"].ToString();
                    DEF.CondicionesGarantia = row["Sucursal_Condiciones_Garantia"].ToString();
                    DEF.Logo = row["Logo"].ToString();
                    DEF.StringCon ="";
                }

                return DEF; // Retorna el objeto DatosEmpresa correctamente
            }
            catch (Exception ex)
            {
                // Opción recomendada: loguear el error en vez de lanzar la excepción directamente
                throw new Exception("Error al obtener los datos de la empresa", ex);
            }
        }
        public string GetConnStringByUser(string userName)
        {
            using var dataTable = new DataTable();

            using var conn = new SqlConnection(ConnectionStrings);
            conn.Open();

            using var cmd = new SqlCommand("SELECT TOP (1) StringCon FROM Empresas E JOIN Usuarios U ON U.Id_Empresa = E.ID_Empresa WHERE U.Login = @userName", conn);
            cmd.Parameters.AddWithValue("@userName", userName);

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.Fill(dataTable);

            var dataEnum = dataTable.AsEnumerable();
            return dataEnum.First()["StringCon"].ToString();
        }

        public async Task<List<RolSucursal>> GetRoles(string userName, string Conn)
        {
            using var dataTable = new DataTable();
            var roles = new List<RolSucursal>();
            
            using var conn = new SqlConnection(Conn);

            using var cmd = new SqlCommand(@"
                                             SELECT P.Nombre, U.id_Sucursal
                                             FROM [seg].[Usuarios] U 
                                             JOIN [seg].[PermisosUsuario] PU ON PU.Usuario = U.Codigo 
                                             JOIN [seg].[Permisos] P ON P.Codigo = PU.Permiso 
                                             WHERE U.Login = @userName 
                                               AND PU.Estado = 1", conn);

            cmd.Parameters.AddWithValue("@userName", userName);

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.Fill(dataTable);

            // Extraer los roles y sucursales y agregarlos a la lista
            roles = dataTable.AsEnumerable().Select(row => new RolSucursal
            {
                Nombre = row.Field<string>("Nombre"),
                IdSucursal = row.Field<int>("id_Sucursal")
            }).ToList();


            return roles;
        }

        private async Task<int> GetPerfilUsuarioSeguridad(string login)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionStrings);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SELECT [Perfil] FROM [Usuarios] WHERE [Login] = @Login", conn);
                cmd.Parameters.AddWithValue("@Login", login);

                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? (int)result : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo perfil del usuario desde SEGURIDAD: {ex.Message}");
                return 0;
            }
        }

        private async Task<List<RolSucursal>> GetRolesSeguridad(string userName)
        {
            using var dataTable = new DataTable();
            var roles = new List<RolSucursal>();
            
            using var conn = new SqlConnection(ConnectionStrings); // Usar SEGURIDAD

            using var cmd = new SqlCommand(@"
                                             SELECT P.Nombre, U.Codigo as id_Sucursal
                                             FROM [Usuarios] U 
                                             JOIN [seg].[PermisosUsuario] PU ON PU.Usuario = U.Codigo 
                                             JOIN [seg].[Permisos] P ON P.Codigo = PU.Permiso 
                                             WHERE U.Login = @userName 
                                               AND PU.Estado = 1", conn);

            cmd.Parameters.AddWithValue("@userName", userName);

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.Fill(dataTable);

            // Extraer los roles desde SEGURIDAD
            roles = dataTable.AsEnumerable().Select(row => new RolSucursal
            {
                Nombre = row.Field<string>("Nombre"),
                IdSucursal = row.Field<int>("id_Sucursal")
            }).ToList();

            return roles;
        }
    }

    public static class AES
    {
        public static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            byte[] saltBytes = passwordBytes;
            // Example:
            //saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;
            // Set your salt here to meet your flavor:
            byte[] saltBytes = passwordBytes;
            // Example:
            //saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        public static string Encrypt(string text)
        {
            byte[] originalBytes = Encoding.UTF8.GetBytes(text);
            byte[] encryptedBytes = null;

            // Hash the password with SHA256
            byte[] passwordBytes = SHA256.Create().ComputeHash(GetPasswordBytes());

            // Getting the salt size
            int saltSize = GetSaltSize(passwordBytes);
            // Generating salt bytes
            byte[] saltBytes = GetRandomBytes(saltSize);

            // Appending salt bytes to original bytes
            byte[] bytesToBeEncrypted = new byte[saltBytes.Length + originalBytes.Length];
            for (int i = 0; i < saltBytes.Length; i++)
            {
                bytesToBeEncrypted[i] = saltBytes[i];
            }
            for (int i = 0; i < originalBytes.Length; i++)
            {
                bytesToBeEncrypted[i + saltBytes.Length] = originalBytes[i];
            }

            encryptedBytes = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(string decryptedText)
        {
            byte[] bytesToBeDecrypted = Convert.FromBase64String(decryptedText);

            // Hash the password with SHA256
            byte[] passwordBytes = SHA256.Create().ComputeHash(GetPasswordBytes());

            byte[] decryptedBytes = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            // Getting the size of salt
            int saltSize = GetSaltSize(passwordBytes);

            // Removing salt bytes, retrieving original bytes
            byte[] originalBytes = new byte[decryptedBytes.Length - saltSize];
            for (int i = saltSize; i < decryptedBytes.Length; i++)
            {
                originalBytes[i - saltSize] = decryptedBytes[i];
            }

            return Encoding.UTF8.GetString(originalBytes);
        }

        public static int GetSaltSize(byte[] passwordBytes)
        {
            var key = new Rfc2898DeriveBytes(passwordBytes, passwordBytes, 1000);
            byte[] ba = key.GetBytes(2);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ba.Length; i++)
            {
                sb.Append(Convert.ToInt32(ba[i]).ToString());
            }
            int saltSize = 0;
            string s = sb.ToString();
            foreach (char c in s)
            {
                int intc = Convert.ToInt32(c.ToString());
                saltSize = saltSize + intc;
            }

            return saltSize;
        }

        public static byte[] GetRandomBytes(int length)
        {
            byte[] ba = new byte[length];
            RandomNumberGenerator.Create().GetBytes(ba);
            return ba;
        }

        public static byte[] GetPasswordBytes()
        {
            byte[] ba = null;

            ba = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            return SHA256.Create().ComputeHash(ba);
        }

        public static void Main()
        {
            var password = @"sunnek-tapraq-2Vunji";
            var passEncrypt = Encrypt(password);
            var decrypt = Decrypt(passEncrypt);
            Console.WriteLine(passEncrypt);
            Console.WriteLine(decrypt);
        }
    }
}
