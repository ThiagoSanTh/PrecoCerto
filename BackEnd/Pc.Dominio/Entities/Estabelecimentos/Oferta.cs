using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Catalogo;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Estabelecimentos
{
    public class Oferta : BaseEntity
    {
        public Guid ProdutoId { get; set; }
        public Produto? Produto { get; set; }
        public Guid LojaId { get; set; }
        public Loja? Loja { get; set; }
        public decimal Preco { get; set; }
        public decimal? PrecoAnterior { get; set; }
        public bool EmPromocao { get; set; } = false;
        public DateTime? DataInicioPromocao { get; set; }
        public DateTime? DataFimPromocao { get; set; }
        public bool Disponivel { get; set; } = true;
        public int? QuantidadeEstoque { get; set; }
        public DateTime DataAtualizacaoPreco { get; set; } = DateTime.UtcNow;

    }
}
