using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.MotorIA;
using Pc.Dominio.Enums.MotorIA;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes.MotorIA;
using Pc.Servico.Implementacoes.MotorIA.Regras;
using Pc.Servico.Interfaces;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;
using Xunit;

namespace Pc.Servico.Tests.MotorIA
{
    public class MotorRecomendacaoFiltroEstritoTests
    {
        [Fact]
        public void OrcamentoMax_descarta_preco_nulo_e_acima_do_limite()
        {
            var ctx = new ContextoIA
            {
                Intencao = IntencaoIA.BuscarProduto,
                ProdutoTermo = "arroz",
                OrcamentoMax = 25m,
                Pesos = PesosIA.Padrao(),
                Candidatos =
                {
                    new CandidatoIA { Titulo = "Sem Preco", Preco = null, Disponivel = true },
                    new CandidatoIA { Titulo = "Caro", Preco = 25.01m, Disponivel = true },
                    new CandidatoIA { Titulo = "No Limite", Preco = 25m, Disponivel = true }
                }
            };

            var decisao = new MotorRecomendacaoIA(new MotorPontuacaoIA()).Recomendar(ctx);

            Assert.Single(decisao.Ranking);
            Assert.Equal("No Limite", decisao.Ranking[0].Titulo);
        }

        [Fact]
        public void DistanciaMaxKm_descarta_distancia_nula_e_acima_do_limite()
        {
            var ctx = new ContextoIA
            {
                Intencao = IntencaoIA.BuscarProduto,
                ProdutoTermo = "arroz",
                DistanciaMaxKm = 5m,
                Latitude = -22.9,
                Longitude = -42.5,
                Pesos = PesosIA.Padrao(),
                Candidatos =
                {
                    new CandidatoIA { Titulo = "Sem Distancia", Preco = 20m, DistanciaKm = null, Disponivel = true },
                    new CandidatoIA { Titulo = "Longe", Preco = 20m, DistanciaKm = 5.1m, Disponivel = true },
                    new CandidatoIA { Titulo = "Perto", Preco = 20m, DistanciaKm = 5m, Disponivel = true }
                }
            };

            var decisao = new MotorRecomendacaoIA(new MotorPontuacaoIA()).Recomendar(ctx);

            Assert.Single(decisao.Ranking);
            Assert.Equal("Perto", decisao.Ranking[0].Titulo);
        }
    }

    public class ValidadorRespostaIATests
    {
        private readonly ValidadorRespostaIA _sut = new();

        private static ContextoIA CriarContextoComRanking() => new()
        {
            Intencao = IntencaoIA.BuscarProduto,
            ProdutoTermo = "arroz",
            Decisao = new DecisaoIA
            {
                Ranking = new List<CandidatoIA>
                {
                    new()
                    {
                        Titulo = "Arroz Branco 5kg",
                        NomeProduto = "Arroz Branco 5kg",
                        NomeLoja = "Mercado Economia",
                        Preco = 19.90m,
                        Disponivel = true
                    }
                }
            }
        };

        [Fact]
        public void Aprova_resposta_com_preco_e_loja_presentes_no_ranking()
        {
            var ctx = CriarContextoComRanking();
            var valido = _sut.Validar("Encontrei Arroz Branco 5kg por R$ 19,90 no Mercado Economia.", ctx);

            Assert.True(valido);
        }

        [Fact]
        public void Rejeita_resposta_com_preco_inventado_ausente_do_ranking()
        {
            var ctx = CriarContextoComRanking();
            var valido = _sut.Validar("Encontrei Arroz Branco 5kg por R$ 99,99 no Mercado Economia.", ctx);

            Assert.False(valido);
        }
    }

    public class MotorIAEvolucaoOrquestracaoTests
    {
        private readonly Mock<IProdutoServico> _produtos = new();
        private readonly Mock<IOfertaServico> _ofertas = new();
        private readonly Mock<ILojaServico> _lojas = new();
        private readonly Mock<IAvaliacaoServico> _avaliacoes = new();
        private readonly Mock<IClimaServico> _clima = new();
        private readonly Mock<IRagConhecimentoIA> _rag = new();
        private readonly Mock<ILlmRouterService> _llm = new();
        private readonly Mock<ISessaoChatRepositorio> _sessaoRepo = new();
        private readonly Mock<IValidadorRespostaIA> _validador = new();

        public MotorIAEvolucaoOrquestracaoTests()
        {
            _avaliacoes.Setup(a => a.ObterResumosPorLojasAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, (double Media, int Quantidade)>());
            _rag.Setup(r => r.BuscarAuxiliarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RagHitIA>());
        }

