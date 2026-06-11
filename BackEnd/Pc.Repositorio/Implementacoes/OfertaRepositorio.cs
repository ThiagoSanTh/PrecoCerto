using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class OfertaRepositorio : Repositorio<Oferta>, IOfertaRepositorio
    {
        public OfertaRepositorio(AppDbContext context) : base(context)
        {
        }

        public override async Task<List<Oferta>> ListarAsync()
        {
            return await _context.Ofertas
                .Include(o => o.Produto)
                .Include(o => o.Loja)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PaginacaoResultado<Oferta>> ListarPaginadoAsync(PaginacaoParametros paginacao)
        {
            var query = _context.Ofertas
                .AsNoTracking()
                .Include(o => o.Produto)
                .Include(o => o.Loja);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.DataAtualizacaoPreco)
                .Skip(paginacao.Skip)
                .Take(paginacao.PageSize)
                .ToListAsync();

            return new PaginacaoResultado<Oferta>
            {
                Items = items,
                Page = paginacao.Page,
                PageSize = paginacao.PageSize,
                Total = total
            };
        }

        public override async Task<Oferta?> ObterPorIdAsync(Guid id)
        {
            return await _context.Ofertas
                .Include(o => o.Produto)
                .Include(o => o.Loja)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Oferta>> ObterPorProdutoAsync(Guid produtoId)
        {
            return await _context.Ofertas
                .Include(o => o.Produto)
                .Include(o => o.Loja)
                .Where(o => o.ProdutoId == produtoId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, Oferta>> ObterMelhorOfertaPorProdutosAsync(IEnumerable<Guid> produtoIds)
        {
            var ids = produtoIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, Oferta>();

            var ofertas = await _context.Ofertas
                .AsNoTracking()
                .Include(o => o.Loja)
                .Where(o => ids.Contains(o.ProdutoId) && o.Disponivel)
                .ToListAsync();

            return ofertas
                .GroupBy(o => o.ProdutoId)
                .ToDictionary(g => g.Key, g => g.OrderBy(o => o.Preco).First());
        }
    }
}
