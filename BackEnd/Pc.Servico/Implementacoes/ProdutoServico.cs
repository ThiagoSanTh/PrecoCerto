using Pc.Dominio.Entities.Catalogo;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;

namespace Pc.Servico.Implementacoes
{
    public class ProdutoServico : IProdutoServico
    {
        private readonly IProdutoRepositorio _produtoRepositorio;

        public ProdutoServico(IProdutoRepositorio produtoRepositorio)
        {
            _produtoRepositorio = produtoRepositorio;
        }

        public async Task<Produto> AdicionarAsync(Produto produto)
        {
            if (string.IsNullOrWhiteSpace(produto.NomeProduto))
                throw new Exception("O nome do produto é obrigatório.");

            return await _produtoRepositorio.AdicionarAsync(produto);
        }

        public async Task<Produto?> ObterPorIdAsync(Guid id)
        {
            return await _produtoRepositorio.ObterPorIdAsync(id);
        }

        public async Task<List<Produto>> ListarProdutosAsync(Guid? lojaId = null)
        {
            return await _produtoRepositorio.ListarPorLojaAsync(lojaId);
        }

        public async Task<List<Produto>> BuscarPorNomeAsync(string nome, Guid? lojaId = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return new List<Produto>();

            return await _produtoRepositorio.BuscarPorNomeAsync(nome, lojaId);
        }

        public async Task AtualizarAsync(Produto produto)
        {
            if (string.IsNullOrWhiteSpace(produto.NomeProduto))
                throw new Exception("O nome do produto é obrigatório.");

            await _produtoRepositorio.AtualizarAsync(produto);
        }

        public async Task RemoverAsync(Guid id)
        {
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
