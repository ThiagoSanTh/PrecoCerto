using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pc.Dominio.Entities.Rag;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;
using Pgvector;

namespace Pc.Servico.Implementacoes.Rag
{
    public class RagIndexadorServico : IRagIndexadorServico
    {
        private readonly IDocumentoRagRepositorio _docs;
        private readonly IRagDocumentBuilder _builder;
        private readonly IEmbeddingService _embeddings;
        private readonly AppDbContext _db;
        private readonly RagSettings _settings;
        private readonly ILogger<RagIndexadorServico> _logger;

        public RagIndexadorServico(
            IDocumentoRagRepositorio docs,
            IRagDocumentBuilder builder,
            IEmbeddingService embeddings,
            AppDbContext db,
            IOptions<RagSettings> settings,
            ILogger<RagIndexadorServico> logger)
        {
            _docs = docs;
            _builder = builder;
            _embeddings = embeddings;
            _db = db;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task ProcessarEventoAsync(RagIndexEvento evento, CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("RAG desabilitado. Evento ignorado. Tipo={Tipo}", evento.Tipo);
                return;
            }

            if (evento.Acao == RagIndexAcao.Remover)
            {
                await _docs.SoftDeleteAsync(evento.Tipo, evento.EntidadeId, cancellationToken);
                _logger.LogInformation("Documento RAG desativado. Tipo={Tipo} Id={Id}", evento.Tipo, evento.EntidadeId);
                return;
            }

            await IndexarEntidadeAsync(evento.Tipo, evento.EntidadeId, cancellationToken);
        }

        public async Task<RagReindexResultado> ReindexarAsync(
            RagDocumentoTipo? apenasTipo = null,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var resultado = new RagReindexResultado();
            _logger.LogInformation("RAG index iniciado. Tipo={Tipo}", apenasTipo?.ToString() ?? "Todos");

            if (!_settings.EstaConfigurado)
            {
                _logger.LogWarning("RAG não configurado (Enabled/ApiKey). Reindex abortado.");
                sw.Stop();
                resultado.TempoMs = sw.ElapsedMilliseconds;
                return resultado;
            }

            var batch = Math.Clamp(_settings.BatchSize, 10, 200);

            if (apenasTipo is null or RagDocumentoTipo.Produto)
                await ReindexTipoAsync(RagDocumentoTipo.Produto, batch, resultado, cancellationToken);
            if (apenasTipo is null or RagDocumentoTipo.Loja)
                await ReindexTipoAsync(RagDocumentoTipo.Loja, batch, resultado, cancellationToken);
            if (apenasTipo is null or RagDocumentoTipo.Oferta)
                await ReindexTipoAsync(RagDocumentoTipo.Oferta, batch, resultado, cancellationToken);
            if (apenasTipo is null or RagDocumentoTipo.Avaliacao)
                await ReindexTipoAsync(RagDocumentoTipo.Avaliacao, batch, resultado, cancellationToken);

            resultado.TotalDocumentos = await _docs.ContarAtivosAsync(cancellationToken);
            sw.Stop();
            resultado.TempoMs = sw.ElapsedMilliseconds;
            _logger.LogInformation(
                "RAG index concluído. Docs={Docs} Embeddings={Emb} IgnoradosHash={Ign} Erros={Erros} TempoMs={Tempo}",
                resultado.TotalDocumentos,
                resultado.TotalEmbeddingsGerados,
                resultado.IgnoradosPorHash,
                resultado.Erros,
                resultado.TempoMs);
            return resultado;
        }

        private async Task ReindexTipoAsync(
            RagDocumentoTipo tipo,
            int batch,
            RagReindexResultado resultado,
            CancellationToken ct)
        {
            var page = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var ids = await ListarIdsAsync(tipo, page, batch, ct);
                if (ids.Count == 0)
                    break;

                foreach (var id in ids)
                {
                    try
                    {
                        var (criado, embeddingGerado, ignoradoHash) =
                            await IndexarEntidadeAsync(tipo, id, ct);

                        if (criado || embeddingGerado || ignoradoHash)
                        {
                            switch (tipo)
                            {
                                case RagDocumentoTipo.Produto: resultado.ProdutosIndexados++; break;
                                case RagDocumentoTipo.Loja: resultado.LojasIndexadas++; break;
                                case RagDocumentoTipo.Oferta: resultado.OfertasIndexadas++; break;
                                case RagDocumentoTipo.Avaliacao: resultado.AvaliacoesIndexadas++; break;
                            }
                        }

                        if (embeddingGerado) resultado.TotalEmbeddingsGerados++;
                        if (ignoradoHash) resultado.IgnoradosPorHash++;
                    }
                    catch (Exception ex)
                    {
                        resultado.Erros++;
                        _logger.LogError(ex, "Erro ao indexar RAG. Tipo={Tipo} Id={Id}", tipo, id);
                    }
                }

                if (ids.Count < batch)
                    break;
                page++;
            }
        }

