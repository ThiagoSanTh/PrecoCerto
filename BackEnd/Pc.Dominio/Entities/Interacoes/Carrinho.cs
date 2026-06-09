using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Usuarios;

namespace Pc.Dominio.Entities.Interacoes
{
    /// <summary>
    /// Carrinho de compras do cliente. Um carrinho ativo por cliente.
    /// </summary>
    public class Carrinho : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public ICollection<ItemCarrinho> Itens { get; set; } = new List<ItemCarrinho>();
    }
}
