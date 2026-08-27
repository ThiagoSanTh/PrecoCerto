using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pc.Servico.Excecoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;
using Pc.WebApi.Controllers;
using Xunit;

namespace Pc.Servico.Tests
{
    public class WeatherControllerTests
    {
        private readonly Mock<IClimaServico> _servico = new();

        [Fact]
        public async Task Sem_coordenadas_retorna_400()
        {
            var resultado = await Criar().Obter(null, null, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(resultado);
            _servico.Verify(
                s => s.ObterPorCoordenadasAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Coordenadas_invalidas_retorna_400()
        {
            var resultado = await Criar().Obter(200m, 0m, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task Sucesso_retorna_200()
        {
            _servico
                .Setup(s => s.ObterPorCoordenadasAsync(-22.93m, -42.51m, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClimaResposta
                {
                    Atual = new ClimaAtual { Temperatura = 27.4m, Descricao = "Ensolarado" }
                });

            var resultado = await Criar().Obter(-22.93m, -42.51m, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(resultado);
            var corpo = Assert.IsType<ClimaResposta>(ok.Value);
            Assert.Equal(27.4m, corpo.Atual.Temperatura);
        }

        [Fact]
        public async Task Provider_indisponivel_retorna_503()
        {
            _servico
                .Setup(s => s.ObterPorCoordenadasAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ClimaIndisponivelException());

            var resultado = await Criar().Obter(-22.93m, -42.51m, CancellationToken.None);
            var obj = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(503, obj.StatusCode);
        }

        private WeatherController Criar() =>
            new(_servico.Object, NullLogger<WeatherController>.Instance);
    }
}
