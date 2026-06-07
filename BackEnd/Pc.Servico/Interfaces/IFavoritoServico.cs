using Pc.Dominio.Entities.Interacoes;

namespace Pc.Servico.Interfaces
{
    /// <summary>
    /// Interface do serviço de Favorito
    /// Define contrato para operações de negócio relacionadas a favoritos
    /// Inclui validações e regras de negócio específicas
    /// </summary>
    public interface IFavoritoServico
    {
        /// <summary>
        /// Adiciona um novo favorito
        /// Valida que ao menos um de Produto ou Loja está preenchido
        /// </summary>
        Task<Favorito> AdicionarAsync(Favorito favorito);

        /// <summary>
        /// Obtém um favorito por ID
        /// </summary>
        Task<Favorito?> ObterPorIdAsync(Guid id);

        /// <summary>
        /// Lista todos os favoritos de um cliente
        /// </summary>
        Task<List<Favorito>> ListarPorClienteAsync(Guid clienteId);

        /// <summary>
        /// Remove um favorito
        /// </summary>
        Task RemoverAsync(Guid id);

        /// <summary>
        /// Remove um favorito de produto específico
        /// </summary>
        Task RemoverFavoritoProdutoAsync(Guid clienteId, Guid produtoId);

        /// <summary>
        /// Remove um favorito de loja específica
        /// </summary>
        Task RemoverFavoritoLojaAsync(Guid clienteId, Guid lojaId);

        /// <summary>
        /// Verifica se um item é favorito
        /// </summary>
        Task<bool> EhFavoritoAsync(Guid clienteId, Guid? produtoId, Guid? lojaId);
    }
}
