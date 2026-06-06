namespace Pc.WebApi.DTOs.Comum
{
    public class AuthLoginRespostaDto
    {
        public string Token { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public object Perfil { get; set; } = null!;
    }
}
