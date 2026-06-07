namespace Pc.WebApi.DTOs.Usuarios
{
    public class AdminCriarDto
    {
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public int NivelAcesso { get; set; } = 2;
    }
}
