using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pc.Dominio.Entities.Interacoes;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Interface para operações de repositório de Favorito
    /// Herda do IRepositorio genérico e adiciona métodos específicos
    /// </summary>
    public interface IFavoritoRepositorio : IRepositorio<Favorito>
    {
        /// <summary>
        /// Obtém todos os favoritos de um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>Lista de favoritos do cliente</returns>
        Task<List<Favorito>> ObterPorClienteAsync(Guid clienteId);

        /// <summary>
        /// Verifica se um cliente tem um produto específico como favorito
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="produtoId">ID do produto</param>
        /// <returns>Favorito se existir, null caso contrário</returns>
        Task<Favorito?> ObterFavoritoProdutoAsync(Guid clienteId, Guid produtoId);

        /// <summary>
        /// Verifica se um cliente tem uma loja específica como favorito
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="lojaId">ID da loja</param>
        /// <returns>Favorito se existir, null caso contrário</returns>
        Task<Favorito?> ObterFavoritoLojaAsync(Guid clienteId, Guid lojaId);
    }
}
