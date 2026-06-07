using Pc.Dominio.Entities.Interacoes;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Interface do serviço de Avaliacao
    /// Define contrato para operações de negócio relacionadas a avaliações
    /// Inclui validações de nota, duplicidade, etc
    /// </summary>
    public interface IAvaliacaoServico
    {
        /// <summary>
        /// Adiciona uma nova avaliação
        /// Valida nota entre 1-5 e evita duplicatas
        /// </summary>
        Task<Avaliacao> AdicionarAsync(Avaliacao avaliacao);

        /// <summary>
        /// Obtém uma avaliação por ID
        /// </summary>
        Task<Avaliacao?> ObterPorIdAsync(Guid id);

        /// <summary>
        /// Obtém todas as avaliações de uma loja
        /// </summary>
        Task<List<Avaliacao>> ListarPorLojaAsync(Guid lojaId);

        /// <summary>
        /// Obtém todas as avaliações de um cliente
        /// </summary>
        Task<List<Avaliacao>> ListarPorClienteAsync(Guid clienteId);

        /// <summary>
        /// Calcula a nota média de uma loja
        /// </summary>
        Task<double> ObterMediaAvaliacaoAsync(Guid lojaId);

        /// <summary>
        /// Atualiza uma avaliação existente
        /// Valida que cliente tem permissão para atualizar
        /// </summary>
        Task AtualizarAsync(Avaliacao avaliacao);

        /// <summary>
        /// Remove uma avaliação
        /// </summary>
        Task RemoverAsync(Guid id);

        /// <summary>
        /// Obtém a contagem de avaliações de uma loja
        /// </summary>
        Task<int> ObterQuantidadeAvaliacoesAsync(Guid lojaId);
    }
}