        private Pc.Servico.Implementacoes.MotorIA.MotorIA CriarMotor(
            ISessaoChatRepositorio? sessaoRepo = null,
            IValidadorRespostaIA? validador = null)
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
                _llm.Object,
                sessaoRepo,
                validador);
        }

        [Fact]
        public async Task AnalisarAsync_hidrata_historico_via_sessao_e_salva_turno()
        {
            var sessaoId = Guid.NewGuid();
            var lojaId = Guid.NewGuid();
            var loja = new Loja { Id = lojaId, NomeFantasia = "Mercado Economia", Ativo = true };
            var prod = new Produto
            {
                Id = Guid.NewGuid(),
                LojaId = lojaId,
                Loja = loja,
                NomeProduto = "Arroz Tipo 1",
                Preco = 21.50m,
                Ativo = true
            };

            _sessaoRepo.Setup(r => r.ObterOuCriarAsync(sessaoId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SessaoChat { Id = sessaoId });
            _sessaoRepo.Setup(r => r.ListarUltimasMensagensAsync(sessaoId, 8, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MensagemChat>
                {
                    new() { SessaoChatId = sessaoId, Papel = "user", Texto = "Quero arroz" },
                    new() { SessaoChatId = sessaoId, Papel = "assistant", Texto = "Tenho Arroz Tipo 1 por R$ 21,50." }
                });
            _produtos.Setup(p => p.BuscarPorTermosAsync(It.IsAny<IEnumerable<string>>(), null))
                .ReturnsAsync(new List<Produto> { prod });
            _ofertas.Setup(o => o.ListarDisponiveisPorProdutosAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<Oferta>());
            _llm.Setup(l => l.InterpretarAsync(It.IsAny<PedidoAnaliseIA>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LlmInterpretacaoResultado?)null);
            _llm.Setup(l => l.GerarRespostaAsync(It.IsAny<ContextoIA>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("Arroz Tipo 1 por R$ 21,50 no Mercado Economia.", "Gemini"));
            _validador.Setup(v => v.Validar(It.IsAny<string>(), It.IsAny<ContextoIA>()))
                .Returns(true);

            var sut = CriarMotor(_sessaoRepo.Object, _validador.Object);
            var res = await sut.AnalisarAsync(new PedidoAnaliseIA
            {
                SessaoId = sessaoId,
                Mensagem = "Tem arroz mais barato?"
            });

            Assert.True(res.Sucesso);
            Assert.Equal(sessaoId, res.SessaoId);
            _llm.Verify(l => l.InterpretarAsync(
                It.Is<PedidoAnaliseIA>(p => p.Historico.Count == 2 && p.Historico[0].Texto == "Quero arroz"),
                It.IsAny<CancellationToken>()), Times.Once);
            _sessaoRepo.Verify(r => r.AdicionarTurnoAsync(
                sessaoId,
                "Tem arroz mais barato?",
                res.Resposta,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AnalisarAsync_faz_fallback_deterministico_quando_validador_rejeita_resposta_llm()
        {
            var lojaId = Guid.NewGuid();
            var loja = new Loja { Id = lojaId, NomeFantasia = "Mercado Economia", Ativo = true };
            var prod = new Produto
            {
                Id = Guid.NewGuid(),
                LojaId = lojaId,
                Loja = loja,
                NomeProduto = "Arroz Tipo 1",
                Preco = 21.50m,
                Ativo = true
            };

            _produtos.Setup(p => p.BuscarPorTermosAsync(It.IsAny<IEnumerable<string>>(), null))
                .ReturnsAsync(new List<Produto> { prod });
            _ofertas.Setup(o => o.ListarDisponiveisPorProdutosAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<Oferta>());
            _llm.Setup(l => l.InterpretarAsync(It.IsAny<PedidoAnaliseIA>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LlmInterpretacaoResultado?)null);
            _llm.Setup(l => l.GerarRespostaAsync(It.IsAny<ContextoIA>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("Arroz inventado por R$ 99,99 na Loja Fake.", "Gemini"));
            _validador.Setup(v => v.Validar(It.IsAny<string>(), It.IsAny<ContextoIA>()))
                .Returns(false);

            var sut = CriarMotor(validador: _validador.Object);
            var res = await sut.AnalisarAsync(new PedidoAnaliseIA
            {
                Mensagem = "Quero arroz"
            });

            Assert.True(res.Sucesso);
            Assert.Contains("resposta_llm_rejeitada", res.Fallbacks);
            Assert.DoesNotContain("99,99", res.Resposta);
            Assert.Contains("Arroz Tipo 1", res.Resposta);
        }
    }
}
