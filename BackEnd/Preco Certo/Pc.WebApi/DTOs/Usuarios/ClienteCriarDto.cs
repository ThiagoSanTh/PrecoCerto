using System.ComponentModel.DataAnnotations;

namespace Pc.WebApi.DTOs.Usuarios
{
    public class ClienteCriarDto
    {
        [Required, MinLength(2)]
        public string NomeUsuario { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Senha { get; set; } = string.Empty;

        public string? Telefone { get; set; }
        public decimal? LatitudeAtual { get; set; }
        public decimal? LongitudeAtual { get; set; }
    }
}
