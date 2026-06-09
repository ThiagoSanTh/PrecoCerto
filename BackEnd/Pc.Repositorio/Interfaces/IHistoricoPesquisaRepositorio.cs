using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pc.Dominio.Entities.Interacoes;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Interface para operações de repositório de HistoricoPesquisa
    /// Herda do IRepositorio genérico e adiciona métodos específicos
    /// </summary>
    public interface IHistoricoPesquisaRepositorio : IRepositorio<HistoricoPesquisa>
    {
        /// <summary>
        /// Obtém todo o histórico de pesquisa de um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>Lista de histórico de pesquisa ordenada por data descrescente</returns>
        Task<List<HistoricoPesquisa>> ObterPorClienteAsync(Guid clienteId);

        /// <summary>
        /// Busca termos de pesquisa similares (para autocomplete)
        /// </summary>
        /// <param name="termo">Termo parcial a buscar</param>
        /// <returns>Lista de históricos contendo o termo</returns>
        Task<List<HistoricoPesquisa>> BuscarPorTermoAsync(string termo);

        /// <summary>
        /// Obtém os últimos N termos de pesquisa de um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="quantidade">Quantidade de registros a retornar</param>
        /// <returns>Lista com os últimos termos pesquisados</returns>
        Task<List<HistoricoPesquisa>> ObterUltimosAsync(Guid clienteId, int quantidade);

        /// <summary>
        /// Obtém as pesquisas vinculadas a uma loja (diretamente ou via produto da loja).
        /// Usado para relatórios de BI do lojista.
        /// </summary>
        /// <param name="lojaId">ID da loja</param>
        /// <returns>Lista de históricos relacionados à loja</returns>
        Task<List<HistoricoPesquisa>> ObterPorLojaAsync(Guid lojaId);
    }
}
