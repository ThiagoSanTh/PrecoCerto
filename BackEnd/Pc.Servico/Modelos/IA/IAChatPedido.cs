namespace Pc.Servico.Modelos.IA
{
    public class IAChatPedido
    {
        public string Mensagem { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid? UsuarioId { get; set; }
    }
}
