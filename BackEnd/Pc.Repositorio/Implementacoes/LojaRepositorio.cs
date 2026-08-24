using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Infraestrutura;
using Pc.Repositorio.Comum;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class LojaRepositorio : Repositorio<Loja>, ILojaRepositorio
    {
        public LojaRepositorio(AppDbContext context) : base(context)
        {
        }

        private IQueryable<Loja> QueryComEndereco() =>
            _context.Lojas.AsNoTracking().Include(l => l.Endereco);

        public override async Task<Loja?> ObterPorIdAsync(Guid id)
        {
            return await QueryComEndereco()
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public override async Task<List<Loja>> ListarAsync()
        {
            return await QueryComEndereco().ToListAsync();
        }

        public async Task<PaginacaoResultado<Loja>> ListarPaginadoAsync(PaginacaoParametros paginacao)
        {
            var total = await _context.Lojas.AsNoTracking().CountAsync();
            var items = await QueryComEndereco()
                .OrderBy(l => l.NomeFantasia)
                .Skip(paginacao.Skip)
                .Take(paginacao.PageSize)
                .ToListAsync();

            return new PaginacaoResultado<Loja>
            {
                Items = items,
                Page = paginacao.Page,
                PageSize = paginacao.PageSize,
                Total = total
            };
        }

        public async Task<List<Loja>> BuscarPorNomeAsync(string nome)
        {
            var pattern = $"%{nome.Trim()}%";
            return await QueryComEndereco()
                .Where(l => EF.Functions.ILike(l.NomeFantasia, pattern))
                .ToListAsync();
        }

        public async Task<PaginacaoResultado<Loja>> BuscarPorNomePaginadoAsync(string nome, PaginacaoParametros paginacao)
        {
            var pattern = $"%{nome.Trim()}%";
            var filtrado = _context.Lojas.AsNoTracking()
                .Where(l => EF.Functions.ILike(l.NomeFantasia, pattern));
            var total = await filtrado.CountAsync();
            var items = await QueryComEndereco()
                .Where(l => EF.Functions.ILike(l.NomeFantasia, pattern))
                .OrderBy(l => l.NomeFantasia)
                .Skip(paginacao.Skip)
                .Take(paginacao.PageSize)
                .ToListAsync();

            return new PaginacaoResultado<Loja>
            {
                Items = items,
                Page = paginacao.Page,
                PageSize = paginacao.PageSize,
                Total = total
            };
        }

        public async Task<List<Loja>> ListarPorProximidadeAsync(
            decimal latitude, decimal longitude, decimal raioKm, int limite = 500)
        {
            var lojas = await QueryComEndereco()
                .Where(l => l.Endereco.Latitude != null && l.Endereco.Longitude != null)
                .ToListAsync();

            return lojas
                .Where(l => GeoHelper.CalcularDistanciaKm(
                    latitude, longitude,
                    l.Endereco.Latitude!.Value, l.Endereco.Longitude!.Value) <= raioKm)
                .OrderBy(l => GeoHelper.CalcularDistanciaKm(
                    latitude, longitude,
                    l.Endereco.Latitude!.Value, l.Endereco.Longitude!.Value))
                .Take(limite)
                .ToList();
        }

        public async Task<Loja?> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            return await QueryComEndereco()
                .FirstOrDefaultAsync(l => l.UsuarioId == usuarioId);
        }

        public override async Task<Loja> AdicionarAsync(Loja loja)
        {
            if (loja.Endereco is not null)
            {
                if (loja.EnderecoId == Guid.Empty)
                    loja.EnderecoId = loja.Endereco.Id;
            }

            await _dbSet.AddAsync(loja);
            await _context.SaveChangesAsync();
            return loja;
        }
    }
}
