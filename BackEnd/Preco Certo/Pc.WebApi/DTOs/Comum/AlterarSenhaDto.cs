using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Comum
{
    public class AlterarSenhaDto
    {
        [Required, MinLength(6)]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
