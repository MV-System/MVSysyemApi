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
            _configuration =configuration;
            ConnectionStrings = configuration.GetConnectionString("MVSystemSeguridad");
            this.jwtService = jwtService;
         
        }
        public DataTable ChangePassword(Usuario usuario)
        {

            using var cn = new SqlConnection(ConnectionStrings);
            cn.Open();
            try
            {
                DataTable dt = new();

                using var conn = new SqlConnection(ConnectionStrings);
                conn.OpenAsync();

                using var cmd = new SqlCommand("UPDATE Usuarios SET Password = @ClaveNueva WHERE Codigo = @CodUsuario", conn);
                cmd.Parameters.AddWithValue("@CodUsuario", usuario.Codigo);
                cmd.Parameters.AddWithValue("@ClaveNueva", AES.Encrypt( usuario.Password));

                using var da = new SqlDataAdapter(cmd);
                cmd.ExecuteNonQuery();

                da.Fill(dt);
                cn.Close();
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
            usuarioDb.Empresa = GetUserEmpresa(usuarioDb.Id_Empresa);

            if (usuarioDb == default || usuarioIn.Password != AES.Decrypt(usuarioDb.Password))
                return default;

            var roles = await GetRoles(usuarioDb.Login);
            return jwtService.BuildToken(usuarioDb, roles);
        }
        public string GetUserEmpresa(int idEmpresa)
        {
            using var dataTable = new DataTable();

            using var conn = new SqlConnection(ConnectionStrings);
            conn.Open();

            using var cmd = new SqlCommand("SELECT TOP (1) " +
                "Nombre_Empresa FROM " +
                "Empresas E JOIN Usuarios U " +
                "ON U.Id_Empresa = E.ID_Empresa " +
                "WHERE U.Id_Empresa = @idEmpresa", conn);
            cmd.Parameters.AddWithValue("@idEmpresa", idEmpresa);

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.Fill(dataTable);

            var dataEnum = dataTable.AsEnumerable();
            return dataEnum.First()["Nombre_Empresa"].ToString();
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

        public async Task<List<string>> GetRoles(string userName)
        {
            using var dataTable = new DataTable();

            using var conn = new SqlConnection(_configuration.GetConnectionString("MVSystemSeguridad"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SELECT P.PermissionName " +
                "FROM [dbo].[Usuarios] U " +
                "JOIN [dbo].[PermisoUsuario] PU ON PU.IdUser = U.Codigo " +
                "JOIN [dbo].[Permisos] P ON P.Id = PU.IdPermiso " +
                "WHERE Login = @userName", conn);
            cmd.Parameters.AddWithValue("@userName", userName);

            using var dataAdapter = new SqlDataAdapter(cmd);
            dataAdapter.Fill(dataTable);

            return dataTable.AsEnumerable().Select(x => x["PermissionName"].ToString()).ToList();
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
