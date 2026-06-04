using Pc.Dominio.Entities.Usuarios;

namespace Pc.Servico.Interfaces
{
    public interface IClienteServico
    {
        Task<Cliente> RegistrarAsync(
            string nomeUsuario,
            string email,
            string senha,
            string? telefone = null,
            decimal? latitudeAtual = null,
            decimal? longitudeAtual = null);

        Task<Cliente?> ValidarLoginAsync(string email, string senha);
        Task<Cliente?> ObterPorIdAsync(Guid id);
        Task AlterarSenhaAsync(Guid id, string senhaAtual, string novaSenha);
    }
}
