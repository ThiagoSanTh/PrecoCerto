using Pc.Dominio.Entities.Usuarios;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pc.Repositorio.Interfaces
{
    /// <summary>
    /// Interface do Repositório de Admins
    /// Define operações de acesso a dados para a entidade Admin
    /// </summary>
    public interface IAdminRepositorio : IRepositorio<Admin>
    {
        /// <summary>
        /// Obtém admin por email (para login)
        /// </summary>
        Task<Admin?> ObterPorEmailAsync(string email);

        /// <summary>
        /// Verifica se email já existe
        /// </summary>
        Task<bool> EmailExisteAsync(string email);

        /// <summary>
        /// Lista todos os admins ativos
        /// </summary>
        Task<List<Admin>> ListarAtivosAsync();
    }
}

