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
    /// Implementação do repositório para HistoricoPesquisa
    /// Herda do Repositorio genérico e implementa métodos específicos de histórico
    /// Responsável apenas por acesso a dados sem validações de negócio
    /// </summary>
    public class HistoricoPesquisaRepositorio : Repositorio<HistoricoPesquisa>, IHistoricoPesquisaRepositorio
    {
        /// <summary>
        /// Construtor que injeta o contexto do banco de dados
        /// </summary>
        public HistoricoPesquisaRepositorio(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtém todo o histórico de pesquisa de um cliente
        /// Ordenado por data descrescente para mostrar pesquisas mais recentes primeiro
        /// </summary>
        public async Task<List<HistoricoPesquisa>> ObterPorClienteAsync(Guid clienteId)
        {
            return await _context.HistoricosPesquisa
                .Where(h => h.ClienteId == clienteId)
                .OrderByDescending(h => h.DataPesquisa)
                .ToListAsync();
        }

        /// <summary>
        /// Busca termos de pesquisa similares para implementar autocomplete
        /// Realiza busca case-insensitive no termo
        /// </summary>
        public async Task<List<HistoricoPesquisa>> BuscarPorTermoAsync(string termo)
        {
            return await _context.HistoricosPesquisa
                .Where(h => h.TermoPesquisa.ToLower().Contains(termo.ToLower()))
                .Distinct()
                .OrderByDescending(h => h.DataPesquisa)
                .ToListAsync();
        }

        /// <summary>
        /// Obtém os últimos N termos de pesquisa de um cliente
        /// Útil para mostrar pesquisas recentes na interface
        /// </summary>
        public async Task<List<HistoricoPesquisa>> ObterUltimosAsync(Guid clienteId, int quantidade)
        {
            return await _context.HistoricosPesquisa
                .Where(h => h.ClienteId == clienteId)
                .OrderByDescending(h => h.DataPesquisa)
                .Take(quantidade)
                .ToListAsync();
        }
    }
}