        private async Task<List<Guid>> ListarIdsAsync(RagDocumentoTipo tipo, int page, int batch, CancellationToken ct)
        {
            return tipo switch
            {
                RagDocumentoTipo.Produto => await _db.Produtos.AsNoTracking()
                    .Where(p => p.Ativo)
                    .OrderBy(p => p.Id)
                    .Skip(page * batch)
                    .Take(batch)
                    .Select(p => p.Id)
                    .ToListAsync(ct),
                RagDocumentoTipo.Loja => await _db.Lojas.AsNoTracking()
                    .Where(l => l.Ativo)
                    .OrderBy(l => l.Id)
                    .Skip(page * batch)
                    .Take(batch)
                    .Select(l => l.Id)
                    .ToListAsync(ct),
                RagDocumentoTipo.Oferta => await _db.Ofertas.AsNoTracking()
                    .Where(o => o.Ativo)
                    .OrderBy(o => o.Id)
                    .Skip(page * batch)
                    .Take(batch)
                    .Select(o => o.Id)
                    .ToListAsync(ct),
                RagDocumentoTipo.Avaliacao => await _db.Avaliacoes.AsNoTracking()
                    .Where(a => a.Ativo && a.Comentario != null && a.Comentario != "")
                    .OrderBy(a => a.Id)
                    .Skip(page * batch)
                    .Take(batch)
                    .Select(a => a.Id)
                    .ToListAsync(ct),
                _ => new List<Guid>()
            };
        }

        private async Task<(bool criado, bool embeddingGerado, bool ignoradoHash)> IndexarEntidadeAsync(
            RagDocumentoTipo tipo,
            Guid entidadeId,
            CancellationToken ct)
        {
            var construido = await _builder.ConstruirAsync(tipo, entidadeId, ct);
            if (construido is null)
            {
                await _docs.SoftDeleteAsync(tipo, entidadeId, ct);
                return (false, false, false);
            }

            if (!construido.DeveIndexar)
            {
                await _docs.SoftDeleteAsync(tipo, entidadeId, ct);
                _logger.LogInformation("Documento RAG ignorado (sem conteúdo). Tipo={Tipo} Id={Id}", tipo, entidadeId);
                return (false, false, false);
            }

            var hash = RagHashHelper.Calcular(construido.Conteudo);
            var existente = await _docs.ObterGlobalAsync(tipo, entidadeId, ct);

            if (existente is not null
                && existente.Ativo
                && existente.HashConteudo == hash
                && existente.Embedding is not null)
            {
                _logger.LogInformation("Documento ignorado por hash. Tipo={Tipo} Id={Id}", tipo, entidadeId);
                return (false, false, true);
            }

            if (!_settings.EstaConfigurado)
                throw new InvalidOperationException("RAG não configurado para gerar embedding.");

            var vector = await _embeddings.GerarEmbeddingAsync(construido.Conteudo, ct);
            var doc = new DocumentoRag
            {
                Tipo = tipo,
                EntidadeId = entidadeId,
                Titulo = construido.Titulo.Length > 300 ? construido.Titulo[..300] : construido.Titulo,
                Conteudo = construido.Conteudo,
                HashConteudo = hash,
                Metadata = construido.Metadata,
                Embedding = new Vector(vector),
                UsuarioId = null,
                Ativo = true,
                DataCriacao = existente?.DataCriacao ?? DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };

            await _docs.UpsertAsync(doc, ct);
            var criado = existente is null;
            _logger.LogInformation(
                "Documento indexado. Tipo={Tipo} Id={Id} Criado={Criado}",
                tipo, entidadeId, criado);
            return (criado, true, false);
        }
    }
}
