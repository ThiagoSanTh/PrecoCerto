using System.Security.Claims;
using Pc.Dominio.Enums;

namespace Pc.WebApi.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(Guid userId, TipoUsuario tipo, Guid? lojaId = null);
    }
}
