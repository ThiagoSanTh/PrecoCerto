using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class LojaRepositorio : Repositorio<Loja>, ILojaRepositorio
    {
        public LojaRepositorio(AppDbContext context) : base(context)
        {
        }

        public override async Task<Loja?> ObterPorIdAsync(Guid id)
        {
            return await _context.Lojas
                .Include(l => l.Endereco)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public override async Task<List<Loja>> ListarAsync()
        {
            return await _context.Lojas
                .Include(l => l.Endereco)
                .ToListAsync();
        }

        public async Task<List<Loja>> BuscarPorNomeAsync(string nome)
        {
            return await _context.Lojas
                .Include(l => l.Endereco)
                .Where(l => l.NomeFantasia.ToLower().Contains(nome.ToLower()))
                .ToListAsync();
        }
    }
}
