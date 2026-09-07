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
        private readonly IRagCascadeIndexador _ragCascade;

        public ProdutoServico(
            IProdutoRepositorio produtoRepositorio,
            IRagIndexFila ragFila,
            IRagCascadeIndexador ragCascade)
        {
            _produtoRepositorio = produtoRepositorio;
            _ragFila = ragFila;
            _ragCascade = ragCascade;
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

        public async Task<List<Produto>> BuscarPorTermosAsync(IEnumerable<string> termos, Guid? lojaId = null)
        {
            return await _produtoRepositorio.BuscarPorTermosAsync(termos, lojaId);
        }

        public Task<PaginacaoResultado<Produto>> BuscarPorNomePaginadoAsync(
            string nome, PaginacaoParametros paginacao, Guid? lojaId = null) =>
            _produtoRepositorio.BuscarPorNomePaginadoAsync(nome, paginacao, lojaId);

        public async Task AtualizarAsync(Produto produto)
        {
            if (string.IsNullOrWhiteSpace(produto.NomeProduto))
                throw new Exception("O nome do produto é obrigatório.");

            await _produtoRepositorio.AtualizarAsync(produto);
            await _ragCascade.EnfileirarProdutoAtualizadoAsync(produto.Id);
        }

        public async Task RemoverAsync(Guid id)
        {
            await _ragCascade.EnfileirarProdutoRemovidoAsync(id);
            await _produtoRepositorio.RemoverAsync(id);
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

            await _ragCascade.EnfileirarProdutoAtualizadoAsync(id);
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

            await _ragCascade.EnfileirarProdutoRemovidoAsync(id);

            var removido = await _produtoRepositorio.RemoverPorIdAsync(id);
            if (!removido)
                throw new ProdutoOperacaoException("Produto não encontrado.");
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
