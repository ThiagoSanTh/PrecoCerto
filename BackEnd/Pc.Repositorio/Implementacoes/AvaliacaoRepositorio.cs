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
                .AsNoTracking()
                .Where(a => a.LojaId == lojaId)
                .Include(a => a.Cliente)
                .OrderByDescending(a => a.DataAvaliacao)
                .ToListAsync();
        }

        public async Task<double> ObterMediaAvaliacaoAsync(Guid lojaId)
        {
            var (media, _) = await ObterResumoPorLojaAsync(lojaId);
            return media;
        }

        public async Task<(double Media, int Quantidade)> ObterResumoPorLojaAsync(Guid lojaId)
        {
            var resumo = await _context.Avaliacoes
                .AsNoTracking()
                .Where(a => a.LojaId == lojaId)
                .GroupBy(a => a.LojaId)
                .Select(g => new { Media = g.Average(a => (double)a.Nota), Quantidade = g.Count() })
                .FirstOrDefaultAsync();

            return resumo is null ? (0, 0) : (resumo.Media, resumo.Quantidade);
        }

        public async Task<List<Avaliacao>> ObterPorClienteAsync(Guid clienteId)
        {
            return await _context.Avaliacoes
                .AsNoTracking()
                .Where(a => a.ClienteId == clienteId)
                .Include(a => a.Loja)
                .OrderByDescending(a => a.DataAvaliacao)
                .ToListAsync();
        }

        public async Task<Avaliacao?> VerificarAvaliacaoExistenteAsync(Guid clienteId, Guid lojaId)
        {
            return await _context.Avaliacoes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ClienteId == clienteId && a.LojaId == lojaId);
        }
    }
}
