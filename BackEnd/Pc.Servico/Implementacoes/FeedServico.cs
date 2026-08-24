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

        public FeedServico(IProdutoRepositorio produtoRepositorio)
        {
            _produtoRepositorio = produtoRepositorio;
        }

        public async Task<PaginacaoResultado<FeedItem>> ListarAsync(
            PaginacaoParametros paginacao,
            string? termo = null,
            CategoriaProduto? categoria = null,
            Guid? lojaId = null)
        {
            var produtos = await _produtoRepositorio.ListarFeedPaginadoAsync(
                paginacao, termo, categoria, lojaId);

            return new PaginacaoResultado<FeedItem>
            {
                Items = produtos.Items.Select(p => new FeedItem
                {
                    ProdutoId = p.ProdutoId,
                    Nome = p.Nome,
                    ImagemUrl = p.ImagemUrl,
                    LojaId = p.LojaId,
                    LojaNome = p.LojaNome,
                    PrecoBase = p.PrecoBase,
                    PrecoExibicao = p.PrecoExibicao,
                    PrecoAnterior = p.PrecoAnterior,
                    EmPromocao = p.EmPromocao,
                    Categoria = p.Categoria
                }).ToList(),
                Page = produtos.Page,
                PageSize = produtos.PageSize,
                Total = produtos.Total
            };
        }
    }
}
