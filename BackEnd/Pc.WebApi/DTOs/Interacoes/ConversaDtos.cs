namespace Pc.WebApi.DTOs.Interacoes
{
    public class ConversaRespostaDto
    {
        public string CodigoPublico { get; set; } = string.Empty;
        public string CodigoLoja { get; set; } = string.Empty;
        public string CodigoCliente { get; set; } = string.Empty;
        public string? NomeContato { get; set; }
        public DateTime? UltimaMensagemEm { get; set; }
        public int NaoLidas { get; set; }
    }

    public class MensagemRespostaDto
    {
        public string CodigoPublico { get; set; } = string.Empty;
        public string CodigoRemetente { get; set; } = string.Empty;
        public int RemetentePapel { get; set; }
        public string Texto { get; set; } = string.Empty;
        public DateTime EnviadaEm { get; set; }
        public bool Lida { get; set; }
    }

    public class MensagemCriarDto
    {
        public string Texto { get; set; } = string.Empty;
    }
}
