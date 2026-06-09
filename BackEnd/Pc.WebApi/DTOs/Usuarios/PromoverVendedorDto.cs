using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Usuarios
{
    /// <summary>Dados para promover um cliente a vendedor de uma loja.</summary>
    public class PromoverVendedorDto
    {
        /// <summary>Id do usuário (cliente) a ser promovido. Opcional se informar e-mail.</summary>
        public Guid? UsuarioId { get; set; }

        /// <summary>E-mail do usuário a ser promovido (alternativa ao Id).</summary>
        [EmailAddress]
        public string? Email { get; set; }

        public string? Cargo { get; set; }
    }
}
