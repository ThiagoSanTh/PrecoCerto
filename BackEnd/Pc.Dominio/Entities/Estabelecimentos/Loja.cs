using Pc.Dominio.Entities.Base;
using Pc.Dominio.Entities.Catalogo;
using Pc.Dominio.Entities.Usuarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pc.Dominio.Entities.Estabelecimentos
{
    public class Loja : BaseEntity
    {
        public string NomeFantasia { get; set; } = string.Empty;
        public string? RazaoSocial { get; set; }
        public string? Cnpj { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? Descricao { get; set; }
        public Guid EnderecoId { get; set; }
        public Endereco Endereco { get; set; } = new Endereco();
        public ICollection<Oferta> Ofertas { get; set; } = new List<Oferta>();
        // LojistaId fica opcional para permitir criar/editar loja em etapas.
        public Guid? LojistaId { get; set; }
        public Lojista? Lojista { get; set; }
    }
}
