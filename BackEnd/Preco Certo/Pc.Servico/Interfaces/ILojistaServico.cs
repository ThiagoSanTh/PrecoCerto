using Pc.Dominio.Entities.Usuarios;

namespace Pc.Servico.Interfaces
{
    public interface ILojistaServico
    {
        Task<Lojista> RegistrarAsync(
            string nomeUsuario,
            string email,
            string senha,
            string? telefone = null,
            string? cargo = null);

        Task<Lojista?> ValidarLoginAsync(string email, string senha);
        Task<Lojista?> ObterPorIdAsync(Guid id);
    }
}
