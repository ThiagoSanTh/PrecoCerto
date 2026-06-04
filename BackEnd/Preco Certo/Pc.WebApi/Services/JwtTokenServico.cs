using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.WebApi.Configuration;

namespace Pc.WebApi.Services
{
    public class JwtTokenServico : IJwtTokenServico
    {
        private readonly JwtSettings _settings;

        public JwtTokenServico(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public string GerarToken(Guid usuarioId, string email, TipoUsuario tipo)
        {
            if (string.IsNullOrWhiteSpace(_settings.Key) || _settings.Key.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:Key deve ter pelo menos 32 caracteres. Configure em User Secrets ou variáveis de ambiente.");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, tipo.ToString()),
                new Claim("tipo", ((int)tipo).ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
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
