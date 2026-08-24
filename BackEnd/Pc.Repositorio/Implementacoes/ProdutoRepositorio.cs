using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Enums;
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
                .AsNoTracking()
                .Include(p => p.Loja)
                    .ThenInclude(l => l!.Endereco);
        }

        private IQueryable<Produto> QueryComLoja()
        {
            return _context.Produtos
                .AsNoTracking()
                .Include(p => p.Loja);
        }

        private static IQueryable<Produto> AplicarFiltroTermo(IQueryable<Produto> query, string termo)
        {
            var pattern = $"%{termo.Trim()}%";
            return query.Where(p =>
                EF.Functions.ILike(p.NomeProduto, pattern) ||
                (p.Marca != null && EF.Functions.ILike(p.Marca, pattern)) ||
                (p.Descricao != null && EF.Functions.ILike(p.Descricao, pattern)));
        }

        private static IQueryable<Produto> AplicarFiltros(
            IQueryable<Produto> query,
            Guid? lojaId,
            CategoriaProduto? categoria)
        {
            if (lojaId.HasValue)
                query = query.Where(p => p.LojaId == lojaId);

            if (categoria.HasValue)
                query = query.Where(p => p.Categoria == categoria.Value);

            return query;
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

        public async Task<PaginacaoResultado<Produto>> ListarPorLojaPaginadoAsync(
            PaginacaoParametros paginacao,
            Guid? lojaId = null,
            CategoriaProduto? categoria = null)
        {
            var filtrado = AplicarFiltros(_context.Produtos.AsNoTracking(), lojaId, categoria);
            var total = await filtrado.CountAsync();
            var items = await AplicarFiltros(QueryComLoja(), lojaId, categoria)
                .OrderBy(p => p.NomeProduto)
                .Skip(paginacao.Skip)
                .Take(paginacao.PageSize)
                .ToListAsync();

            return new PaginacaoResultado<Produto>
            {
                Items = items,
                Page = paginacao.Page,
                PageSize = paginacao.PageSize,
                Total = total
            };
        }

        public async Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return new List<Produto>();

            var query = AplicarFiltroTermo(QueryComLojaEEndereco(), nome);
            if (lojaId.HasValue)
                query = query.Where(p => p.LojaId == lojaId);

            return await query.OrderBy(p => p.NomeProduto).Take(50).ToListAsync();
        }

        public async Task<PaginacaoResultado<Produto>> BuscarPorNomePaginadoAsync(
            string nome,
            PaginacaoParametros paginacao,
            Guid? lojaId = null)
        {
            if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length < 2)
            {
                return new PaginacaoResultado<Produto>
                {
                    Items = Array.Empty<Produto>(),
                    Page = paginacao.Page,
                    PageSize = paginacao.PageSize,
                    Total = 0
                };
            }

            var filtrado = AplicarFiltroTermo(AplicarFiltros(_context.Produtos.AsNoTracking(), lojaId, null), nome);
            var total = await filtrado.CountAsync();
            var items = await AplicarFiltroTermo(AplicarFiltros(QueryComLoja(), lojaId, null), nome)
                .OrderBy(p => p.NomeProduto)
                .Skip(paginacao.Skip)
                .Take(paginacao.PageSize)
                .ToListAsync();

            return new PaginacaoResultado<Produto>
            {
                Items = items,
                Page = paginacao.Page,
                PageSize = paginacao.PageSize,
                Total = total
            };
        }

        public async Task<PaginacaoResultado<FeedProdutoLinha>> ListarFeedPaginadoAsync(
            PaginacaoParametros paginacao,
            string? termo = null,
            CategoriaProduto? categoria = null,
            Guid? lojaId = null)
        {
            IQueryable<Produto> query;
            if (!string.IsNullOrWhiteSpace(termo))
            {
                if (termo.Trim().Length < 2)
                {
                    return new PaginacaoResultado<FeedProdutoLinha>
                    {
                        Items = Array.Empty<FeedProdutoLinha>(),
                        Page = paginacao.Page,
                        PageSize = paginacao.PageSize,
                        Total = 0
                    };
                }

                query = AplicarFiltroTermo(
                    AplicarFiltros(_context.Produtos.AsNoTracking(), lojaId, null),
                    termo);
            }
            else
            {
                query = AplicarFiltros(_context.Produtos.AsNoTracking(), lojaId, categoria);
            }

            var total = await query.CountAsync();
            var linhas = await query
                .OrderBy(p => p.NomeProduto)
                .Skip(paginacao.Skip)
                .Take(paginacao.PageSize)
                .Select(p => new
                {
                    p.Id,
                    p.NomeProduto,
                    p.ImagemUrl,
                    p.LojaId,
                    ProdutoLojaNome = p.Loja != null ? p.Loja.NomeFantasia : null,
                    p.Preco,
                    p.Categoria,
                    Melhor = _context.Ofertas
                        .Where(o => o.ProdutoId == p.Id && o.Disponivel)
                        .OrderBy(o => o.Preco)
                        .Select(o => new
                        {
                            o.Preco,
                            o.PrecoAnterior,
                            o.EmPromocao,
                            LojaNome = o.Loja != null ? o.Loja.NomeFantasia : null
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var items = linhas.Select(p =>
            {
                var precoOferta = p.Melhor?.Preco;
                var precoExibicao = precoOferta.HasValue && precoOferta.Value < p.Preco
                    ? precoOferta.Value
                    : p.Preco;

                return new FeedProdutoLinha
                {
                    ProdutoId = p.Id,
                    Nome = p.NomeProduto,
                    ImagemUrl = p.ImagemUrl,
                    LojaId = p.LojaId,
                    LojaNome = p.Melhor?.LojaNome ?? p.ProdutoLojaNome,
                    PrecoBase = p.Preco,
                    PrecoExibicao = precoExibicao,
                    PrecoAnterior = p.Melhor?.PrecoAnterior,
                    EmPromocao = p.Melhor?.EmPromocao ?? false,
                    Categoria = p.Categoria
                };
            }).ToList();

            return new PaginacaoResultado<FeedProdutoLinha>
            {
                Items = items,
                Page = paginacao.Page,
                PageSize = paginacao.PageSize,
                Total = total
            };
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
