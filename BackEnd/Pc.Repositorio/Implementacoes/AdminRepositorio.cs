using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Usuarios;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    /// <summary>
    /// Implementação do repositório para Admin
    /// Herda do Repositorio genérico e implementa métodos específicos
    /// Dados consolidados - sem intermediário Usuario
    /// </summary>
    public class AdminRepositorio : Repositorio<Admin>, IAdminRepositorio
    {
        public AdminRepositorio(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtém admin por email (para login)
        /// </summary>
        public async Task<Admin?> ObterPorEmailAsync(string email)
        {
            return await _context.Admins
                .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower() && a.Ativo);
        }

        /// <summary>
        /// Verifica se email já existe
        /// </summary>
        public async Task<bool> EmailExisteAsync(string email)
        {
            return await _context.Admins
                .AnyAsync(a => a.Email.ToLower() == email.ToLower());
        }

        /// <summary>
        /// Lista todos os admins ativos
        /// </summary>
        public async Task<List<Admin>> ListarAtivosAsync()
        {
            return await _context.Admins
                .Where(a => a.Ativo)
                .ToListAsync();
        }

        /// <summary>
        /// Lista todos os admins (inclui inativos)
        /// </summary>
        public async Task<List<Admin>> ListarAsync()
        {
            return await _context.Admins
                .ToListAsync();
        }
    }
}
