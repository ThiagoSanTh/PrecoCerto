using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pc.Dominio.Entities.Rag;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    public class RagIndexDlqServico : IRagIndexDlqServico
    {
        private readonly AppDbContext _db;
        private readonly IRagIndexFila _fila;
        private readonly ILogger<RagIndexDlqServico> _logger;

        public RagIndexDlqServico(
            AppDbContext db,
            IRagIndexFila fila,
            ILogger<RagIndexDlqServico> logger)
        {
            _db = db;
            _fila = fila;
            _logger = logger;
        }

        public async Task RegistrarFalhaAsync(
            RagIndexEvento evento,
            string erro,
            CancellationToken cancellationToken = default)
        {
            var item = new RagIndexDlq
            {
                Tipo = evento.Tipo,
                EntidadeId = evento.EntidadeId,
                Acao = (int)evento.Acao,
                Tentativas = evento.Tentativas,
                UltimoErro = Truncar(erro, 2000),
                Status = RagDlqStatus.Pending,
                CriadoEmUtc = DateTime.UtcNow
            };

            _db.RagIndexDlq.Add(item);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "RAG DLQ registrada. Id={Id} Tipo={Tipo} Entidade={Entidade} Tentativas={T}",
                item.Id, item.Tipo, item.EntidadeId, item.Tentativas);
        }

        public async Task<IReadOnlyList<RagDlqItemDto>> ListarPendentesAsync(
            int limite = 50,
            CancellationToken cancellationToken = default)
        {
            limite = Math.Clamp(limite, 1, 200);
            var itens = await _db.RagIndexDlq.AsNoTracking()
                .Where(x => x.Status == RagDlqStatus.Pending)
                .OrderByDescending(x => x.CriadoEmUtc)
                .Take(limite)
                .ToListAsync(cancellationToken);

            return itens.Select(Mapear).ToList();
        }

        public async Task<bool> ReprocessarAsync(Guid dlqId, CancellationToken cancellationToken = default)
        {
            var item = await _db.RagIndexDlq.FirstOrDefaultAsync(x => x.Id == dlqId, cancellationToken);
            if (item is null || item.Status != RagDlqStatus.Pending)
                return false;

            _fila.Enfileirar(item.Tipo, item.EntidadeId, (RagIndexAcao)item.Acao);
            item.Status = RagDlqStatus.Reprocessed;
            item.ReprocessadoEmUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("RAG DLQ reprocessada. Id={Id} Tipo={Tipo} Entidade={E}",
                item.Id, item.Tipo, item.EntidadeId);
            return true;
        }

        public async Task<int> ReprocessarPendentesAsync(int limite = 20, CancellationToken cancellationToken = default)
        {
            limite = Math.Clamp(limite, 1, 500);
            var itens = await _db.RagIndexDlq
                .Where(x => x.Status == RagDlqStatus.Pending)
                .OrderBy(x => x.CriadoEmUtc)
                .Take(limite)
                .ToListAsync(cancellationToken);

            foreach (var item in itens)
            {
                _fila.Enfileirar(item.Tipo, item.EntidadeId, (RagIndexAcao)item.Acao);
                item.Status = RagDlqStatus.Reprocessed;
                item.ReprocessadoEmUtc = DateTime.UtcNow;
            }

            if (itens.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);

            return itens.Count;
        }

        public async Task<bool> DescartarAsync(Guid dlqId, CancellationToken cancellationToken = default)
        {
            var item = await _db.RagIndexDlq.FirstOrDefaultAsync(x => x.Id == dlqId, cancellationToken);
            if (item is null || item.Status != RagDlqStatus.Pending)
                return false;

            item.Status = RagDlqStatus.Discarded;
            item.ReprocessadoEmUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static RagDlqItemDto Mapear(RagIndexDlq x) => new()
        {
            Id = x.Id,
            Tipo = x.Tipo.ToString(),
            EntidadeId = x.EntidadeId,
            Acao = ((RagIndexAcao)x.Acao).ToString(),
            Tentativas = x.Tentativas,
            UltimoErro = x.UltimoErro,
            Status = x.Status.ToString(),
            CriadoEmUtc = x.CriadoEmUtc,
            ReprocessadoEmUtc = x.ReprocessadoEmUtc
        };

        private static string Truncar(string s, int max) =>
            string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);
    }
}
