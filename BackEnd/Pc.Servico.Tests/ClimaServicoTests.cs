using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Pc.Servico.Excecoes;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos;
using Xunit;

namespace Pc.Servico.Tests
{
    public class ClimaWmoTests
    {
        [Theory]
        [InlineData(0, "Céu limpo", "clear")]
        [InlineData(61, "Chuva", "rain")]
        [InlineData(95, "Tempestade", "thunder")]
        [InlineData(1234, "Condição indefinida", "cloudy")]
        public void Interpretar_codigos_conhecidos(int codigo, string descricao, string icone)
        {
            var resultado = ClimaWmo.Interpretar(codigo);
            Assert.Equal(descricao, resultado.Descricao);
            Assert.Equal(icone, resultado.Icone);
        }

        [Fact]
        public void UfBrasil_mapeia_estado()
        {
            Assert.Equal("RJ", ClimaWmo.UfBrasil("Rio de Janeiro"));
            Assert.Equal("SP", ClimaWmo.UfBrasil("São Paulo"));
            Assert.Equal("RJ", ClimaWmo.UfBrasil("RJ"));
        }
    }

    public class ClimaServicoTests
    {
        private readonly Mock<IClimaProvedor> _provedor = new();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly ClimaSettings _settings = new()
        {
            CacheMinutos = 15,
            CacheStaleHoras = 6,
            PrecisaoCoordenadas = 2
        };

        public ClimaServicoTests()
        {
            _provedor.SetupGet(p => p.Nome).Returns("fake");
        }

