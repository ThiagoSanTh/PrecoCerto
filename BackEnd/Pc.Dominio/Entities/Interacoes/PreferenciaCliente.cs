using System;
using System.Collections.Generic;
using System.Text;
using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Dominio.Entities.Interacoes
{
    public class PreferenciaCliente : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }
}
