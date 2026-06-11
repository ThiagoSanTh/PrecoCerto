using System.ComponentModel.DataAnnotations;
using Pc.WebApi.Validacao;

namespace Pc.WebApi.DTOs.Estabelecimentos
{
    public class LojaCriarDto
    {
        [Required, MinLength(2), MaxLength(150)]
        public string NomeFantasia { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? RazaoSocial { get; set; }

        [CnpjValido, MaxLength(18)]
        public string? Cnpj { get; set; }

        [TelefoneValido, MaxLength(20)]
        public string? Telefone { get; set; }

        [EmailAddress, MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(1000)]
        public string? Descricao { get; set; }

        public EnderecoDto? Endereco { get; set; }
        public Guid? EnderecoId { get; set; }
        public Guid? UsuarioId { get; set; }
    }
}
