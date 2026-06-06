using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Interface para operações de repositório de Cliente
    /// Herda do IRepositorio genérico e adiciona métodos específicos
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    public interface IClienteRepositorio : IRepositorio<Cliente>
    {
        /// <summary>
        /// Obtém cliente por email (para login)
        /// </summary>
        Task<Cliente?> ObterPorEmailAsync(string email);

        /// <summary>
        /// Obtém clientes por proximidade (geolocalização)
        /// </summary>
        Task<List<Cliente>> ObterPorProximidadeAsync(decimal latitude, decimal longitude, decimal raioKm);

        /// <summary>
        /// Atualiza a localização atual do cliente
        /// </summary>
        Task AtualizarLocalizacaoAsync(Guid clienteId, decimal latitude, decimal longitude);

        /// <summary>
        /// Lista todos os clientes ativos
        /// </summary>
        Task<List<Cliente>> ListarAtivosAsync();

        /// <summary>
        /// Atualiza apenas o UltimoLogin (evita UPDATE completo no login)
        /// </summary>
        Task AtualizarUltimoLoginAsync(Guid clienteId, DateTime ultimoLogin);
    }
}
