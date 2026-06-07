using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Interacoes;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    /// <summary>
    /// Implementação do repositório para Avaliacao
    /// Herda do Repositorio genérico e implementa métodos específicos de avaliação
    /// Fornece dados para cálculos de média, listagem por loja e cliente, etc
    /// </summary>
    public class AvaliacaoRepositorio : Repositorio<Avaliacao>, IAvaliacaoRepositorio
    {
        /// <summary>
        /// Construtor que injeta o contexto do banco de dados
        /// </summary>
        public AvaliacaoRepositorio(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtém todas as avaliações de uma loja
        /// Inclui dados do cliente para mostrar quem avaliou
        /// Ordenado por data descrescente
        /// </summary>
        public async Task<List<Avaliacao>> ObterPorLojaAsync(Guid lojaId)
        {
            return await _context.Avaliacoes
                .Where(a => a.LojaId == lojaId)
                .Include(a => a.Cliente)
                .OrderByDescending(a => a.DataAvaliacao)
                .ToListAsync();
        }

        /// <summary>
        /// Calcula a nota média de uma loja
        /// Retorna 0 se nenhuma avaliação existir
        /// </summary>
        public async Task<double> ObterMediaAvaliacaoAsync(Guid lojaId)
        {
            var avaliacoes = await _context.Avaliacoes
                .Where(a => a.LojaId == lojaId)
                .ToListAsync();

            if (avaliacoes.Count == 0)
                return 0;

            return avaliacoes.Average(a => a.Nota);
        }

        /// <summary>
        /// Obtém todas as avaliações realizadas por um cliente
        /// Inclui informações das lojas avaliadas
        /// Útil para histórico de avaliações do cliente
        /// </summary>
        public async Task<List<Avaliacao>> ObterPorClienteAsync(Guid clienteId)
        {
            return await _context.Avaliacoes
                .Where(a => a.ClienteId == clienteId)
                .Include(a => a.Loja)
                .OrderByDescending(a => a.DataAvaliacao)
                .ToListAsync();
        }

        /// <summary>
        /// Verifica se um cliente já avaliou uma loja (evita duplicatas)
        /// Retorna a avaliação existente se encontrada
        /// </summary>
        public async Task<Avaliacao?> VerificarAvaliacaoExistenteAsync(Guid clienteId, Guid lojaId)
        {
            return await _context.Avaliacoes
                .Include(a => a.Cliente)
                .Include(a => a.Loja)
                .FirstOrDefaultAsync(a => a.ClienteId == clienteId && a.LojaId == lojaId);
        }
    }
}
