using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Dominio.Entities.MotorIA
{
    public class SessaoChat : BaseEntity
    {
        public Guid? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public DateTime UltimaInteracaoEm { get; set; } = DateTime.UtcNow;

        public ICollection<MensagemChat> Mensagens { get; set; } = new List<MensagemChat>();
    }
}
