using Microsoft.Extensions.Logging;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    public class RagCascadeIndexador : IRagCascadeIndexador
    {
        public const int LimiteProdutosPorLoja = 500;
        public const int LimiteOfertasPorProduto = 200;
        public const int LimiteOfertasPorLoja = 500;

        private readonly IProdutoRepositorio _produtos;
        private readonly IOfertaRepositorio _ofertas;
        private readonly IRagIndexFila _fila;
        private readonly ILogger<RagCascadeIndexador> _logger;

        public RagCascadeIndexador(
            IProdutoRepositorio produtos,
            IOfertaRepositorio ofertas,
            IRagIndexFila fila,
            ILogger<RagCascadeIndexador> logger)
        {
            _produtos = produtos;
            _ofertas = ofertas;
            _fila = fila;
            _logger = logger;
        }

        public async Task EnfileirarLojaAtualizadaAsync(Guid lojaId, CancellationToken cancellationToken = default)
        {
            _fila.Enfileirar(RagDocumentoTipo.Loja, lojaId, RagIndexAcao.Indexar);

            var produtoIds = await _produtos.ListarIdsPorLojaAsync(lojaId, LimiteProdutosPorLoja);
            foreach (var pid in produtoIds)
                _fila.Enfileirar(RagDocumentoTipo.Produto, pid, RagIndexAcao.Indexar);

            var ofertaIds = await _ofertas.ListarIdsPorLojaAsync(lojaId, LimiteOfertasPorLoja);
            foreach (var oid in ofertaIds)
                _fila.Enfileirar(RagDocumentoTipo.Oferta, oid, RagIndexAcao.Indexar);

            _logger.LogInformation(
                "RAG cascata loja atualizada. Loja={Loja} Produtos={P} Ofertas={O}",
                lojaId, produtoIds.Count, ofertaIds.Count);
        }

        public async Task EnfileirarLojaRemovidaAsync(Guid lojaId, CancellationToken cancellationToken = default)
        {
            var produtoIds = await _produtos.ListarIdsPorLojaAsync(lojaId, LimiteProdutosPorLoja);
            var ofertaIds = await _ofertas.ListarIdsPorLojaAsync(lojaId, LimiteOfertasPorLoja);

            foreach (var oid in ofertaIds)
                _fila.Enfileirar(RagDocumentoTipo.Oferta, oid, RagIndexAcao.Remover);
            foreach (var pid in produtoIds)
                _fila.Enfileirar(RagDocumentoTipo.Produto, pid, RagIndexAcao.Remover);

            _fila.Enfileirar(RagDocumentoTipo.Loja, lojaId, RagIndexAcao.Remover);

            _logger.LogInformation(
                "RAG cascata loja removida. Loja={Loja} Produtos={P} Ofertas={O}",
                lojaId, produtoIds.Count, ofertaIds.Count);
        }

        public async Task EnfileirarProdutoAtualizadoAsync(Guid produtoId, CancellationToken cancellationToken = default)
        {
            _fila.Enfileirar(RagDocumentoTipo.Produto, produtoId, RagIndexAcao.Indexar);

            var ofertaIds = await _ofertas.ListarIdsPorProdutoAsync(produtoId, LimiteOfertasPorProduto);
            foreach (var oid in ofertaIds)
                _fila.Enfileirar(RagDocumentoTipo.Oferta, oid, RagIndexAcao.Indexar);

            _logger.LogInformation(
                "RAG cascata produto atualizado. Produto={Produto} Ofertas={O}",
                produtoId, ofertaIds.Count);
        }

        public async Task EnfileirarProdutoRemovidoAsync(Guid produtoId, CancellationToken cancellationToken = default)
        {
            var ofertaIds = await _ofertas.ListarIdsPorProdutoAsync(produtoId, LimiteOfertasPorProduto);
            foreach (var oid in ofertaIds)
                _fila.Enfileirar(RagDocumentoTipo.Oferta, oid, RagIndexAcao.Remover);

            _fila.Enfileirar(RagDocumentoTipo.Produto, produtoId, RagIndexAcao.Remover);

            _logger.LogInformation(
                "RAG cascata produto removido. Produto={Produto} Ofertas={O}",
                produtoId, ofertaIds.Count);
        }
    }
}
