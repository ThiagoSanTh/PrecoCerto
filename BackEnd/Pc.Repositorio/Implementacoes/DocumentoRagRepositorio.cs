using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Entities.Rag;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Repositorio.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Pc.Repositorio.Implementacoes
{
    public class DocumentoRagRepositorio : IDocumentoRagRepositorio
    {
        private readonly AppDbContext _context;

        public DocumentoRagRepositorio(AppDbContext context)
        {
            _context = context;
        }

        public Task<DocumentoRag?> ObterGlobalAsync(
            RagDocumentoTipo tipo,
            Guid entidadeId,
            CancellationToken ct = default)
        {
            return _context.DocumentosRag
                .FirstOrDefaultAsync(
                    d => d.Tipo == tipo && d.EntidadeId == entidadeId && d.UsuarioId == null,
                    ct);
        }

        public async Task UpsertAsync(DocumentoRag documento, CancellationToken ct = default)
        {
            var existente = await ObterGlobalAsync(documento.Tipo, documento.EntidadeId, ct);
            if (existente is null)
            {
                _context.DocumentosRag.Add(documento);
            }
            else
            {
                existente.Titulo = documento.Titulo;
                existente.Conteudo = documento.Conteudo;
                existente.HashConteudo = documento.HashConteudo;
                existente.Metadata = documento.Metadata;
                existente.Embedding = documento.Embedding;
                existente.Ativo = true;
                existente.DataAtualizacao = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task SoftDeleteAsync(RagDocumentoTipo tipo, Guid entidadeId, CancellationToken ct = default)
        {
            var existente = await ObterGlobalAsync(tipo, entidadeId, ct);
            if (existente is null)
                return;

            existente.Ativo = false;
            existente.DataAtualizacao = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<(DocumentoRag Doc, double Distancia)>> BuscarPorSimilaridadeAsync(
            Vector embedding,
            int limite,
            double maxDistancia,
            CancellationToken ct = default)
        {
            var totalComEmbedding = await _context.DocumentosRag.AsNoTracking()
                .CountAsync(d => d.Ativo && d.UsuarioId == null && d.Embedding != null, ct);

            var rows = await _context.DocumentosRag
                .AsNoTracking()
                .Where(d => d.Ativo && d.UsuarioId == null && d.Embedding != null)
                .OrderBy(d => d.Embedding!.CosineDistance(embedding))
                .Take(limite)
                .Select(d => new
                {
                    Doc = d,
                    Distancia = d.Embedding!.CosineDistance(embedding)
                })
                .ToListAsync(ct);

            var filtrados = rows
                .Where(r => r.Distancia <= maxDistancia)
                .Select(r => (r.Doc, (double)r.Distancia))
                .ToList();

            // #region agent log
            try
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "6c7c29",
                    runId = "post-fix",
                    hypothesisId = "C,D",
                    location = "DocumentoRagRepositorio.cs:BuscarPorSimilaridadeAsync",
                    message = "rag-vector-filter",
                    data = new
                    {
                        totalComEmbedding,
                        rowsAntesFiltro = rows.Count,
                        rowsAposFiltro = filtrados.Count,
                        maxDistancia,
                        indiceVazio = totalComEmbedding == 0,
                        topDists = rows.Select(r => Math.Round((double)r.Distancia, 4)).Take(5).ToList(),
                        topTitulos = rows.Select(r => r.Doc.Titulo).Take(5).ToList()
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                System.IO.File.AppendAllText(@"d:\Dev\PrecoCerto\debug-6c7c29.log", payload + "\n");
            }
            catch { /* debug */ }
            // #endregion

            return filtrados;
        }

        public Task<int> ContarAtivosAsync(CancellationToken ct = default) =>
            _context.DocumentosRag.AsNoTracking().CountAsync(d => d.Ativo && d.UsuarioId == null, ct);
    }
}
