using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Interacoes;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class CarrinhoRepositorio : Repositorio<Carrinho>, ICarrinhoRepositorio
    {
        public CarrinhoRepositorio(AppDbContext context) : base(context)
        {
        }

        public async Task<Carrinho?> ObterPorClienteAsync(Guid clienteId)
        {
            return await _context.Carrinhos
                .Include(c => c.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(c => c.Itens)
                    .ThenInclude(i => i.Oferta)
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId);
        }

        public async Task<ItemCarrinho?> ObterItemAsync(Guid itemId)
        {
            return await _context.ItensCarrinho
                .Include(i => i.Produto)
                .Include(i => i.Oferta)
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<ItemCarrinho> AdicionarItemAsync(ItemCarrinho item)
        {
            await _context.ItensCarrinho.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task AtualizarItemAsync(ItemCarrinho item)
        {
            _context.ItensCarrinho.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task RemoverItemAsync(ItemCarrinho item)
        {
            _context.ItensCarrinho.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
