using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pc.Dominio.Entities.Interacoes;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Interface para operações de repositório de PreferenciaCliente
    /// Herda do IRepositorio genérico e adiciona métodos específicos
    /// </summary>
    public interface IPreferenciaClienteRepositorio : IRepositorio<PreferenciaCliente>
    {
        /// <summary>
        /// Obtém todas as preferências de um cliente
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>Lista de preferências do cliente</returns>
        Task<List<PreferenciaCliente>> ObterPorClienteAsync(Guid clienteId);

        /// <summary>
        /// Obtém o valor de uma preferência específica
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="chave">Chave da preferência</param>
        /// <returns>PreferenciaCliente se existir, null caso contrário</returns>
        Task<PreferenciaCliente?> ObterPorChaveAsync(Guid clienteId, string chave);

        /// <summary>
        /// Busca preferências por chave (útil para configurações comuns)
        /// </summary>
        /// <param name="chave">Chave da preferência a buscar</param>
        /// <returns>Lista de preferências com essa chave</returns>
        Task<List<PreferenciaCliente>> BuscarPorChaveAsync(string chave);
    }
}
