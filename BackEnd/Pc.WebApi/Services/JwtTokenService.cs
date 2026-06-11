using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pc.Dominio.Enums;
using Pc.WebApi.Configuration;

namespace Pc.WebApi.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public string GenerateToken(Guid userId, TipoUsuario tipo, Guid? lojaId = null)
        {
            var role = tipo switch
            {
                TipoUsuario.Cliente => "Cliente",
                TipoUsuario.Lojista => "Lojista",
                TipoUsuario.Vendedor => "Vendedor",
                TipoUsuario.Admin => "Admin",
                _ => "Cliente"
            };

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role),
            };

            if (lojaId.HasValue && lojaId.Value != Guid.Empty)
                claims.Add(new Claim("lojaId", lojaId.Value.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_settings.ExpirationHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
