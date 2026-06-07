using Pc.Dominio.Entities.Usuarios;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Interface do serviço de Lojista
    /// Define contrato para operações de negócio relacionadas a lojistas
    /// Inclui autenticação, gerenciamento de acesso a lojas e perfis
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    public interface ILojistaServico
    {
        /// <summary>
        /// Registra um novo lojista (signup)
        /// Validações: email único e senha mínima de 6 caracteres
        /// </summary>
        Task<Lojista> RegistrarAsync(Lojista lojista);

        /// <summary>
        /// Valida credenciais de login
        /// </summary>
        Task<Lojista?> ValidarLoginAsync(string email, string senha);

        /// <summary>
        /// Obtém lojista por ID
        /// </summary>
        Task<Lojista?> ObterPorIdAsync(Guid id);

        /// <summary>
        /// Obtém lojista por email
        /// </summary>
        Task<Lojista?> ObterPorEmailAsync(string email);

        /// <summary>
        /// Obtém todos os lojistas ativos
        /// </summary>
        Task<List<Lojista>> ListarAtivosAsync();

        /// <summary>
        /// Lista todos os lojistas de uma loja
        /// Útil para gerenciar múltiplos funcionários
        /// </summary>
        Task<List<Lojista>> ListarPorLojaAsync(Guid lojaId);

        /// <summary>
        /// Lista todos os lojistas
        /// </summary>
        Task<List<Lojista>> ListarAsync();

        /// <summary>
        /// Atualiza dados do lojista (cargo, etc)
        /// </summary>
        Task AtualizarAsync(Lojista lojista);

        /// <summary>
        /// Altera a senha do lojista
        /// </summary>
        Task AlterarSenhaAsync(Guid lojistaId, string senhaAtual, string novaSenha);

        /// <summary>
        /// Remove lojista (soft delete)
        /// </summary>
        Task RemoverAsync(Guid id);
    }
}
