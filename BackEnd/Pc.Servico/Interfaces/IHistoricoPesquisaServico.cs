using Pc.Dominio.Entities.Interacoes;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Interface do serviço de HistoricoPesquisa
    /// Define contrato para operações de negócio relacionadas ao histórico
    /// Inclui validações e organização de dados
    /// </summary>
    public interface IHistoricoPesquisaServico
    {
        /// <summary>
        /// Registra uma nova pesquisa no histórico
        /// Valida que o termo não está vazio
        /// </summary>
        Task<HistoricoPesquisa> RegistrarPesquisaAsync(HistoricoPesquisa historico);

        /// <summary>
        /// Obtém um registro de histórico por ID
        /// </summary>
        Task<HistoricoPesquisa?> ObterPorIdAsync(Guid id);

        /// <summary>
        /// Obtém todo o histórico de um cliente
        /// Ordenado por data descrescente
        /// </summary>
        Task<List<HistoricoPesquisa>> ObterHistoricoClienteAsync(Guid clienteId);

        /// <summary>
        /// Obtém os últimos N termos de pesquisa (para mostrar recentes)
        /// </summary>
        Task<List<HistoricoPesquisa>> ObterUltimosTermosAsync(Guid clienteId, int quantidade);

        /// <summary>
        /// Obtém sugestões de termos para autocomplete
        /// </summary>
        Task<List<HistoricoPesquisa>> ObterSugestoesAsync(string termoPartial);

        /// <summary>
        /// Limpa o histórico de um cliente
        /// </summary>
        Task LimparHistoricoAsync(Guid clienteId);

        /// <summary>
        /// Remove um registro específico do histórico
        /// </summary>
        Task RemoverAsync(Guid id);
    }
}
