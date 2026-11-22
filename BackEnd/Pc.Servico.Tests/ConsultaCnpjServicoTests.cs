using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Controllers;
using Xunit;

namespace Pc.Servico.Tests
{
    public class ConsultaCnpjServicoTests
    {
        private const string Cnpj = "19131243000197";

        [Fact]
        public async Task OpenCnpj_mapeia_endereco_email_e_telefone()
        {
            var chamadasBrasil = 0;
            var sut = Criar(req =>
            {
                if (req.RequestUri!.Host.Contains("opencnpj", StringComparison.OrdinalIgnoreCase))
                    return Json(HttpStatusCode.OK, OpenCnpjComEndereco);

                chamadasBrasil++;
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

            var resultado = await sut.ConsultarAsync(Cnpj);

            Assert.NotNull(resultado);
            Assert.Equal(Cnpj, resultado.Cnpj);
            Assert.Equal("OPEN KNOWLEDGE BRASIL", resultado.RazaoSocial);
            Assert.Equal("REDE PELO CONHECIMENTO LIVRE", resultado.NomeFantasia);
            Assert.Equal("Ativa", resultado.SituacaoCadastral);
            Assert.Equal("contato@okbr.org", resultado.Email);
            Assert.Equal("1123851939", resultado.Telefone);
            var endereco = Assert.IsType<CnpjEndereco>(resultado.Endereco);
            Assert.Equal("PAULISTA", endereco.Logradouro);
            Assert.Equal("37", endereco.Numero);
            Assert.Null(endereco.Complemento);
            Assert.Equal("BELA VISTA", endereco.Bairro);
            Assert.Equal("SAO PAULO", endereco.Cidade);
            Assert.Equal("SP", endereco.Uf);
            Assert.Equal("01311902", endereco.Cep);
            Assert.Equal(0, chamadasBrasil);
        }

        [Fact]
        public async Task BrasilApi_mapeia_endereco_email_e_telefone_quando_opencnpj_falha()
        {
            var sut = Criar(req =>
            {
                if (req.RequestUri!.Host.Contains("opencnpj", StringComparison.OrdinalIgnoreCase))
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

                return Json(HttpStatusCode.OK, BrasilApiComEndereco);
            });

            var resultado = await sut.ConsultarAsync(Cnpj);

            Assert.NotNull(resultado);
            Assert.Equal(Cnpj, resultado.Cnpj);
            Assert.Equal("OPEN KNOWLEDGE BRASIL", resultado.RazaoSocial);
            Assert.Null(resultado.NomeFantasia);
            Assert.Equal("ATIVA", resultado.SituacaoCadastral);
            Assert.Null(resultado.Email);
            Assert.Equal("1123851939", resultado.Telefone);
            var endereco = Assert.IsType<CnpjEndereco>(resultado.Endereco);
            Assert.Equal("PAULISTA 37", endereco.Logradouro);
            Assert.Equal("37", endereco.Numero);
            Assert.Equal("ANDAR 4", endereco.Complemento);
            Assert.Equal("BELA VISTA", endereco.Bairro);
            Assert.Equal("SAO PAULO", endereco.Cidade);
            Assert.Equal("SP", endereco.Uf);
            Assert.Equal("01311902", endereco.Cep);
        }

        [Fact]
        public async Task OpenCnpj_sem_endereco_devolve_endereco_nulo()
        {
            var sut = Criar(_ => Json(HttpStatusCode.OK, OpenCnpjSemEndereco));

            var resultado = await sut.ConsultarAsync(Cnpj);

            Assert.NotNull(resultado);
            Assert.Equal("EMPRESA SEM ENDERECO LTDA", resultado.RazaoSocial);
            Assert.Equal("SEM ENDERECO", resultado.NomeFantasia);
            Assert.Equal("Ativa", resultado.SituacaoCadastral);
            Assert.Null(resultado.Endereco);
            Assert.Null(resultado.Email);
            Assert.Null(resultado.Telefone);
        }

        private static ConsultaCnpjServico Criar(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new ScriptHttpHandler { Responder = responder };
            var http = new HttpClient(handler);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var settings = Options.Create(new ConsultaCnpjSettings());
            return new ConsultaCnpjServico(http, cache, settings);
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

        // Campos reais da OpenCNPJ (https://api.opencnpj.org/schema): logradouro, numero,
        // complemento, bairro, municipio, uf, cep, email e telefones[{ddd, numero, is_fax}].
        private const string OpenCnpjComEndereco = """
            {
              "cnpj": "19131243000197",
              "razao_social": "OPEN KNOWLEDGE BRASIL",
              "nome_fantasia": "  REDE PELO CONHECIMENTO LIVRE  ",
              "situacao_cadastral": "Ativa",
              "tipo_logradouro": "AVENIDA",
              "logradouro": "PAULISTA",
              "numero": "37",
              "complemento": "",
              "bairro": "BELA VISTA",
              "cep": "01311-902",
              "uf": "SP",
              "municipio": "SAO PAULO",
              "email": " contato@okbr.org ",
              "telefones": [
                { "ddd": "11", "numero": "99990000", "is_fax": true },
                { "ddd": "11", "numero": "2385-1939", "is_fax": false }
              ]
            }
            """;

        private const string OpenCnpjSemEndereco = """
            {
              "cnpj": "19131243000197",
              "razao_social": "EMPRESA SEM ENDERECO LTDA",
              "nome_fantasia": "SEM ENDERECO",
              "situacao_cadastral": "Ativa",
              "tipo_logradouro": "",
              "logradouro": "",
              "numero": " ",
              "complemento": "",
              "bairro": "",
              "cep": "-----",
              "uf": "",
              "municipio": "",
              "email": "",
              "telefones": [
                { "ddd": "11", "numero": "33334444", "is_fax": true }
              ]
            }
            """;

        // Campos reais da BrasilAPI CNPJ v1: logradouro, numero, complemento, bairro,
        // municipio, uf, cep, email e ddd_telefone_1.
        private const string BrasilApiComEndereco = """
            {
              "cnpj": "19131243000197",
              "razao_social": "OPEN KNOWLEDGE BRASIL",
              "nome_fantasia": "",
              "descricao_situacao_cadastral": "ATIVA",
              "descricao_tipo_de_logradouro": "AVENIDA",
              "logradouro": "PAULISTA 37",
              "numero": "37",
              "complemento": "ANDAR 4",
              "bairro": "BELA VISTA",
              "cep": "01311-902",
              "uf": "SP",
              "municipio": "SAO PAULO",
              "email": null,
              "ddd_telefone_1": "(11) 2385-1939",
              "ddd_telefone_2": "1133334444"
            }
            """;
    }

    public class ConsultasControllerContratoTests
    {
        private const string Cnpj = "19131243000197";

        [Fact]
        public async Task Resposta_traz_situacao_e_endereco_aninhado()
        {
            var servico = new Mock<IConsultaCnpjServico>();
            servico
                .Setup(s => s.ConsultarAsync(Cnpj, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CnpjConsultaResultado
                {
                    Cnpj = Cnpj,
                    RazaoSocial = "OPEN KNOWLEDGE BRASIL",
                    NomeFantasia = "REDE",
                    SituacaoCadastral = "ATIVA",
                    Endereco = new CnpjEndereco
                    {
                        Logradouro = "PAULISTA",
                        Numero = "37",
                        Complemento = null,
                        Bairro = "BELA VISTA",
                        Cidade = "SAO PAULO",
                        Uf = "SP",
                        Cep = "01311902"
                    },
                    Email = "contato@okbr.org",
                    Telefone = "1123851939"
                });

            var json = await Consultar(servico.Object);

            Assert.Equal(Cnpj, json.GetProperty("cnpj").GetString());
            Assert.Equal("OPEN KNOWLEDGE BRASIL", json.GetProperty("razaoSocial").GetString());
            Assert.Equal("REDE", json.GetProperty("nomeFantasia").GetString());
            Assert.Equal("ATIVA", json.GetProperty("situacao").GetString());
            Assert.Equal("ATIVA", json.GetProperty("situacaoCadastral").GetString());
            Assert.True(json.GetProperty("ativo").GetBoolean());
            Assert.Equal("contato@okbr.org", json.GetProperty("email").GetString());
            Assert.Equal("1123851939", json.GetProperty("telefone").GetString());
            var endereco = json.GetProperty("endereco");
            Assert.Equal(JsonValueKind.Object, endereco.ValueKind);
            Assert.Equal("PAULISTA", endereco.GetProperty("logradouro").GetString());
            Assert.Equal("37", endereco.GetProperty("numero").GetString());
            Assert.Equal(JsonValueKind.Null, endereco.GetProperty("complemento").ValueKind);
            Assert.Equal("BELA VISTA", endereco.GetProperty("bairro").GetString());
            Assert.Equal("SAO PAULO", endereco.GetProperty("cidade").GetString());
            Assert.Equal("SP", endereco.GetProperty("uf").GetString());
            Assert.Equal("01311902", endereco.GetProperty("cep").GetString());
        }

        [Fact]
        public async Task Resposta_com_endereco_ausente_serializa_nulos()
        {
            var servico = new Mock<IConsultaCnpjServico>();
            servico
                .Setup(s => s.ConsultarAsync(Cnpj, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CnpjConsultaResultado
                {
                    Cnpj = Cnpj,
                    RazaoSocial = "EMPRESA SEM ENDERECO LTDA",
                    NomeFantasia = null,
                    SituacaoCadastral = "ATIVA"
                });

            var json = await Consultar(servico.Object);

            Assert.Equal(JsonValueKind.Null, json.GetProperty("nomeFantasia").ValueKind);
            Assert.Equal("ATIVA", json.GetProperty("situacao").GetString());
            Assert.Equal("ATIVA", json.GetProperty("situacaoCadastral").GetString());
            Assert.True(json.GetProperty("ativo").GetBoolean());
            Assert.Equal(JsonValueKind.Null, json.GetProperty("endereco").ValueKind);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("email").ValueKind);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("telefone").ValueKind);
        }

        private static async Task<JsonElement> Consultar(IConsultaCnpjServico servico)
        {
            var controller = new ConsultasController(servico);
            var resultado = await controller.ConsultarCnpj(Cnpj, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(resultado);
            var corpo = JsonSerializer.Serialize(ok.Value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return JsonDocument.Parse(corpo).RootElement;
        }
    }
}
