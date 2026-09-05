using Pc.Dominio.Comum;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes
{
    public class ProdutoServico : IProdutoServico
    {
        private readonly IProdutoRepositorio _produtoRepositorio;
        private readonly IRagIndexFila _ragFila;

        public ProdutoServico(IProdutoRepositorio produtoRepositorio, IRagIndexFila ragFila)
        {
            _produtoRepositorio = produtoRepositorio;
            _ragFila = ragFila;
        }

        public async Task<Produto> AdicionarAsync(Produto produto)
        {
            if (string.IsNullOrWhiteSpace(produto.NomeProduto))
                throw new Exception("O nome do produto é obrigatório.");

            var criado = await _produtoRepositorio.AdicionarAsync(produto);
            _ragFila.Enfileirar(RagDocumentoTipo.Produto, criado.Id, RagIndexAcao.Indexar);
            return criado;
        }

        public async Task<Produto?> ObterPorIdAsync(Guid id)
        {
            return await _produtoRepositorio.ObterPorIdAsync(id);
        }

        public async Task<List<Produto>> ListarProdutosAsync(Guid? lojaId = null)
        {
            return await _produtoRepositorio.ListarPorLojaAsync(lojaId);
        }

        public Task<PaginacaoResultado<Produto>> ListarProdutosPaginadoAsync(
            PaginacaoParametros paginacao, Guid? lojaId = null, CategoriaProduto? categoria = null) =>
            _produtoRepositorio.ListarPorLojaPaginadoAsync(paginacao, lojaId, categoria);

        public async Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return new List<Produto>();

            return await _produtoRepositorio.BuscarPorNomeAsync(nome, lojaId);
        }

        public Task<PaginacaoResultado<Produto>> BuscarPorNomePaginadoAsync(
            string nome, PaginacaoParametros paginacao, Guid? lojaId = null) =>
            _produtoRepositorio.BuscarPorNomePaginadoAsync(nome, paginacao, lojaId);

        public async Task AtualizarAsync(Produto produto)
        {
            if (string.IsNullOrWhiteSpace(produto.NomeProduto))
                throw new Exception("O nome do produto é obrigatório.");

            await _produtoRepositorio.AtualizarAsync(produto);
            _ragFila.Enfileirar(RagDocumentoTipo.Produto, produto.Id, RagIndexAcao.Indexar);
        }

        public async Task RemoverAsync(Guid id)
        {
            await _produtoRepositorio.RemoverAsync(id);
            _ragFila.Enfileirar(RagDocumentoTipo.Produto, id, RagIndexAcao.Remover);
        }

        public async Task AtualizarPorLojaAsync(Guid id, Produto dados, Guid lojaId)
        {
            ValidarDadosProduto(dados);

            var existente = await _produtoRepositorio.ObterPorIdAsync(id);
            if (existente is null)
                throw new ProdutoOperacaoException("Produto não encontrado.");

            if (!PertenceALoja(existente, lojaId))
                throw new ProdutoOperacaoException(
                    "Somente a loja que cadastrou este produto pode editá-lo.",
                    acessoNegado: true);

            existente.NomeProduto = dados.NomeProduto;
            existente.Descricao = dados.Descricao;
            existente.Marca = dados.Marca;
            existente.CodigoBarras = dados.CodigoBarras;
            existente.Preco = dados.Preco;
            existente.Categoria = dados.Categoria;
            if (dados.ImagemUrl != null)
                existente.ImagemUrl = dados.ImagemUrl;

            var atualizado = await _produtoRepositorio.AtualizarCamposAsync(existente);
            if (!atualizado)
                throw new ProdutoOperacaoException("Produto não encontrado.");

            _ragFila.Enfileirar(RagDocumentoTipo.Produto, id, RagIndexAcao.Indexar);
        }

        public async Task RemoverPorLojaAsync(Guid id, Guid lojaId)
        {
            var existente = await _produtoRepositorio.ObterPorIdAsync(id);
            if (existente is null)
                throw new ProdutoOperacaoException("Produto não encontrado.");

            if (!PertenceALoja(existente, lojaId))
                throw new ProdutoOperacaoException(
                    "Somente a loja que cadastrou este produto pode excluí-lo.",
                    acessoNegado: true);

            var removido = await _produtoRepositorio.RemoverPorIdAsync(id);
            if (!removido)
                throw new ProdutoOperacaoException("Produto não encontrado.");

            _ragFila.Enfileirar(RagDocumentoTipo.Produto, id, RagIndexAcao.Remover);
        }

        private static void ValidarDadosProduto(Produto produto)
        {
            if (string.IsNullOrWhiteSpace(produto.NomeProduto))
                throw new Exception("O nome do produto é obrigatório.");
        }

        private static bool PertenceALoja(Produto produto, Guid lojaId) =>
            produto.LojaId.HasValue && produto.LojaId.Value == lojaId;
    }
}
