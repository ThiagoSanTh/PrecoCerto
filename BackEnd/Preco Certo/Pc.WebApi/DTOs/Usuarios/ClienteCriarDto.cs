namespace Pc.WebApi.DTOs.Usuarios
{
    public class ClienteCriarDto
    {
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public decimal? LatitudeAtual { get; set; }
        public decimal? LongitudeAtual { get; set; }
    }
}
