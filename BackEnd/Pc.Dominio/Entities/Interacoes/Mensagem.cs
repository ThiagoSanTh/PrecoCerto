using Pc.Dominio.Enums;

namespace Pc.Dominio.Entities.Interacoes
{
    /// <summary>
    /// Mensagem de uma conversa. Não herda BaseEntity: o ciclo de vida
    /// usa EnviadaEm/RecebidaEm em vez de DataCriacao/DataAtualizacao/Ativo.
    /// RemetenteId identifica o usuário (não há campo Remetente).
    /// </summary>
    public class Mensagem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ConversaId { get; set; }
        public Conversa? Conversa { get; set; }

        public Guid RemetenteId { get; set; }
        public PapelUsuario RemetentePapel { get; set; }

        public string Texto { get; set; } = string.Empty;
        public DateTime EnviadaEm { get; set; } = DateTime.UtcNow;
        public DateTime? RecebidaEm { get; set; }
        public bool Lida { get; set; }
    }
}
