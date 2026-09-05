using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Pc.Dominio.Entities.Rag;
using Pc.Dominio.Enums;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes.Rag;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;
using Pgvector;
using Xunit;

namespace Pc.Servico.Tests
{
    public class RagHashHelperTests
    {
        [Fact]
        public void Hash_estavel_para_mesmo_conteudo()
        {
            var a = RagHashHelper.Calcular("Produto: Arroz.");
            var b = RagHashHelper.Calcular("Produto: Arroz.");
            Assert.Equal(a, b);
            Assert.Equal(64, a.Length);
        }

        [Fact]
        public void Hash_muda_quando_conteudo_muda()
        {
            var a = RagHashHelper.Calcular("Produto: Arroz.");
            var b = RagHashHelper.Calcular("Produto: Feijão.");
            Assert.NotEqual(a, b);
        }
    }

    public class RagIndexFilaTests
    {
        [Fact]
        public void Coalesce_evita_duplicar_mesmo_evento()
        {
            var fila = new RagIndexFila(NullLogger<RagIndexFila>.Instance);
            var id = Guid.NewGuid();

            fila.Enfileirar(RagDocumentoTipo.Produto, id, RagIndexAcao.Indexar);
            fila.Enfileirar(RagDocumentoTipo.Produto, id, RagIndexAcao.Indexar);
            fila.Enfileirar(RagDocumentoTipo.Produto, id, RagIndexAcao.Indexar);

            Assert.True(fila.Reader.TryRead(out var primeiro));
            Assert.False(fila.Reader.TryRead(out _));
            Assert.Equal(id, primeiro.EntidadeId);

            fila.Liberar(primeiro);
            fila.Enfileirar(RagDocumentoTipo.Produto, id, RagIndexAcao.Indexar);
            Assert.True(fila.Reader.TryRead(out _));
        }
    }

    public class RagIndexadorServicoTests
    {
        private readonly Mock<IDocumentoRagRepositorio> _docs = new();
        private readonly Mock<IRagDocumentBuilder> _builder = new();
        private readonly Mock<IEmbeddingService> _embeddings = new();
        private readonly RagSettings _settings = new()
        {
            Enabled = true,
            ApiKey = "test-key",
            EmbeddingDimension = 1536,
            EmbeddingModel = "text-embedding-3-small",
            BatchSize = 50
        };

        private static float[] VetorFake()
        {
            var v = new float[1536];
            v[0] = 0.1f;
            return v;
        }

        private RagIndexadorServico CriarSut()
        {
            // AppDbContext não é usado nos testes de ProcessarEvento (só Reindexar)
            return new RagIndexadorServico(
                _docs.Object,
                _builder.Object,
                _embeddings.Object,
                db: null!,
                Options.Create(_settings),
                NullLogger<RagIndexadorServico>.Instance);
        }

