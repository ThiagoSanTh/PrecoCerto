using Pc.Dominio.Enums;

namespace Pc.Servico.Interfaces
{
    public interface IJwtTokenServico
    {
        string GerarToken(Guid usuarioId, string email, TipoUsuario tipo);
    }
}
