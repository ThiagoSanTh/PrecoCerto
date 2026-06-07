using Pc.Dominio.Entities.Interacoes;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Interface do serviço de PreferenciaCliente
    /// Define contrato para operações de negócio relacionadas a preferências
    /// Gerencia configurações do usuário com padrão chave-valor
    /// </summary>
    public interface IPreferenciaClienteServico
    {
        /// <summary>
        /// Adiciona ou atualiza uma preferência
        /// Usa padrão chave-valor para armazenar configurações
        /// </summary>
        Task<PreferenciaCliente> SalvarAsync(PreferenciaCliente preferencia);

        /// <summary>
        /// Obtém uma preferência específica
        /// </summary>
        Task<PreferenciaCliente?> ObterAsync(Guid id);

        /// <summary>
        /// Obtém todas as preferências de um cliente
        /// </summary>
        Task<List<PreferenciaCliente>> ListarPorClienteAsync(Guid clienteId);

        /// <summary>
        /// Obtém o valor de uma preferência específica
        /// Retorna null se não existir
        /// </summary>
        Task<string?> ObterValorAsync(Guid clienteId, string chave);

        /// <summary>
        /// Atualiza o valor de uma preferência existente
        /// </summary>
        Task AtualizarAsync(Guid clienteId, string chave, string valor);

        /// <summary>
        /// Remove uma preferência
        /// </summary>
        Task RemoverAsync(Guid id);

        /// <summary>
        /// Remove uma preferência por cliente e chave
        /// </summary>
        Task RemoverPorChaveAsync(Guid clienteId, string chave);

        /// <summary>
        /// Limpa todas as preferências de um cliente
        /// </summary>
        Task LimparTudasAsync(Guid clienteId);
    }
}
