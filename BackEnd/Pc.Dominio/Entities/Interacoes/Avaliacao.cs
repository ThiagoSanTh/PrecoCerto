using System;
using System.Collections.Generic;
using System.Text;
using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Dominio.Entities.Interacoes
{
    public class Avaliacao : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public Guid LojaId { get; set; }
        public Loja? Loja { get; set; }

        public int Nota { get; set; }

        public string? Comentario { get; set; }

        public DateTime DataAvaliacao { get; set; } = DateTime.UtcNow;
    }
}
