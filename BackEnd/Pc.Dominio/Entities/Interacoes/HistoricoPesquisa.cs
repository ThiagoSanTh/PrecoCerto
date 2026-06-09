using System;
using System.Collections.Generic;
using System.Text;
using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Dominio.Entities.Interacoes
{
    public class HistoricoPesquisa : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public string TermoPesquisa { get; set; } = string.Empty;

        public DateTime DataPesquisa { get; set; } = DateTime.UtcNow;

        // 📊 BI: vincula a pesquisa a um produto/loja quando o cliente
        // abre um resultado, permitindo ao lojista saber o que foi buscado.
        public Guid? ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        public Guid? LojaId { get; set; }
        public Loja? Loja { get; set; }
    }
}
