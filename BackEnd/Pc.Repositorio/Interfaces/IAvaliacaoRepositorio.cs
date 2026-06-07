using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pc.Dominio.Entities.Interacoes;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Interface para operações de repositório de Avaliacao
    /// Herda do IRepositorio genérico e adiciona métodos específicos
    /// </summary>
    public interface IAvaliacaoRepositorio : IRepositorio<Avaliacao>
    {
        /// <summary>
        /// Obtém todas as avaliações de uma loja
        /// </summary>
        /// <param name="lojaId">ID da loja</param>
        /// <returns>Lista de avaliações da loja</returns>
        Task<List<Avaliacao>> ObterPorLojaAsync(Guid lojaId);

        /// <summary>
        /// Obtém a avaliação média de uma loja
        /// </summary>
        /// <param name="lojaId">ID da loja</param>
        /// <returns>Nota média da loja</returns>
        Task<double> ObterMediaAvaliacaoAsync(Guid lojaId);

        /// <summary>
        /// Obtém todas as avaliações feitas por um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>Lista de avaliações do cliente</returns>
        Task<List<Avaliacao>> ObterPorClienteAsync(Guid clienteId);

        /// <summary>
        /// Verifica se um cliente já avaliou uma loja específica
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="lojaId">ID da loja</param>
        /// <returns>Avaliação se existir, null caso contrário</returns>
        Task<Avaliacao?> VerificarAvaliacaoExistenteAsync(Guid clienteId, Guid lojaId);
    }
}
