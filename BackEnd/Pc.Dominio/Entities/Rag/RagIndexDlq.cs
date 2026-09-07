using Pc.Dominio.Enums;

namespace Pc.Dominio.Entities.Rag
{
    public enum RagDlqStatus
    {
        Pending = 0,
        Reprocessed = 1,
        Discarded = 2
    }

    /// <summary>
    /// Dead-letter: eventos de indexação RAG que esgotaram retries.
    /// </summary>
    public class RagIndexDlq
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public RagDocumentoTipo Tipo { get; set; }
        public Guid EntidadeId { get; set; }
        public int Acao { get; set; }
        public int Tentativas { get; set; }
        public string UltimoErro { get; set; } = string.Empty;
        public RagDlqStatus Status { get; set; } = RagDlqStatus.Pending;
        public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReprocessadoEmUtc { get; set; }
    }
}
