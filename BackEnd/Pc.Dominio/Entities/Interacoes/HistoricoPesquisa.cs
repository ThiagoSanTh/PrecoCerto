using System;
using System.Collections.Generic;
using System.Text;
using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Dominio.Entities.Interacoes
{
    public class HistoricoPesquisa : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public string TermoPesquisa { get; set; } = string.Empty;

        public DateTime DataPesquisa { get; set; } = DateTime.UtcNow;
    }
}
