using Microsoft.EntityFrameworkCore;
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
    }
}