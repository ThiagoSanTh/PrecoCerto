using Pc.Dominio.Entities.Usuarios;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Serviço do usuário (entidade unificada Usuario).
    /// Mantém o nome IClienteServico por compatibilidade com os controllers existentes.
    /// </summary>
    public interface IClienteServico
    {
        Task<Usuario> RegistrarAsync(Usuario cliente);

        Task<Usuario?> ValidarLoginAsync(string email, string senha);

        Task<Usuario?> ObterPorIdAsync(Guid id);

        Task<Usuario?> ObterComLojaAsync(Guid id);

        Task<Usuario?> ObterPorEmailAsync(string email);

        Task<List<Usuario>> ListarAtivosAsync();

        Task<List<Usuario>> ListarAsync();

        Task AtualizarAsync(Usuario cliente);

        Task AtualizarLocalizacaoAsync(Guid clienteId, decimal latitude, decimal longitude);

        Task<List<Usuario>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm);

        Task AlterarSenhaAsync(Guid clienteId, string senhaAtual, string novaSenha);

        Task RemoverAsync(Guid id);

        Task<string?> GerarTokenRecuperacaoSenhaAsync(string email);

        Task<bool> RedefinirSenhaComTokenAsync(string token, string novaSenha);

        Task AlterarEmailAsync(Guid usuarioId, string senhaAtual, string novoEmail);

        /// <summary>Marca um usuário como Lojista (após abrir loja com CNPJ válido).</summary>
        Task DefinirComoLojistaAsync(Guid usuarioId);

        /// <summary>
        /// Promove um cliente a Vendedor de uma loja (controle de estoque).
        /// Apenas o lojista dono da loja deve chamar (validação no controller).
        /// </summary>
        Task PromoverParaVendedorAsync(Guid usuarioId, Guid lojaId, string? cargo);

        /// <summary>Remove o vínculo de vendedor, voltando o usuário a Cliente.</summary>
        Task RemoverVendedorAsync(Guid usuarioId);

        /// <summary>Lista os vendedores vinculados a uma loja.</summary>
        Task<List<Usuario>> ListarVendedoresPorLojaAsync(Guid lojaId);
    }
}
