using Pc.Dominio.Entities.Usuarios;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Interface do serviço de Admin
    /// Define contrato para operações de negócio relacionadas a administradores
    /// Inclui autenticação, gerenciamento de permissões
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    public interface IAdminServico
    {
        /// <summary>
        /// Registra um novo administrador (setup - apenas para superadmins)
        /// Validações: email único, senha mínima 6 chars
        /// </summary>
        Task<Admin> RegistrarAsync(Admin admin);

        /// <summary>
        /// Valida credenciais de login
        /// </summary>
        Task<Admin?> ValidarLoginAsync(string email, string senha);

        /// <summary>
        /// Obtém admin por ID
        /// </summary>
        Task<Admin?> ObterPorIdAsync(Guid id);

        /// <summary>
        /// Obtém admin por email
        /// </summary>
        Task<Admin?> ObterPorEmailAsync(string email);

        /// <summary>
        /// Obtém todos os admins ativos
        /// </summary>
        Task<List<Admin>> ListarAtivosAsync();

        /// <summary>
        /// Lista todos os admins (inclui inativos)
        /// </summary>
        Task<List<Admin>> ListarAsync();

        /// <summary>
        /// Atualiza dados do admin (permissões, nível de acesso, etc)
        /// </summary>
        Task AtualizarAsync(Admin admin);

        /// <summary>
        /// Altera a senha do admin
        /// </summary>
        Task AlterarSenhaAsync(Guid adminId, string senhaAtual, string novaSenha);

        /// <summary>
        /// Remove admin (soft delete)
        /// </summary>
        Task RemoverAsync(Guid id);
    }
}
