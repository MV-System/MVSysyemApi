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

        public CredentialsDTO BuildToken(Usuario user, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Name, user.Login),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var rol in roles)
                claims.Add(new Claim("role", rol));

            var expires = DateTime.UtcNow.Date.AddDays(1);

            var tokenDescriptor = new JwtSecurityToken(_issuer, _issuer, claims, expires: expires, signingCredentials: _credentials);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            return new CredentialsDTO(expires, token);
        }
    }
}
