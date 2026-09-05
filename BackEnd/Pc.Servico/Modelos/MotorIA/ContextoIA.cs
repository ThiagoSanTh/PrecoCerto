using Pc.Dominio.Enums;
using Pc.Dominio.Enums.MotorIA;

namespace Pc.Servico.Modelos.MotorIA
{
    public class PesosIA
    {
        public double Preco { get; set; } = 25;
        public double Distancia { get; set; } = 20;
        public double Promocao { get; set; } = 15;
        public double Disponibilidade { get; set; } = 10;
        public double Entrega { get; set; } = 10;
        public double Avaliacao { get; set; } = 10;
        public double Conveniencia { get; set; } = 10;

        public static PesosIA Padrao() => new();

        public PesosIA Clone() => new()
        {
            Preco = Preco,
            Distancia = Distancia,
            Promocao = Promocao,
            Disponibilidade = Disponibilidade,
            Entrega = Entrega,
            Avaliacao = Avaliacao,
            Conveniencia = Conveniencia
        };
    }

    public class CriteriosNormalizadosIA
    {
        public double Preco { get; set; }
        public double Distancia { get; set; }
        public double Promocao { get; set; }
        public double Disponibilidade { get; set; }
        public double Entrega { get; set; }
        public double Avaliacao { get; set; }
        public double Conveniencia { get; set; }
    }

    public class CandidatoIA
    {
        public Guid? ProdutoId { get; set; }
        public Guid? OfertaId { get; set; }
        public Guid? LojaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? NomeProduto { get; set; }
        public string? NomeLoja { get; set; }
        public decimal? Preco { get; set; }
        public decimal? DistanciaKm { get; set; }
        public bool EmPromocao { get; set; }
        public bool Disponivel { get; set; } = true;
        public double? MediaAvaliacao { get; set; }
        public int QuantidadeAvaliacoes { get; set; }
        public double Score { get; set; }
        public CriteriosNormalizadosIA Criterios { get; set; } = new();
        public List<string> Motivos { get; set; } = new();
    }

    public class DecisaoIA
    {
        public CandidatoIA? Melhor { get; set; }
        public IReadOnlyList<CandidatoIA> Ranking { get; set; } = Array.Empty<CandidatoIA>();
        public List<string> Motivos { get; set; } = new();
        public List<string> RegrasAplicadas { get; set; } = new();
        public double Confianca { get; set; }
        public string? DadosUtilizadosResumo { get; set; }
    }

    public class ContextoIA
    {
        public string MensagemOriginal { get; set; } = string.Empty;
        public string MensagemNormalizada { get; set; } = string.Empty;
        public IntencaoIA Intencao { get; set; } = IntencaoIA.NaoEntendida;
        public ObjetivoIA? Objetivo { get; set; }

        public string? ProdutoTermo { get; set; }
        public string? LojaTermo { get; set; }
        public CategoriaProduto? Categoria { get; set; }
        public string? Caracteristica { get; set; }
        public decimal? OrcamentoMax { get; set; }
        public decimal? DistanciaMaxKm { get; set; }
        public string? PesoOuUnidade { get; set; }

        public NivelPreferenciaIA PreferenciaPreco { get; set; } = NivelPreferenciaIA.Neutro;
        public NivelPreferenciaIA PreferenciaDistancia { get; set; } = NivelPreferenciaIA.Neutro;
        public NivelPreferenciaIA PreferenciaQualidade { get; set; } = NivelPreferenciaIA.Neutro;
        public bool UrgenciaAlta { get; set; }
        public bool NecessitaEntrega { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid? UsuarioId { get; set; }

        public bool? Chuva { get; set; }
        public string? ClimaDescricao { get; set; }
        public bool ClimaDisponivel { get; set; }

        public PesosIA Pesos { get; set; } = PesosIA.Padrao();
        public List<CandidatoIA> Candidatos { get; set; } = new();
        public DecisaoIA? Decisao { get; set; }

        public double NivelConfianca { get; set; }
        public List<string> FallbacksUsados { get; set; } = new();
        public List<string> RegrasAplicadas { get; set; } = new();
        public bool UsouRag { get; set; }
        public int ResultadosRag { get; set; }
    }

    public class PedidoAnaliseIA
    {
        public string Mensagem { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public class ResultadoItemAnaliseIA
    {
        public string Tipo { get; set; } = "Oferta";
        public string Titulo { get; set; } = string.Empty;
        public double Score { get; set; }
        public decimal? Preco { get; set; }
        public double? DistanciaKm { get; set; }
        public string? Loja { get; set; }
        public string? Produto { get; set; }
        public List<string> Motivos { get; set; } = new();
    }

    public class ResultadoAnaliseIA
    {
        public bool Sucesso { get; set; } = true;
        public string Resposta { get; set; } = string.Empty;
        public IntencaoIA Intencao { get; set; }
        public ObjetivoIA? Objetivo { get; set; }
        public double Confianca { get; set; }
        public List<ResultadoItemAnaliseIA> Resultados { get; set; } = new();
        public List<string> Motivos { get; set; } = new();
        public List<string> Fallbacks { get; set; } = new();
    }
}
