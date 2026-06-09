using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Estabelecimentos;

namespace Pc.Dominio.Entities.Interacoes
{
    /// <summary>
    /// Item de um carrinho: produto, quantidade e preço unitário (snapshot).
    /// </summary>
    public class ItemCarrinho : BaseEntity
    {
        public Guid CarrinhoId { get; set; }
        public Carrinho? Carrinho { get; set; }

        public Guid ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        // Oferta opcional usada como referência de preço promocional.
        public Guid? OfertaId { get; set; }
        public Oferta? Oferta { get; set; }

        public int Quantidade { get; set; } = 1;
        public decimal PrecoUnitario { get; set; }
    }
}
