using Pc.Dominio.Enums;

namespace Pc.Servico.Modelos.IA
{
    public class IAChatResultado
    {
        public bool Sucesso { get; set; } = true;
        public IAIntencao Intencao { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public object? Dados { get; set; }
    }
}
