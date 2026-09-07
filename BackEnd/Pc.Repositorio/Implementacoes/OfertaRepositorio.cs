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

            var total = await _context.Ofertas.AsNoTracking().CountAsync();
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
                    .ThenInclude(l => l!.Endereco)
                .Where(o => o.ProdutoId == produtoId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Guid>> ListarIdsPorProdutoAsync(Guid produtoId, int limite = 200)
        {
            limite = Math.Clamp(limite, 1, 2000);
            return await _context.Ofertas.AsNoTracking()
                .Where(o => o.ProdutoId == produtoId)
                .OrderBy(o => o.Id)
                .Select(o => o.Id)
                .Take(limite)
                .ToListAsync();
        }

        public async Task<List<Guid>> ListarIdsPorLojaAsync(Guid lojaId, int limite = 500)
        {
            limite = Math.Clamp(limite, 1, 5000);
            return await _context.Ofertas.AsNoTracking()
                .Where(o => o.LojaId == lojaId)
                .OrderBy(o => o.Id)
                .Select(o => o.Id)
                .Take(limite)
                .ToListAsync();
        }

        public async Task<List<Oferta>> ListarDisponiveisPorProdutosAsync(IEnumerable<Guid> produtoIds)
        {
            var ids = produtoIds.Distinct().Take(20).ToList();
            if (ids.Count == 0)
                return new List<Oferta>();

            return await _context.Ofertas
                .AsNoTracking()
                .Include(o => o.Produto)
                .Include(o => o.Loja)
                    .ThenInclude(l => l!.Endereco)
                .Where(o => ids.Contains(o.ProdutoId) && o.Disponivel)
                .OrderBy(o => o.Preco)
                .Take(100)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, Oferta>> ObterMelhorOfertaPorProdutosAsync(IEnumerable<Guid> produtoIds)
        {
            var ids = produtoIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, Oferta>();

            var melhores = await _context.Ofertas
                .AsNoTracking()
                .Where(o => ids.Contains(o.ProdutoId) && o.Disponivel)
                .GroupBy(o => o.ProdutoId)
                .Select(g => g.OrderBy(o => o.Preco).First())
                .ToListAsync();

            return melhores.ToDictionary(o => o.ProdutoId);
        }
    }
}