        [Fact]
        public async Task Produto_novo_gera_embedding_e_documento()
        {
            var id = Guid.NewGuid();
            _builder.Setup(b => b.ConstruirAsync(RagDocumentoTipo.Produto, id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RagDocumentoConstruido
                {
                    Tipo = RagDocumentoTipo.Produto,
                    EntidadeId = id,
                    Titulo = "Arroz",
                    Conteudo = "Produto: Arroz.",
                    DeveIndexar = true
                });
            _docs.Setup(d => d.ObterGlobalAsync(RagDocumentoTipo.Produto, id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DocumentoRag?)null);
            _embeddings.Setup(e => e.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(VetorFake());

            var sut = CriarSut();
            await sut.ProcessarEventoAsync(new RagIndexEvento
            {
                Tipo = RagDocumentoTipo.Produto,
                EntidadeId = id,
                Acao = RagIndexAcao.Indexar
            });

            _embeddings.Verify(e => e.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _docs.Verify(d => d.UpsertAsync(It.IsAny<DocumentoRag>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Produto_sem_alteracao_nao_regenera_embedding()
        {
            var id = Guid.NewGuid();
            var conteudo = "Produto: Arroz.";
            var hash = RagHashHelper.Calcular(conteudo);

            _builder.Setup(b => b.ConstruirAsync(RagDocumentoTipo.Produto, id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RagDocumentoConstruido
                {
                    Tipo = RagDocumentoTipo.Produto,
                    EntidadeId = id,
                    Titulo = "Arroz",
                    Conteudo = conteudo,
                    DeveIndexar = true
                });
            _docs.Setup(d => d.ObterGlobalAsync(RagDocumentoTipo.Produto, id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DocumentoRag
                {
                    Tipo = RagDocumentoTipo.Produto,
                    EntidadeId = id,
                    HashConteudo = hash,
                    Ativo = true,
                    Embedding = new Vector(VetorFake())
                });

            var sut = CriarSut();
            await sut.ProcessarEventoAsync(new RagIndexEvento
            {
                Tipo = RagDocumentoTipo.Produto,
                EntidadeId = id,
                Acao = RagIndexAcao.Indexar
            });

            _embeddings.Verify(e => e.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _docs.Verify(d => d.UpsertAsync(It.IsAny<DocumentoRag>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Produto_alterado_regenera_embedding()
        {
            var id = Guid.NewGuid();
            _builder.Setup(b => b.ConstruirAsync(RagDocumentoTipo.Produto, id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RagDocumentoConstruido
                {
                    Tipo = RagDocumentoTipo.Produto,
                    EntidadeId = id,
                    Titulo = "Arroz Integral",
                    Conteudo = "Produto: Arroz Integral.",
                    DeveIndexar = true
                });
            _docs.Setup(d => d.ObterGlobalAsync(RagDocumentoTipo.Produto, id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DocumentoRag
                {
                    Tipo = RagDocumentoTipo.Produto,
                    EntidadeId = id,
                    HashConteudo = RagHashHelper.Calcular("Produto: Arroz."),
                    Ativo = true,
                    Embedding = new Vector(VetorFake())
                });
            _embeddings.Setup(e => e.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(VetorFake());

            var sut = CriarSut();
            await sut.ProcessarEventoAsync(new RagIndexEvento
            {
                Tipo = RagDocumentoTipo.Produto,
                EntidadeId = id,
                Acao = RagIndexAcao.Indexar
            });

            _embeddings.Verify(e => e.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _docs.Verify(d => d.UpsertAsync(It.IsAny<DocumentoRag>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Produto_excluido_desativa_documento()
        {
            var id = Guid.NewGuid();
            var sut = CriarSut();
            await sut.ProcessarEventoAsync(new RagIndexEvento
            {
                Tipo = RagDocumentoTipo.Produto,
                EntidadeId = id,
                Acao = RagIndexAcao.Remover
            });

            _docs.Verify(d => d.SoftDeleteAsync(RagDocumentoTipo.Produto, id, It.IsAny<CancellationToken>()), Times.Once);
            _embeddings.Verify(e => e.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class RagServicoTests
    {
        [Fact]
        public async Task Busca_aplica_threshold_e_limite()
        {
            var docs = new Mock<IDocumentoRagRepositorio>();
            var embeddings = new Mock<IEmbeddingService>();
            var settings = new RagSettings
            {
                Enabled = true,
                ApiKey = "k",
                EmbeddingDimension = 1536,
                MaxResults = 5,
                SimilarityThreshold = 0.70
            };

            var vetor = new float[1536];
            embeddings.Setup(e => e.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(vetor);

            docs.Setup(d => d.BuscarPorSimilaridadeAsync(
                    It.IsAny<Vector>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<(DocumentoRag, double)>
                {
                    (new DocumentoRag { Tipo = RagDocumentoTipo.Produto, Titulo = "Arroz", Conteudo = "c", EntidadeId = Guid.NewGuid() }, 0.1)
                });

            var sut = new RagServico(docs.Object, embeddings.Object, Options.Create(settings), NullLogger<RagServico>.Instance);
            var results = await sut.BuscarAsync("arroz integral", 3);

            Assert.Single(results);
            Assert.True(results[0].Score >= 0.70);
            docs.Verify(d => d.BuscarPorSimilaridadeAsync(
                It.IsAny<Vector>(), 3, It.Is<double>(x => Math.Abs(x - 0.30) < 0.001), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Busca_sem_config_retorna_vazio()
        {
            var sut = new RagServico(
                new Mock<IDocumentoRagRepositorio>().Object,
                new Mock<IEmbeddingService>().Object,
                Options.Create(new RagSettings { Enabled = true, ApiKey = "" }),
                NullLogger<RagServico>.Instance);

            var results = await sut.BuscarAsync("arroz");
            Assert.Empty(results);
        }
    }
}
