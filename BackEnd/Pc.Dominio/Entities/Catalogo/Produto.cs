using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Dominio.Enums;

namespace Pc.Dominio.Entities.Catalogo
{
    public class Produto : BaseEntity
    {
        public string NomeProduto { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? Marca { get; set; }
        public string? CodigoBarras { get; set; }
        public decimal Preco { get; set; }
        public string? ImagemUrl { get; set; }

        /// <summary>Categoria do produto (enum substitui a entidade Categoria).</summary>
        public CategoriaProduto Categoria { get; set; } = CategoriaProduto.Outros;

        /// <summary>Loja que cadastrou o produto (null = legado / catálogo sem dono).</summary>
        public Guid? LojaId { get; set; }
        public Loja? Loja { get; set; }
    }
}