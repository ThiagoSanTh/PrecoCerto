using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes
{
    public class OfertaServico : IOfertaServico
    {
        private readonly IOfertaRepositorio _ofertaRepositorio;
        private readonly IRagIndexFila _ragFila;

        public OfertaServico(IOfertaRepositorio ofertaRepositorio, IRagIndexFila ragFila)
        {
            _ofertaRepositorio = ofertaRepositorio;
            _ragFila = ragFila;
        }

        public async Task<Oferta> AdicionarAsync(Oferta oferta)
        {
            if (oferta.Preco <= 0)
                throw new Exception("O preço da oferta deve ser maior que zero.");

            var criada = await _ofertaRepositorio.AdicionarAsync(oferta);
            _ragFila.Enfileirar(RagDocumentoTipo.Oferta, criada.Id, RagIndexAcao.Indexar);
            return criada;
        }

        public async Task<Oferta?> ObterPorIdAsync(Guid id)
        {
            return await _ofertaRepositorio.ObterPorIdAsync(id);
        }

        public async Task<List<Oferta>> ListarAsync()
        {
            return await _ofertaRepositorio.ListarAsync();
        }

        public Task<PaginacaoResultado<Oferta>> ListarPaginadoAsync(PaginacaoParametros paginacao) =>
            _ofertaRepositorio.ListarPaginadoAsync(paginacao);

        public async Task<List<Oferta>> ObterPorProdutoAsync(Guid produtoId)
        {
            return await _ofertaRepositorio.ObterPorProdutoAsync(produtoId);
        }

        public Task<List<Oferta>> ListarDisponiveisPorProdutosAsync(IEnumerable<Guid> produtoIds) =>
            _ofertaRepositorio.ListarDisponiveisPorProdutosAsync(produtoIds);

        public async Task AtualizarAsync(Oferta oferta)
        {
            if (oferta.Preco <= 0)
                throw new Exception("O preço da oferta deve ser maior que zero.");

            await _ofertaRepositorio.AtualizarAsync(oferta);
            _ragFila.Enfileirar(RagDocumentoTipo.Oferta, oferta.Id, RagIndexAcao.Indexar);
        }

        public async Task RemoverAsync(Guid id)
        {
            await _ofertaRepositorio.RemoverAsync(id);
            _ragFila.Enfileirar(RagDocumentoTipo.Oferta, id, RagIndexAcao.Remover);
        }
    }
}
