using APIPartNet.DTOs.Auth;
using Microsoft.IdentityModel.Tokens;
using MVSystemApi.Model.Seguridad;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    public class JwtService
    {
        private readonly SigningCredentials _credentials;
        private readonly string _issuer;

        public JwtService(IConfiguration configuration)
        {
            _issuer = configuration["Jwt:Issuer"];
            var key = configuration["Jwt:Key"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            _credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        }

        public CredentialsDTO BuildToken(Usuario user, IEnumerable<RolSucursal> roles)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.NameId, user.Codigo.ToString()),
        new Claim(JwtRegisteredClaimNames.Name, user.Login),
        new Claim(JwtRegisteredClaimNames.GivenName, user.Nombre),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("Perfil", user.Perfil.ToString()),
    };

            // Añadir los roles al token (sin incluir sucursal en el arreglo de roles)
            foreach (var rol in roles)
            {
                claims.Add(new Claim("role", rol.Nombre));
            }

            // Añadir el ID de sucursal directamente de la tabla Usuarios (no de los roles)
            claims.Add(new Claim("idSucursal", user.Id_Sucursal.ToString()));

            claims.Add(new Claim("IdEmpresa", user.Id_Empresa.ToString()));
            claims.Add(new Claim("Empresa", user.Empresa));
            claims.Add(new Claim("NombreEmpresaMostrar", user.EmpresaMostrar));
            claims.Add(new Claim("Rnc", user.Rnc ?? string.Empty));

            var expires = DateTime.UtcNow.Date.AddDays(1);
            var tokenDescriptor = new JwtSecurityToken(_issuer, _issuer, claims, expires: expires, signingCredentials: _credentials);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            return new CredentialsDTO(expires, token);
        }
    }
}
