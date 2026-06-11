using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Dominio.Entities.Interacoes
{
    public class Conversa : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Usuario? Cliente { get; set; }

        public Guid LojaId { get; set; }
        public Loja? Loja { get; set; }

        public DateTime? UltimaMensagemEm { get; set; }

        public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    }
}
