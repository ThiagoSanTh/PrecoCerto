using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Implementacoes.MotorIA;
using Pc.Servico.Modelos.MotorIA;
using Xunit;

namespace Pc.Servico.Tests.MotorIA
{
    public class LlmRouterServiceTests
    {
        private static IHttpClientFactory CreateFactory(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken _) => responder(req));

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient("GeminiLlm"))
                .Returns(() => new HttpClient(handler.Object)
                {
                    BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/")
                });
            factory.Setup(f => f.CreateClient("NvidiaLlm"))
                .Returns(() => new HttpClient(handler.Object)
                {
                    BaseAddress = new Uri("https://integrate.api.nvidia.com/v1/")
                });

            return factory.Object;
        }

        [Fact]
        public async Task Quando_Gemini_retorna_200_interpreta_json_e_usa_provedor_Gemini()
        {
            var geminiPayload = """
            {
              "candidates": [
                {
                  "content": {
                    "parts": [
                      {
                        "text": "{\"intencao\":\"BuscarProduto\",\"objetivo\":\"Economizar\",\"produtoTermo\":\"arroz\",\"itensBusca\":[\"arroz\"],\"orcamentoMax\":15,\"distanciaMaxKm\":5}"
                      }
                    ]
                  }
                }
              ]
            }
            """;

            var factory = CreateFactory(req =>
            {
                if (req.RequestUri!.Host.Contains("googleapis.com"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(geminiPayload, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

            var sut = new LlmRouterService(
                factory,
                Options.Create(new LlmSettings
                {
                    GeminiApiKey = "gem-key",
                    NvidiaApiKey = "nv-key",
                    TimeoutMs = 3500,
                    CooldownSegundos = 300
                }),
                NullLogger<LlmRouterService>.Instance);
            sut.ResetarCircuitBreaker();

            var res = await sut.InterpretarAsync(new PedidoAnaliseIA { Mensagem = "Quero arroz até 15 reais até 5 km" });

            Assert.NotNull(res);
            Assert.Equal("Gemini", res!.Provedor);
            Assert.Equal("BuscarProduto", res.Intencao);
            Assert.Equal("arroz", res.ProdutoTermo);
            Assert.Equal(15m, res.OrcamentoMax);
            Assert.Equal(5m, res.DistanciaMaxKm);
        }

        [Fact]
        public async Task Quando_Gemini_retorna_429_aciona_circuit_breaker_e_faz_failover_imediato_para_NvidiaNim()
        {
            int geminiCalls = 0;
            int nvidiaCalls = 0;

            var nvidiaPayload = """
            {
              "choices": [
                {
                  "message": {
                    "content": "{\"intencao\":\"MontarCesta\",\"objetivo\":\"Economizar\",\"itensBusca\":[\"arroz\",\"feijao\",\"oleo\"]}"
                  }
                }
              ]
            }
            """;

            var factory = CreateFactory(req =>
            {
                if (req.RequestUri!.Host.Contains("googleapis.com"))
                {
                    Interlocked.Increment(ref geminiCalls);
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                }

                Interlocked.Increment(ref nvidiaCalls);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(nvidiaPayload, Encoding.UTF8, "application/json")
                };
            });

            var sut = new LlmRouterService(
                factory,
                Options.Create(new LlmSettings
                {
                    GeminiApiKey = "gem-key",
                    NvidiaApiKey = "nv-key",
                    TimeoutMs = 3500,
                    CooldownSegundos = 300
                }),
                NullLogger<LlmRouterService>.Instance);
            sut.ResetarCircuitBreaker();

            // 1a chamada: Gemini da 429 -> cai para NvidiaNim
            var res1 = await sut.InterpretarAsync(new PedidoAnaliseIA { Mensagem = "Cesta com arroz, feijão e óleo" });
            Assert.NotNull(res1);
            Assert.Equal("NvidiaNim", res1!.Provedor);
            Assert.Contains("arroz", res1.ItensBusca);
            Assert.Equal(1, geminiCalls);
            Assert.Equal(1, nvidiaCalls);

            // 2a chamada: Gemini está em cooldown no Circuit Breaker -> vai direto para NvidiaNim sem chamar Gemini
            var res2 = await sut.InterpretarAsync(new PedidoAnaliseIA { Mensagem = "Cesta com arroz, feijão e óleo" });
            Assert.NotNull(res2);
            Assert.Equal("NvidiaNim", res2!.Provedor);
            Assert.Equal(1, geminiCalls);
            Assert.Equal(2, nvidiaCalls);
        }

        [Fact]
        public async Task Quando_ambos_falham_ou_sem_chaves_retorna_null_sem_lancar_excecao()
        {
            var factory = CreateFactory(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            var sut = new LlmRouterService(
                factory,
                Options.Create(new LlmSettings { GeminiApiKey = "", NvidiaApiKey = "" }),
                NullLogger<LlmRouterService>.Instance);
            sut.ResetarCircuitBreaker();

            var res = await sut.InterpretarAsync(new PedidoAnaliseIA { Mensagem = "Quero arroz" });
            Assert.Null(res);
        }
    }
}
