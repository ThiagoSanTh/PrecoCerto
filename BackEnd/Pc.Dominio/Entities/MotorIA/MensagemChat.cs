using Pc.Dominio.Entities.Base;

namespace Pc.Dominio.Entities.MotorIA
{
    public class MensagemChat : BaseEntity
    {
        public Guid SessaoChatId { get; set; }
        public SessaoChat? SessaoChat { get; set; }

        public string Papel { get; set; } = "user";
        public string Texto { get; set; } = string.Empty;
    }
}
