using Pc.Dominio.Comum;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;

namespace Pc.Servico.Implementacoes
{
    public class FeedServico : IFeedServico
    {
        private readonly IProdutoRepositorio _produtoRepositorio;
        private readonly IOfertaRepositorio _ofertaRepositorio;

        public FeedServico(IProdutoRepositorio produtoRepositorio, IOfertaRepositorio ofertaRepositorio)
        {
            _produtoRepositorio = produtoRepositorio;
            _ofertaRepositorio = ofertaRepositorio;
        }

        public async Task<PaginacaoResultado<FeedItem>> ListarAsync(
            PaginacaoParametros paginacao,
            string? termo = null,
            CategoriaProduto? categoria = null,
            Guid? lojaId = null)
        {
            PaginacaoResultado<Pc.Dominio.Entities.Catalogo.Produto> produtos;

            if (!string.IsNullOrWhiteSpace(termo))
            {
                produtos = await _produtoRepositorio.BuscarPorNomePaginadoAsync(termo, paginacao, lojaId);
            }
            else
            {
                produtos = await _produtoRepositorio.ListarPorLojaPaginadoAsync(paginacao, lojaId, categoria);
            }

            var ids = produtos.Items.Select(p => p.Id).ToList();
            var melhoresOfertas = await _ofertaRepositorio.ObterMelhorOfertaPorProdutosAsync(ids);

            var items = produtos.Items.Select(p =>
            {
                melhoresOfertas.TryGetValue(p.Id, out var oferta);
                var precoOferta = oferta?.Preco;
                var precoExibicao = precoOferta.HasValue && precoOferta.Value < p.Preco
                    ? precoOferta.Value
                    : p.Preco;

                return new FeedItem
                {
                    ProdutoId = p.Id,
                    Nome = p.NomeProduto,
                    ImagemUrl = p.ImagemUrl,
                    LojaId = p.LojaId,
                    LojaNome = oferta?.Loja?.NomeFantasia ?? p.Loja?.NomeFantasia,
                    PrecoBase = p.Preco,
                    PrecoExibicao = precoExibicao,
                    PrecoAnterior = oferta?.PrecoAnterior,
                    EmPromocao = oferta?.EmPromocao ?? false,
                    Categoria = p.Categoria
                };
            }).ToList();

            return new PaginacaoResultado<FeedItem>
            {
                Items = items,
                Page = produtos.Page,
                PageSize = produtos.PageSize,
                Total = produtos.Total
            };
        }
    }
}
