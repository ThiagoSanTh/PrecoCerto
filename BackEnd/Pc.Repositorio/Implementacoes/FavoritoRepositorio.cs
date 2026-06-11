using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Interacoes;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    /// <summary>
    /// Implementação do repositório para Favorito
    /// Herda do Repositorio genérico e implementa métodos específicos de favorito
    /// Mantém separação de responsabilidades: apenas acesso a dados sem lógica de negócio
    /// </summary>
    public class FavoritoRepositorio : Repositorio<Favorito>, IFavoritoRepositorio
    {
        /// <summary>
        /// Construtor que injeta o contexto do banco de dados
        /// </summary>
        public FavoritoRepositorio(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtém todos os favoritos de um cliente
        /// Incluir dados relacionados (Produto, Loja) para evitar lazy loading
        /// </summary>
        public async Task<List<Favorito>> ObterPorClienteAsync(Guid clienteId)
        {
            return await QueryPorCliente(clienteId).ToListAsync();
        }

        public async Task<PaginacaoResultado<Favorito>> ObterPorClientePaginadoAsync(
            Guid clienteId, PaginacaoParametros paginacao)
        {
            var query = QueryPorCliente(clienteId);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.DataCriacao)
                .Skip(paginacao.Skip)
                .Take(paginacao.PageSize)
                .ToListAsync();

            return new PaginacaoResultado<Favorito>
            {
                Items = items,
                Page = paginacao.Page,
                PageSize = paginacao.PageSize,
                Total = total
            };
        }

        private IQueryable<Favorito> QueryPorCliente(Guid clienteId) =>
            _context.Favoritos
                .AsNoTracking()
                .Where(f => f.ClienteId == clienteId)
                .Include(f => f.Produto)
                .Include(f => f.Loja);

        /// <summary>
        /// Verifica se um cliente tem um produto específico como favorito
        /// Retorna o favorito exato ou null
        /// </summary>
        public async Task<Favorito?> ObterFavoritoProdutoAsync(Guid clienteId, Guid produtoId)
        {
            return await _context.Favoritos
                .Include(f => f.Produto)
                .Include(f => f.Loja)
                .FirstOrDefaultAsync(f => f.ClienteId == clienteId && f.ProdutoId == produtoId);
        }

        /// <summary>
        /// Verifica se um cliente tem uma loja específica como favorito
        /// Retorna o favorito exato ou null
        /// </summary>
        public async Task<Favorito?> ObterFavoritoLojaAsync(Guid clienteId, Guid lojaId)
        {
            return await _context.Favoritos
                .Include(f => f.Produto)
                .Include(f => f.Loja)
                .FirstOrDefaultAsync(f => f.ClienteId == clienteId && f.LojaId == lojaId);
        }
    }
}
