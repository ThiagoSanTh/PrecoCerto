using Moq;
using Pc.Dominio.Entities.Catalogo;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Excecoes;
using Pc.Servico.Implementacoes;
using Xunit;

namespace Pc.Tests
{
    public class ProdutoServicoTests
    {
        private readonly Mock<IProdutoRepositorio> _repo = new();
        private readonly ProdutoServico _servico;

        public ProdutoServicoTests()
        {
            _servico = new ProdutoServico(_repo.Object);
        }

        [Fact]
        public async Task AdicionarAsync_DeveLancar_QuandoNomeVazio()
        {
            var produto = new Produto { NomeProduto = "" };
            await Assert.ThrowsAsync<Exception>(() => _servico.AdicionarAsync(produto));
        }

        [Fact]
        public async Task AdicionarAsync_DeveSalvar_QuandoValido()
        {
            var produto = new Produto { NomeProduto = "Arroz" };
            _repo.Setup(r => r.AdicionarAsync(It.IsAny<Produto>())).ReturnsAsync((Produto p) => p);

            var resultado = await _servico.AdicionarAsync(produto);

            Assert.Equal("Arroz", resultado.NomeProduto);
            _repo.Verify(r => r.AdicionarAsync(produto), Times.Once);
        }

        [Fact]
        public async Task AtualizarPorLojaAsync_DeveLancar_QuandoProdutoNaoExiste()
        {
            _repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Produto?)null);

            await Assert.ThrowsAsync<ProdutoOperacaoException>(
                () => _servico.AtualizarPorLojaAsync(Guid.NewGuid(), new Produto { NomeProduto = "X" }, Guid.NewGuid()));
        }

        [Fact]
        public async Task AtualizarPorLojaAsync_DeveNegar_QuandoNaoEhDono()
        {
            var lojaDona = Guid.NewGuid();
            var outraLoja = Guid.NewGuid();
            var existente = new Produto { NomeProduto = "Arroz", LojaId = lojaDona };
            _repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync(existente);

            var ex = await Assert.ThrowsAsync<ProdutoOperacaoException>(
                () => _servico.AtualizarPorLojaAsync(Guid.NewGuid(), new Produto { NomeProduto = "Arroz" }, outraLoja));
            Assert.True(ex.AcessoNegado);
        }

        [Fact]
        public async Task AtualizarPorLojaAsync_DeveAtualizar_QuandoDono()
        {
            var lojaDona = Guid.NewGuid();
            var existente = new Produto { NomeProduto = "Arroz", LojaId = lojaDona };
            _repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync(existente);
            _repo.Setup(r => r.AtualizarCamposAsync(It.IsAny<Produto>())).ReturnsAsync(true);

            var dados = new Produto { NomeProduto = "Arroz Integral", Preco = 10 };
            await _servico.AtualizarPorLojaAsync(Guid.NewGuid(), dados, lojaDona);

            Assert.Equal("Arroz Integral", existente.NomeProduto);
            _repo.Verify(r => r.AtualizarCamposAsync(existente), Times.Once);
        }
    }
}
