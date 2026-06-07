using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Interface para operações de repositório de Lojista
    /// Herda do IRepositorio genérico e adiciona métodos específicos
    /// Consolidado - sem intermediário Usuario
    /// </summary>
    public interface ILojistaRepositorio : IRepositorio<Lojista>
    {
        /// <summary>
        /// Obtém lojista por email (para login)
        /// </summary>
        Task<Lojista?> ObterPorEmailAsync(string email);

        /// <summary>
        /// Verifica se email já existe
        /// </summary>
        Task<bool> EmailExisteAsync(string email);

        /// <summary>
        /// Obtém todos os lojistas de uma loja
        /// </summary>
        Task<List<Lojista>> ListarPorLojaAsync(Guid lojaId);

        /// <summary>
        /// Lista todos os lojistas ativos
        /// </summary>
        Task<List<Lojista>> ListarAtivosAsync();
    }
}
