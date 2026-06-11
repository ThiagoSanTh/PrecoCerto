using Pc.Dominio.Entities.Base;
using Pc.Dominio.Enums;

namespace Pc.Dominio.Entities.Interacoes
{
    public class Mensagem : BaseEntity
    {
        public Guid ConversaId { get; set; }
        public Conversa? Conversa { get; set; }

        public Guid RemetenteId { get; set; }
        public PapelUsuario RemetentePapel { get; set; }

        public string Texto { get; set; } = string.Empty;
        public DateTime EnviadaEm { get; set; } = DateTime.UtcNow;
        public bool Lida { get; set; }
    }
}