        [Fact]
        public async Task Resposta_valida_normaliza_coordenadas()
        {
            _provedor
                .Setup(p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClimaOk(-22.93m, -42.51m));

            var sut = CriarSut();
            var resultado = await sut.ObterPorCoordenadasAsync(-22.9341m, -42.5099m);

            Assert.Equal(-22.93m, resultado.Localidade.Latitude);
            Assert.Equal(-42.51m, resultado.Localidade.Longitude);
            Assert.Equal(27.4m, resultado.Atual.Temperatura);
            Assert.False(resultado.DeCache);
            _provedor.Verify(
                p => p.ConsultarAsync(-22.93m, -42.51m, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Cache_hit_nao_chama_provedor_de_novo()
        {
            _provedor
                .Setup(p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClimaOk(-22.93m, -42.51m));

            var sut = CriarSut();
            await sut.ObterPorCoordenadasAsync(-22.93m, -42.51m);
            var segundo = await sut.ObterPorCoordenadasAsync(-22.931m, -42.512m);

            Assert.True(segundo.DeCache);
            _provedor.Verify(
                p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Cache_miss_chama_provedor()
        {
            _provedor
                .Setup(p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClimaOk(-22.93m, -42.51m));

            var sut = CriarSut();
            await sut.ObterPorCoordenadasAsync(-22.93m, -42.51m);
            await sut.ObterPorCoordenadasAsync(-23.00m, -43.20m);

            _provedor.Verify(
                p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task Erro_do_provedor_sem_cache_propaga()
        {
            _provedor
                .Setup(p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ClimaIndisponivelException("caiu"));

            var sut = CriarSut();
            await Assert.ThrowsAsync<ClimaIndisponivelException>(() =>
                sut.ObterPorCoordenadasAsync(-22.93m, -42.51m));
        }

        [Fact]
        public async Task Erro_do_provedor_com_cache_stale_retorna_fallback()
        {
            var chave = ClimaServico.ChaveCache(-22.93m, -42.51m);
            _cache.Set(chave, new ClimaCacheEntry
            {
                Dados = ClimaOk(-22.93m, -42.51m),
                FrescoAteUtc = DateTime.UtcNow.AddMinutes(-1)
            }, TimeSpan.FromHours(6));

            _provedor
                .Setup(p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ClimaIndisponivelException("caiu"));

            var sut = CriarSut();
            var resultado = await sut.ObterPorCoordenadasAsync(-22.93m, -42.51m);

            Assert.True(resultado.DeCache);
            Assert.True(resultado.Desatualizado);
            Assert.Equal(27.4m, resultado.Atual.Temperatura);
        }

        [Fact]
        public async Task Localizacao_invalida_lanca()
        {
            var sut = CriarSut();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                sut.ObterPorCoordenadasAsync(200m, 0m));
            _provedor.Verify(
                p => p.ConsultarAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void Normalizar_agrupa_gps_proximo()
        {
            var a = ClimaServico.Normalizar(-22.9311m, -42.5088m, 2);
            var b = ClimaServico.Normalizar(-22.9349m, -42.5112m, 2);
            Assert.Equal(a, b);
        }

        private ClimaServico CriarSut() =>
            new(_provedor.Object, _cache, Options.Create(_settings), NullLogger<ClimaServico>.Instance);

        private static ClimaResposta ClimaOk(decimal lat, decimal lng) => new()
        {
            Localidade = new ClimaLocalidade { Latitude = lat, Longitude = lng, Cidade = "Saquarema", Estado = "RJ", Pais = "BR" },
            Atual = new ClimaAtual
            {
                Temperatura = 27.4m,
                SensacaoTermica = 29.1m,
                Umidade = 78,
                Descricao = "Ensolarado",
                Icone = "clear",
                CodigoClima = 1
            },
            ObtidoEm = DateTime.UtcNow,
            Provedor = "fake"
        };
    }

    public class OpenMeteoClimaProvedorTests
    {
        [Fact]
        public async Task Resposta_valida_normaliza_campos()
        {
            var handler = new ScriptHttpHandler
            {
                Responder = req =>
                {
                    var url = req.RequestUri?.ToString() ?? "";
                    if (url.Contains("open-meteo.com"))
                        return Json(HttpStatusCode.OK, ForecastJson());
                    return Json(HttpStatusCode.OK, NominatimJson());
                }
            };

            var sut = CriarProvedor(handler);
            var clima = await sut.ConsultarAsync(-22.93m, -42.51m);

            Assert.Equal(20.4m, clima.Atual.Temperatura);
            Assert.Equal(22.2m, clima.Atual.SensacaoTermica);
            Assert.Equal(97, clima.Atual.Umidade);
            Assert.Equal("Garoa", clima.Atual.Descricao);
            Assert.Equal("drizzle", clima.Atual.Icone);
            Assert.Equal("Saquarema", clima.Localidade.Cidade);
            Assert.Equal("RJ", clima.Localidade.Estado);
            Assert.Equal("BR", clima.Localidade.Pais);
            Assert.NotEmpty(clima.Horaria);
            Assert.NotEmpty(clima.Diaria);
            Assert.Equal(18.5m, clima.Diaria[0].TemperaturaMinima);
            Assert.Equal(20.7m, clima.Diaria[0].TemperaturaMaxima);
        }

        [Fact]
        public async Task Provider_http_erro_lanca_indisponivel()
        {
            var handler = new ScriptHttpHandler
            {
                Responder = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            };
            var sut = CriarProvedor(handler);
            await Assert.ThrowsAsync<ClimaIndisponivelException>(() => sut.ConsultarAsync(-22.93m, -42.51m));
        }

        [Fact]
        public async Task Json_invalido_lanca_indisponivel()
        {
            var handler = new ScriptHttpHandler
            {
                Responder = req =>
                {
                    var url = req.RequestUri?.ToString() ?? "";
                    if (url.Contains("open-meteo.com"))
                        return Texto(HttpStatusCode.OK, "nao-e-json");
                    return Json(HttpStatusCode.OK, NominatimJson());
                }
            };
            var sut = CriarProvedor(handler);
            await Assert.ThrowsAsync<ClimaIndisponivelException>(() => sut.ConsultarAsync(-22.93m, -42.51m));
        }

        [Fact]
        public async Task Sem_current_lanca_indisponivel()
        {
            var handler = new ScriptHttpHandler
            {
                Responder = req =>
                {
                    var url = req.RequestUri?.ToString() ?? "";
                    if (url.Contains("open-meteo.com"))
                        return Json(HttpStatusCode.OK, """{"latitude":-22.9,"longitude":-42.5}""");
                    return Json(HttpStatusCode.OK, NominatimJson());
                }
            };
            var sut = CriarProvedor(handler);
            await Assert.ThrowsAsync<ClimaIndisponivelException>(() => sut.ConsultarAsync(-22.93m, -42.51m));
        }

        [Fact]
        public async Task Nominatim_falha_ainda_retorna_clima()
        {
            var handler = new ScriptHttpHandler
            {
                Responder = req =>
                {
                    var url = req.RequestUri?.ToString() ?? "";
                    if (url.Contains("open-meteo.com"))
                        return Json(HttpStatusCode.OK, ForecastJson());
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
            };

            var sut = CriarProvedor(handler);
            var clima = await sut.ConsultarAsync(-22.93m, -42.51m);
            Assert.Equal(20.4m, clima.Atual.Temperatura);
            Assert.Null(clima.Localidade.Cidade);
        }

        private static OpenMeteoClimaProvedor CriarProvedor(HttpMessageHandler handler)
        {
            var http = new HttpClient(handler);
            return new OpenMeteoClimaProvedor(
                http,
                Options.Create(new ClimaSettings()),
                NullLogger<OpenMeteoClimaProvedor>.Instance);
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
            new(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

        private static HttpResponseMessage Texto(HttpStatusCode status, string texto) =>
            new(status)
            {
                Content = new StringContent(texto, Encoding.UTF8, "text/plain")
            };

        private static string ForecastJson() =>
            """
            {
              "latitude": -22.93,
              "longitude": -42.51,
              "current": {
                "time": "2026-08-27T15:00",
                "temperature_2m": 20.4,
                "apparent_temperature": 22.2,
                "relative_humidity_2m": 97,
                "precipitation": 0.2,
                "weather_code": 53,
                "wind_speed_10m": 13.7
              },
              "hourly": {
                "time": ["2026-08-27T15:00", "2026-08-27T16:00"],
                "temperature_2m": [20.4, 20.1],
                "apparent_temperature": [22.2, 21.8],
                "precipitation": [0.2, 0],
                "precipitation_probability": [80, 40],
                "weather_code": [53, 3]
              },
              "daily": {
                "time": ["2026-08-27", "2026-08-28"],
                "weather_code": [80, 3],
                "temperature_2m_max": [20.7, 26.6],
                "temperature_2m_min": [18.5, 17.6],
                "apparent_temperature_max": [22.6, 30.4],
                "precipitation_sum": [8, 0],
                "precipitation_probability_max": [100, 0]
              }
            }
            """;

        private static string NominatimJson() =>
            """
            {
              "address": {
                "city": "Saquarema",
                "state": "Rio de Janeiro",
                "country_code": "br",
                "ISO3166-2-lvl4": "BR-RJ"
              }
            }
            """;
    }

    public sealed class ScriptHttpHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(Responder(request));
    }
}
