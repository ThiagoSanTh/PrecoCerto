using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
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

        [Theory]
        [InlineData("Quero comprar arroz até 15 reais até 5 km", IntencaoIA.BuscarProduto, "arroz")]
        [InlineData("Não quero arroz caro, quero o mais em conta em Saquarema", IntencaoIA.BuscarProduto, "arroz")]
        [InlineData("Onde encontro açúcar cristal?", IntencaoIA.BuscarProduto, "acucar")]
        public void Extrai_produto_correto_com_numeros_negacao_e_cidade(string msg, IntencaoIA intencaoEsperada, string termoEsperado)
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = msg });
            Assert.Equal(intencaoEsperada, ctx.Intencao);
            Assert.NotNull(ctx.ProdutoTermo);
            Assert.Contains(termoEsperado, TextoNormalizador.Normalizar(ctx.ProdutoTermo!), StringComparison.Ordinal);
        }

        [Fact]
        public void Classifica_produtos_da_loja_e_limpa_verbo_vende()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quais produtos a loja Bink vende?" });
            Assert.Equal(IntencaoIA.ProdutosDaLoja, ctx.Intencao);
            Assert.Equal("bink", ctx.LojaTermo);
        }

        [Fact]
        public void Classifica_copa_do_mundo_como_fora_do_dominio()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quem ganhou a copa do mundo?" });
            Assert.Equal(IntencaoIA.ForaDoDominio, ctx.Intencao);
        }

        [Fact]
        public void Extrai_multiplos_itens_na_cesta_basica()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Quero montar uma cesta básica com arroz, feijão e óleo" });
            Assert.Equal(IntencaoIA.MontarCesta, ctx.Intencao);
            Assert.Contains("arroz", ctx.ItensBusca);
            Assert.Contains("feijao", ctx.ItensBusca);
            Assert.Contains("oleo", ctx.ItensBusca);
        }

        [Fact]
        public void Classifica_ocasiao_churrasco_e_expande_itens()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA { Mensagem = "Recomenda algo para churrasco" });
            Assert.Equal(IntencaoIA.RecomendarPorOcasiao, ctx.Intencao);
            Assert.NotEmpty(ctx.ItensBusca);
            Assert.Contains("carne", ctx.ItensBusca);
        }

        [Fact]
        public void Classifica_analise_concorrencia_lojista_quando_em_modo_loja()
        {
            var ctx = _sut.Interpretar(new PedidoAnaliseIA
            {
                Mensagem = "Quais produtos meus estão mais caros que a concorrência?",
                LojaIdUsuario = Guid.NewGuid(),
                ModoOperacional = "loja"
            });
            Assert.Equal(IntencaoIA.AnalisarConcorrenciaLojista, ctx.Intencao);
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

    public class MotorIAOrquestracaoV3Tests
    {
        private readonly Mock<IProdutoServico> _produtos = new();
        private readonly Mock<IOfertaServico> _ofertas = new();
        private readonly Mock<ILojaServico> _lojas = new();
        private readonly Mock<IAvaliacaoServico> _avaliacoes = new();
        private readonly Mock<IClimaServico> _clima = new();
        private readonly Mock<IRagConhecimentoIA> _rag = new();
        private readonly Mock<ILlmRouterService> _llm = new();

        public MotorIAOrquestracaoV3Tests()
        {
            _avaliacoes.Setup(a => a.ObterResumosPorLojasAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, (double Media, int Quantidade)>());
            _rag.Setup(r => r.BuscarAuxiliarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RagHitIA>());
        }

        private Pc.Servico.Implementacoes.MotorIA.MotorIA CriarMotor()
        {
            var interpretador = new InterpretadorIA(new ClassificadorIntencaoIA(), new ExtratorEntidadesIA());
            var regras = new MotorRegrasIA(new IRegraIA[]
            {
                new RegraEconomizar(),
                new RegraUrgencia(),
                new RegraChuva(),
                new RegraEntrega(),
                new RegraProximidade(),
                new RegraPromocao(),
                new RegraQualidade()
            });
            var recomendacao = new MotorRecomendacaoIA(new MotorPontuacaoIA());
            var gerador = new GeradorRespostaIA();

            return new Pc.Servico.Implementacoes.MotorIA.MotorIA(
                interpretador,
                regras,
                recomendacao,
                gerador,
                _rag.Object,
                _produtos.Object,
                _ofertas.Object,
                _lojas.Object,
                _avaliacoes.Object,
                _clima.Object,
                NullLogger<Pc.Servico.Implementacoes.MotorIA.MotorIA>.Instance,
                _llm.Object);
        }

        [Fact]
        public async Task AnalisarAsync_MontarCesta_agrupa_ofertas_por_loja_e_soma_total()
        {
            var lojaAId = Guid.NewGuid();
            var lojaBId = Guid.NewGuid();
            var lojaA = new Loja { Id = lojaAId, NomeFantasia = "Mercado Economia", Ativo = true };
            var lojaB = new Loja { Id = lojaBId, NomeFantasia = "Super Caro", Ativo = true };

            var prodArrozA = new Produto { Id = Guid.NewGuid(), LojaId = lojaAId, Loja = lojaA, NomeProduto = "Arroz 5kg", Preco = 20m, Ativo = true };
            var prodFeijaoA = new Produto { Id = Guid.NewGuid(), LojaId = lojaAId, Loja = lojaA, NomeProduto = "Feijão 1kg", Preco = 8m, Ativo = true };
            var prodArrozB = new Produto { Id = Guid.NewGuid(), LojaId = lojaBId, Loja = lojaB, NomeProduto = "Arroz 5kg", Preco = 28m, Ativo = true };
            var prodFeijaoB = new Produto { Id = Guid.NewGuid(), LojaId = lojaBId, Loja = lojaB, NomeProduto = "Feijão 1kg", Preco = 12m, Ativo = true };

            _produtos.Setup(p => p.BuscarPorNomeAsync(It.Is<string>(s => s.Contains("arroz")), null))
                .ReturnsAsync(new List<Produto> { prodArrozA, prodArrozB });
            _produtos.Setup(p => p.BuscarPorNomeAsync(It.Is<string>(s => s.Contains("feijao") || s.Contains("feijão")), null))
                .ReturnsAsync(new List<Produto> { prodFeijaoA, prodFeijaoB });
            _ofertas.Setup(o => o.ListarDisponiveisPorProdutosAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<Oferta>());

            var sut = CriarMotor();
            var res = await sut.AnalisarAsync(new PedidoAnaliseIA
            {
                Mensagem = "Quero montar uma cesta com arroz e feijão"
            });

            Assert.True(res.Sucesso);
            Assert.Equal(IntencaoIA.MontarCesta, res.Intencao);
            Assert.NotEmpty(res.Resultados);
            var melhor = res.Resultados[0];
            Assert.Equal(lojaAId, melhor.LojaId);
            Assert.Equal(28m, melhor.Preco);
            Assert.Contains(melhor.Motivos, m => m.Contains("2/2 itens", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task AnalisarAsync_ProdutosDaLoja_lista_catalogo_da_loja_especifica()
        {
            var lojaId = Guid.NewGuid();
            var loja = new Loja { Id = lojaId, NomeFantasia = "Bink", Ativo = true };
            var prod1 = new Produto { Id = Guid.NewGuid(), LojaId = lojaId, Loja = loja, NomeProduto = "Café Torrado", Preco = 14.90m, Ativo = true };
            var prod2 = new Produto { Id = Guid.NewGuid(), LojaId = lojaId, Loja = loja, NomeProduto = "Açúcar Cristal", Preco = 5.50m, Ativo = true };

            _lojas.Setup(l => l.BuscarPorNomeAsync("bink"))
                .ReturnsAsync(new List<Loja> { loja });
            _produtos.Setup(p => p.ListarProdutosAsync(lojaId))
                .ReturnsAsync(new List<Produto> { prod1, prod2 });
            _ofertas.Setup(o => o.ListarDisponiveisPorProdutosAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<Oferta>());

            var sut = CriarMotor();
            var res = await sut.AnalisarAsync(new PedidoAnaliseIA
            {
                Mensagem = "Quais produtos a loja Bink vende?"
            });

            Assert.True(res.Sucesso);
            Assert.Equal(IntencaoIA.ProdutosDaLoja, res.Intencao);
            Assert.Equal(2, res.Resultados.Count);
            Assert.All(res.Resultados, r => Assert.Equal(lojaId, r.LojaId));
            Assert.Contains(res.Resultados, r => r.ProdutoId == prod1.Id);
            Assert.Contains("Bink", res.Resposta);
        }

        [Fact]
        public async Task AnalisarAsync_AnalisarConcorrenciaLojista_compara_precos_da_loja_do_usuario_com_concorrentes()
        {
            var minhaLojaId = Guid.NewGuid();
            var concorrenteId = Guid.NewGuid();
            var minhaLoja = new Loja { Id = minhaLojaId, NomeFantasia = "Minha Loja", Ativo = true };
            var concorrente = new Loja { Id = concorrenteId, NomeFantasia = "Concorrente", Ativo = true };

            var meuProd = new Produto { Id = Guid.NewGuid(), LojaId = minhaLojaId, Loja = minhaLoja, NomeProduto = "Arroz Branco", Preco = 25m, Ativo = true };
            var concProd = new Produto { Id = Guid.NewGuid(), LojaId = concorrenteId, Loja = concorrente, NomeProduto = "Arroz Branco", Preco = 20m, Ativo = true };

            _produtos.Setup(p => p.ListarProdutosAsync(minhaLojaId))
                .ReturnsAsync(new List<Produto> { meuProd });
            _produtos.Setup(p => p.BuscarPorNomeAsync("Arroz Branco", null))
                .ReturnsAsync(new List<Produto> { meuProd, concProd });
            _ofertas.Setup(o => o.ListarDisponiveisPorProdutosAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<Oferta>());

            var sut = CriarMotor();
            var res = await sut.AnalisarAsync(new PedidoAnaliseIA
            {
                Mensagem = "Quais produtos meus estão mais caros que a concorrência?",
                LojaIdUsuario = minhaLojaId,
                ModoOperacional = "loja"
            });

            Assert.True(res.Sucesso);
            Assert.Equal(IntencaoIA.AnalisarConcorrenciaLojista, res.Intencao);
            Assert.NotEmpty(res.Resultados);
            Assert.Contains(res.Resultados[0].Motivos, m => m.Contains("mais caro", StringComparison.OrdinalIgnoreCase) || m.Contains("concorrência", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task AnalisarAsync_retorna_ProdutoId_e_LojaId_nos_resultados()
        {
            var lojaId = Guid.NewGuid();
            var prodId = Guid.NewGuid();
            var loja = new Loja { Id = lojaId, NomeFantasia = "Bink", Ativo = true };
            var prod = new Produto { Id = prodId, LojaId = lojaId, Loja = loja, NomeProduto = "Arroz Tipo 1", Preco = 19.90m, Ativo = true };

            _produtos.Setup(p => p.BuscarPorTermosAsync(It.IsAny<IEnumerable<string>>(), null))
                .ReturnsAsync(new List<Produto> { prod });
            _ofertas.Setup(o => o.ListarDisponiveisPorProdutosAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<Oferta>());

            var sut = CriarMotor();
            var res = await sut.AnalisarAsync(new PedidoAnaliseIA { Mensagem = "Quero arroz" });

            Assert.NotEmpty(res.Resultados);
            Assert.Equal(prodId, res.Resultados[0].ProdutoId);
            Assert.Equal(lojaId, res.Resultados[0].LojaId);
        }
    }
}
