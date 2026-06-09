using Moq;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Xunit;

namespace Pc.Tests
{
    public class OfertaServicoTests
    {
        private readonly Mock<IOfertaRepositorio> _repo = new();
        private readonly OfertaServico _servico;

        public OfertaServicoTests()
        {
            _servico = new OfertaServico(_repo.Object);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task AdicionarAsync_DeveLancar_QuandoPrecoInvalido(decimal preco)
        {
            var oferta = new Oferta { Preco = preco };
            await Assert.ThrowsAsync<Exception>(() => _servico.AdicionarAsync(oferta));
        }

        [Fact]
        public async Task AdicionarAsync_DeveSalvar_QuandoPrecoValido()
        {
            var oferta = new Oferta { Preco = 9.99m };
            _repo.Setup(r => r.AdicionarAsync(It.IsAny<Oferta>())).ReturnsAsync((Oferta o) => o);

            var resultado = await _servico.AdicionarAsync(oferta);

            Assert.Equal(9.99m, resultado.Preco);
            _repo.Verify(r => r.AdicionarAsync(oferta), Times.Once);
        }

        [Fact]
        public async Task AtualizarAsync_DeveLancar_QuandoPrecoInvalido()
        {
            var oferta = new Oferta { Preco = 0 };
            await Assert.ThrowsAsync<Exception>(() => _servico.AtualizarAsync(oferta));
        }
    }
}
