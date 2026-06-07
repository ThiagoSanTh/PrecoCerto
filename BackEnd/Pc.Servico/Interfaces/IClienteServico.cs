using Pc.Dominio.Entities.Usuarios;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Interface do serviço de Cliente
    /// Define contrato para operações de negócio relacionadas a clientes
    /// Inclui autenticação, gerenciamento de localização e perfil
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    public interface IClienteServico
    {
        /// <summary>
        /// Registra um novo cliente (signup)
        /// Validações: email único, senha mínima 6 chars
        /// </summary>
        Task<Cliente> RegistrarAsync(Cliente cliente);

        /// <summary>
        /// Valida credenciais de login
        /// </summary>
        Task<Cliente?> ValidarLoginAsync(string email, string senha);

        /// <summary>
        /// Obtém cliente por ID
        /// </summary>
        Task<Cliente?> ObterPorIdAsync(Guid id);

        /// <summary>
        /// Obtém cliente por email
        /// </summary>
        Task<Cliente?> ObterPorEmailAsync(string email);

        /// <summary>
        /// Lista todos os clientes ativos
        /// </summary>
        Task<List<Cliente>> ListarAtivosAsync();

        /// <summary>
        /// Lista todos os clientes (inclui inativos)
        /// </summary>
        Task<List<Cliente>> ListarAsync();

        /// <summary>
        /// Atualiza dados do cliente
        /// </summary>
        Task AtualizarAsync(Cliente cliente);

        /// <summary>
        /// Atualiza localização atual do cliente (geolocalização)
        /// Recebe latitude/longitude do GPS
        /// </summary>
        Task AtualizarLocalizacaoAsync(Guid clienteId, decimal latitude, decimal longitude);

        /// <summary>
        /// Obtém clientes próximos (em um raio de geolocalização)
        /// Útil para recomendações de lojas próximas
        /// </summary>
        Task<List<Cliente>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm);

        /// <summary>
        /// Altera a senha do cliente
        /// </summary>
        Task AlterarSenhaAsync(Guid clienteId, string senhaAtual, string novaSenha);

        /// <summary>
        /// Remove cliente (soft delete)
        /// </summary>
        Task RemoverAsync(Guid id);
    }
}
