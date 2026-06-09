using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Catalogo;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;

namespace Pc.Repositorio.Implementacoes
{
    public class ProdutoRepositorio : Repositorio<Produto>, IProdutoRepositorio
    {
        public ProdutoRepositorio(AppDbContext context) : base(context)
        {
        }

        private IQueryable<Produto> QueryComLojaEEndereco()
        {
            return _context.Produtos
                .Include(p => p.Loja)
                    .ThenInclude(l => l!.Endereco);
        }

        public override async Task<Produto?> ObterPorIdAsync(Guid id)
        {
            return await QueryComLojaEEndereco()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Produto>> ListarPorLojaAsync(Guid? lojaId = null)
        {
            var query = QueryComLojaEEndereco();

            if (lojaId.HasValue)
                query = query.Where(p => p.LojaId == lojaId);

            return await query.OrderBy(p => p.NomeProduto).ToListAsync();
        }

        public async Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null)
        {
            var termo = nome.Trim().ToLower();
            var query = QueryComLojaEEndereco()
                .Where(p => p.NomeProduto.ToLower().Contains(termo)
                    || (p.Marca != null && p.Marca.ToLower().Contains(termo))
                    || (p.Descricao != null && p.Descricao.ToLower().Contains(termo)));

            if (lojaId.HasValue)
                query = query.Where(p => p.LojaId == lojaId);

            return await query.OrderBy(p => p.NomeProduto).ToListAsync();
        }

        public async Task<bool> AtualizarCamposAsync(Produto produto)
        {
            var existente = await _context.Produtos.FindAsync(produto.Id);
            if (existente is null)
                return false;

            existente.NomeProduto = produto.NomeProduto;
            existente.Descricao = produto.Descricao;
            existente.Marca = produto.Marca;
            existente.CodigoBarras = produto.CodigoBarras;
            existente.Preco = produto.Preco;
            existente.ImagemUrl = produto.ImagemUrl;
            existente.Categoria = produto.Categoria;
            existente.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoverPorIdAsync(Guid id)
        {
            var existente = await _context.Produtos.FindAsync(id);
            if (existente is null)
                return false;

            _context.Produtos.Remove(existente);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
