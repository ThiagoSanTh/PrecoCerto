namespace Pc.WebApi.DTOs.Comum
{
    public class LoginRespostaDto<TPerfil>
    {
        public string Token { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public TPerfil Perfil { get; set; } = default!;
    }
}
