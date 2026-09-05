using Pc.Dominio.Enums;

namespace Pc.Servico.Modelos.IA
{
    /// <summary>
    /// Dados estruturados obtidos do backend. A camada de resposta
    /// (determinística agora; LLM no futuro) só transforma isso em texto.
    /// </summary>
    public class IAContextoResposta
    {
        public IAIntencao Intencao { get; set; }
        public string? ProdutoMencionado { get; set; }
        public string? LojaMencionada { get; set; }
        public string? ProdutoEncontrado { get; set; }
        public string? LojaEncontrada { get; set; }
        public bool? Disponivel { get; set; }
        public int? QuantidadeLojas { get; set; }
        public int? QuantidadeProdutos { get; set; }
        public decimal? Preco { get; set; }
        public decimal? DistanciaKm { get; set; }
        public bool EmPromocao { get; set; }
        public IReadOnlyList<string>? NomesLojas { get; set; }
        public IReadOnlyList<string>? NomesProdutos { get; set; }
        public string? Categoria { get; set; }
        public bool EntidadeNaoEncontrada { get; set; }
        public bool PrecisaLocalizacao { get; set; }
        public string? MensagemFixa { get; set; }
    }
}
