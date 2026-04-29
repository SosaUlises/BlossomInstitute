using BlossomInstitute.Application.External;
using BlossomInstitute.Domain.Entidades.Usuario;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlossomInstitute.Infraestructure.GetTokenJWT
{
    public class GetTokenJWTService : IGetTokenJWTService
    {
        private readonly IConfiguration _configuration;

        public GetTokenJWTService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Execute(string userId, IEnumerable<string> roles, UsuarioEntity usuario)
        {
            var jwtKey = _configuration["Jwt_Key"];
            var jwtIssuer = _configuration["Jwt_Issuer"];
            var jwtAudience = _configuration["Jwt_Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Jwt_Key no está configurado.");

            if (jwtKey.Length < 32)
                throw new InvalidOperationException("Jwt_Key debe tener al menos 32 caracteres.");

            if (string.IsNullOrWhiteSpace(jwtIssuer))
                throw new InvalidOperationException("Jwt_Issuer no está configurado.");

            if (string.IsNullOrWhiteSpace(jwtAudience))
                throw new InvalidOperationException("Jwt_Audience no está configurado.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, usuario.Email ?? ""),
                new Claim("given_name", usuario.Nombre ?? ""),
                new Claim("family_name", usuario.Apellido ?? ""),
            };

            foreach (var r in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
                claims.Add(new Claim(ClaimTypes.Role, r));

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(3),
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
                Issuer = jwtIssuer,
                Audience = jwtAudience
            };

            var token = tokenHandler.CreateToken(descriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}
