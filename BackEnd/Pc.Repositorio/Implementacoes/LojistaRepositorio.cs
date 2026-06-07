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
    /// Implementação do repositório para Lojista
    /// Herda do Repositorio genérico e implementa métodos específicos
    /// Inclui dados relacionados (Usuario, Loja) para contexto completo
    /// </summary>
    public class LojistaRepositorio : Repositorio<Lojista>, ILojistaRepositorio
    {
        /// <summary>
        /// Construtor que injeta o contexto do banco de dados
        /// </summary>
        public LojistaRepositorio(AppDbContext context) : base(context)
        {
        }

    /// <summary>
    /// Obtém lojista por email (para login)
    /// Inclui dados da loja
    /// </summary>
    public async Task<Lojista?> ObterPorEmailAsync(string email)
    {
        return await _context.Lojistas
            .Include(l => l.Loja)
            .FirstOrDefaultAsync(l => l.Email.ToLower() == email.ToLower() && l.Ativo);
    }

    /// <summary>
    /// Verifica se email já existe
    /// </summary>
    public async Task<bool> EmailExisteAsync(string email)
    {
        return await _context.Lojistas
            .AnyAsync(l => l.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Lista todos os lojistas ativos
    /// Inclui dados da loja
    /// </summary>
    public async Task<List<Lojista>> ListarAtivosAsync()
    {
        return await _context.Lojistas
            .Where(l => l.Ativo)
            .Include(l => l.Loja)
            .ToListAsync();
    }

    /// <summary>
    /// Lista lojistas de uma loja específica
    /// Útil para gerenciar equipe da loja
    /// </summary>
    public async Task<List<Lojista>> ListarPorLojaAsync(Guid lojaId)
    {
        return await _context.Lojistas
            .Where(l => l.LojaId == lojaId && l.Ativo)
            .Include(l => l.Loja)
            .ToListAsync();
    }

    /// <summary>
    /// Lista todos os lojistas (inclui inativos)
    /// </summary>
    public async Task<List<Lojista>> ListarAsync()
    {
        return await _context.Lojistas
            .Include(l => l.Loja)
            .ToListAsync();
    }
    }
}
