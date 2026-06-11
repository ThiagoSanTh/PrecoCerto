using System.ComponentModel.DataAnnotations;
using Pc.WebApi.Validacao;

namespace Pc.WebApi.DTOs.Comum
{
    public class AlterarEmailDto
    {
        [Required, MinLength(6), MaxLength(100)]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required, EmailValido, MaxLength(150)]
        public string NovoEmail { get; set; } = string.Empty;
    }
}
