using Pc.Dominio.Entities.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Catalogo
{
    public class Categoria : BaseEntity
    {
        public string NomeCategoria { get; set; } =string.Empty;
        public string? Descricao { get; set; }
        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}
