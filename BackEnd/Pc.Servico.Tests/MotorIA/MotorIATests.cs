using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Implementacoes.IA;
using Pc.Servico.Implementacoes.MotorIA;
using Pc.Servico.Implementacoes.MotorIA.Regras;
using Pc.Servico.Interfaces;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;
using Pc.Servico.Modelos.Rag;
using Xunit;

namespace Pc.Servico.Tests.MotorIA
{
    public class MotorIAInterpretacaoTests
    {
        private readonly IInterpretadorIA _sut = new InterpretadorIA(
            new ClassificadorIntencaoIA(),
            new ExtratorEntidadesIA());

        [Fact]
        public void Normalizacao_remove_acentos()
        {
            Assert.Equal("arroz barato", TextoNormalizador.Normalizar("  Árroz Barato "));
        }

        [Theory]
        [InlineData("Quero arroz barato", IntencaoIA.BuscarProduto)]
        [InlineData("Quero arroz perto de mim", IntencaoIA.BuscarProduto)]
        [InlineData("Preciso de arroz agora", IntencaoIA.BuscarProduto)]
        [InlineData("tem promoção de sabonete?", IntencaoIA.BuscarPromocao)]
        [InlineData("Qual loja vale mais a pena?", IntencaoIA.CompararPrecos)]
        [InlineData("Quem é Neymar?", IntencaoIA.ForaDoDominio)]
        [InlineData("Não quero sair de casa, tem entrega?", IntencaoIA.ConsultarEntrega)]
        [InlineData("Vocês fazem entrega de feijão?", IntencaoIA.ConsultarEntrega)]
        [InlineData("ola", IntencaoIA.Saudacao)]
        [InlineData("Bom dia", IntencaoIA.Saudacao)]
        [InlineData("Oi!", IntencaoIA.Saudacao)]
        [InlineData("aonde eu posso achar uma câmera vendendo?", IntencaoIA.BuscarProduto)]
        [InlineData("coca", IntencaoIA.BuscarProduto)]
        [InlineData("estou procurando por uma coca, aonde eu posso encontrar o mais barato", IntencaoIA.BuscarProdutoMaisBarato)]
        public void Classifica_intencoes(string msg, IntencaoIA esperada)
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = msg });
            Assert.Equal(esperada, ctx.Intencao);
        }

        [Fact]
        public void Sinonimos_preco_baixo_definem_objetivo_economizar()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quero fazer uma compra gastando pouco" });
            Assert.Equal(NivelPreferenciaIA.Alta, ctx.PreferenciaPreco);
            Assert.Equal(ObjetivoIA.Economizar, ctx.Objetivo);
        }

        [Fact]
        public void Extrai_produto_e_urgencia()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Preciso de arroz agora" });
            Assert.True(ctx.UrgenciaAlta);
            Assert.NotNull(ctx.ProdutoTermo);
            Assert.Contains("arroz", ctx.ProdutoTermo!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Extrai_orcamento_e_distancia()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quero arroz até 30 reais até 2 km" });
            Assert.Equal(30m, ctx.OrcamentoMax);
            Assert.Equal(2m, ctx.DistanciaMaxKm);
        }

        [Fact]
        public void Identifica_necessidade_entrega()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Não quero sair de casa, tem delivery?" });
            Assert.True(ctx.NecessitaEntrega);
            Assert.Equal(IntencaoIA.ConsultarEntrega, ctx.Intencao);
        }

        [Fact]
        public void Loja_mais_proxima_nao_vira_termo_de_loja()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Qual loja mais próxima?" });
            Assert.Equal(IntencaoIA.BuscarLojaMaisProxima, ctx.Intencao);
            Assert.True(string.IsNullOrWhiteSpace(ctx.LojaTermo));
        }

        [Fact]
        public void Extrai_produto_de_promocao()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Tem promoção de leite?" });
            Assert.Equal(IntencaoIA.BuscarPromocao, ctx.Intencao);
            Assert.NotNull(ctx.ProdutoTermo);
            Assert.Contains("leite", ctx.ProdutoTermo!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Extrai_camera_da_frase_completa_nao_so_o_inicio()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA
            {
                Mensagem = "aonde eu posso achar uma câmera vendendo?"
            });
            Assert.Equal(IntencaoIA.BuscarProduto, ctx.Intencao);
            Assert.NotNull(ctx.ProdutoTermo);
            Assert.Contains("camera", TextoNormalizador.Normalizar(ctx.ProdutoTermo!), StringComparison.Ordinal);
            Assert.DoesNotContain("aonde", TextoNormalizador.Normalizar(ctx.ProdutoTermo!), StringComparison.Ordinal);
            Assert.DoesNotContain("posso", TextoNormalizador.Normalizar(ctx.ProdutoTermo!), StringComparison.Ordinal);
        }

        [Fact]
        public void Saudacao_nao_extrai_produto()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Bom dia" });
            Assert.Equal(IntencaoIA.Saudacao, ctx.Intencao);
            Assert.True(string.IsNullOrWhiteSpace(ctx.ProdutoTermo));
        }

        [Theory]
        [InlineData("estou procurando por uma coca, aonde eu posso encontrar o mais barato", "coca")]
        [InlineData("estou procurando por uma camera, aonde eu posso encontrar o mais barato", "camera")]
        [InlineData("estou procurando por uma brinco, aonde eu posso encontrar o mais barato", "brinco")]
        [InlineData("to procurando um arroz mais barato", "arroz")]
        [InlineData("preciso duma coca barata", "coca")]
        public void Frases_elaboradas_extraem_so_o_produto(string msg, string produtoEsperado)
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = msg });
            Assert.False(string.IsNullOrWhiteSpace(ctx.ProdutoTermo));
            var termo = TextoNormalizador.Normalizar(ctx.ProdutoTermo!);
            Assert.Contains(produtoEsperado, termo, StringComparison.Ordinal);
            Assert.DoesNotContain("estou", termo, StringComparison.Ordinal);
            Assert.DoesNotContain("procurando", termo, StringComparison.Ordinal);
            Assert.DoesNotContain("aonde", termo, StringComparison.Ordinal);
            Assert.Contains(ctx.TermosBuscaProduto, t =>
                TextoNormalizador.Normalizar(t).Contains(produtoEsperado, StringComparison.Ordinal));
        }

        [Fact]
        public void Coca_expande_sinonimos_de_busca()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "coca" });
            Assert.Equal(IntencaoIA.BuscarProduto, ctx.Intencao);
            Assert.Contains(ctx.TermosBuscaProduto, t =>
                TextoNormalizador.Normalizar(t).Contains("refrigerante", StringComparison.Ordinal)
                || TextoNormalizador.Normalizar(t).Contains("cola", StringComparison.Ordinal));
        }

        [Fact]
        public void Extrai_feijao_em_consulta_entrega()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Vocês fazem entrega de feijão?" });
            Assert.Equal(IntencaoIA.ConsultarEntrega, ctx.Intencao);
            Assert.NotNull(ctx.ProdutoTermo);
            Assert.Contains("feijao", TextoNormalizador.Normalizar(ctx.ProdutoTermo!), StringComparison.Ordinal);
        }
    }

    public class MotorIARegrasPontuacaoTests
    {
        private static IMotorRegrasIA CriarRegras() => new MotorRegrasIA(new IRegraIA[]
        {
            new RegraEconomizar(),
            new RegraUrgencia(),
            new RegraChuva(),
            new RegraEntrega(),
            new RegraProximidade(),
            new RegraPromocao(),
            new RegraQualidade()
        });

        [Fact]
        public void Regra_economizar_aumenta_peso_preco()
        {
            var ctx = new ContextoIA
            {
                Objetivo = ObjetivoIA.Economizar,
                PreferenciaPreco = NivelPreferenciaIA.Alta,
                Pesos = PesosIA.Padrao(),
                Latitude = -22.9,
                Longitude = -42.5
            };
            CriarRegras().Aplicar(ctx);
            Assert.Contains("USUARIO_QUER_ECONOMIZAR", ctx.RegrasAplicadas);
            Assert.Equal(40, ctx.Pesos.Preco);
        }

        [Fact]
        public void Regra_chuva_altera_pesos()
        {
            var ctx = new ContextoIA
            {
                ClimaDisponivel = true,
                Chuva = true,
                Pesos = PesosIA.Padrao(),
                Latitude = -22.9,
                Longitude = -42.5
            };
            CriarRegras().Aplicar(ctx);
            Assert.Contains("CHUVA", ctx.RegrasAplicadas);
            Assert.Equal(25, ctx.Pesos.Entrega);
        }

        [Fact]
        public void Sem_localizacao_zera_peso_distancia()
        {
            var ctx = new ContextoIA { PreferenciaDistancia = NivelPreferenciaIA.Alta, Pesos = PesosIA.Padrao() };
            CriarRegras().Aplicar(ctx);
            Assert.Equal(0, ctx.Pesos.Distancia);
            Assert.Contains("sem_localizacao", ctx.FallbacksUsados);
        }

        [Fact]
        public void Multiplas_regras_combinam()
        {
            var ctx = new ContextoIA
            {
                Objetivo = ObjetivoIA.Economizar,
                UrgenciaAlta = true,
                PreferenciaPreco = NivelPreferenciaIA.Alta,
                Latitude = 1,
                Longitude = 1,
                Pesos = PesosIA.Padrao()
            };
            CriarRegras().Aplicar(ctx);
            Assert.True(ctx.RegrasAplicadas.Count >= 2);
        }

        [Fact]
        public void Pontuacao_prefere_menor_preco_quando_peso_alto()
        {
            var ctx = new ContextoIA
            {
                Pesos = new PesosIA { Preco = 100, Distancia = 0, Promocao = 0, Avaliacao = 0, Entrega = 0, Disponibilidade = 0, Conveniencia = 0 },
                Candidatos =
                {
                    new CandidatoIA { Titulo = "A", Preco = 50, Disponivel = true },
                    new CandidatoIA { Titulo = "B", Preco = 10, Disponivel = true }
                }
            };
            new MotorPontuacaoIA().Pontuar(ctx);
            Assert.Equal("B", ctx.Candidatos[0].Titulo);
            Assert.True(ctx.Candidatos[0].Score > ctx.Candidatos[1].Score);
        }

        [Fact]
        public void Ranking_e_recomendacao()
        {
            var ctx = new ContextoIA
            {
                Intencao = IntencaoIA.BuscarProduto,
                ProdutoTermo = "arroz",
                Pesos = PesosIA.Padrao(),
                Latitude = -22,
                Longitude = -42,
                Candidatos =
                {
                    new CandidatoIA { Titulo = "Longe", Preco = 20, DistanciaKm = 10, Disponivel = true },
                    new CandidatoIA { Titulo = "Perto", Preco = 22, DistanciaKm = 1, Disponivel = true, EmPromocao = true }
                }
            };
            var decisao = new MotorRecomendacaoIA(new MotorPontuacaoIA()).Recomendar(ctx);
            Assert.NotNull(decisao.Melhor);
            Assert.True(decisao.Confianca > 0);
            Assert.NotEmpty(decisao.Ranking);
        }

        [Fact]
        public void Sem_resultados_gera_resposta_fallback()
        {
            var ctx = new ContextoIA { Intencao = IntencaoIA.BuscarProduto, ProdutoTermo = "xyz" };
            new MotorRecomendacaoIA(new MotorPontuacaoIA()).Recomendar(ctx);
            var texto = new GeradorRespostaIA().Gerar(ctx);
            Assert.Contains("Não encontrei", texto);
        }

        [Fact]
        public void Fora_dominio_resposta_padrao()
        {
            var ctx = new ContextoIA { Intencao = IntencaoIA.ForaDoDominio };
            var texto = new GeradorRespostaIA().Gerar(ctx);
            Assert.Contains("Preço Certo", texto);
        }

        [Fact]
        public void Saudacao_resposta_amigavel()
        {
            var ctx = new ContextoIA { Intencao = IntencaoIA.Saudacao };
            var texto = new GeradorRespostaIA().Gerar(ctx);
            Assert.Contains("Olá", texto);
            Assert.DoesNotContain("Não entendi", texto);
        }
    }

    public class RagConhecimentoFallbackTests
    {
        [Fact]
        public async Task Sem_rag_configurado_retorna_vazio()
        {
            var rag = new Mock<IRagServico>();
            var sut = new RagConhecimentoIA(
                rag.Object,
                Options.Create(new RagSettings { Enabled = true, ApiKey = "" }),
                NullLogger<RagConhecimentoIA>.Instance);

            var hits = await sut.BuscarAuxiliarAsync("arroz");
            Assert.Empty(hits);
            rag.Verify(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
