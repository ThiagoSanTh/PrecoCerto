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

            return rows
                .Where(r => r.Distancia <= maxDistancia)
                .Select(r => (r.Doc, (double)r.Distancia))
                .ToList();
        }

        public Task<int> ContarAtivosAsync(CancellationToken ct = default) =>
            _context.DocumentosRag.AsNoTracking().CountAsync(d => d.Ativo && d.UsuarioId == null, ct);
    }
}
