using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Usuarios;
using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Estabelecimentos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Interacoes
{
    public class Favorito : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public Guid? ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        public Guid? LojaId { get; set; }
        public Loja? Loja { get; set; }
    }
}
