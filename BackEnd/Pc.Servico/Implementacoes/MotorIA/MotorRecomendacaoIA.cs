using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class MotorRecomendacaoIA : IMotorRecomendacaoIA
    {
        private readonly IMotorPontuacaoIA _pontuacao;

        public MotorRecomendacaoIA(IMotorPontuacaoIA pontuacao)
        {
            _pontuacao = pontuacao;
        }

        public DecisaoIA Recomendar(ContextoIA contexto)
        {
            contexto.Candidatos = contexto.Candidatos
                .Where(c => c.Disponivel && (c.Preco is null || c.Preco > 0))
                .Where(c => contexto.OrcamentoMax is null || c.Preco is null || c.Preco <= contexto.OrcamentoMax)
                .Where(c => contexto.DistanciaMaxKm is null || c.DistanciaKm is null || c.DistanciaKm <= contexto.DistanciaMaxKm)
                .ToList();

            _pontuacao.Pontuar(contexto);

            var ranking = contexto.Candidatos.Take(5).ToList();
            var melhor = ranking.FirstOrDefault();

            var decisao = new DecisaoIA
            {
                Melhor = melhor,
                Ranking = ranking,
                RegrasAplicadas = contexto.RegrasAplicadas.ToList(),
                Confianca = CalcularConfianca(contexto, melhor),
                Motivos = melhor?.Motivos.ToList() ?? new List<string> { "Sem resultados suficientes." },
                DadosUtilizadosResumo = $"candidatos={contexto.Candidatos.Count};rag={contexto.UsouRag}"
            };

            contexto.Decisao = decisao;
            contexto.NivelConfianca = decisao.Confianca;
            return decisao;
        }

        private static double CalcularConfianca(ContextoIA ctx, CandidatoIA? melhor)
        {
            double pts = 0;
            const double max = 6;
            if (ctx.Intencao is not IntencaoIA.NaoEntendida and not IntencaoIA.ForaDoDominio)
                pts += 1;
            if (!string.IsNullOrWhiteSpace(ctx.ProdutoTermo) || !string.IsNullOrWhiteSpace(ctx.LojaTermo))
                pts += 1;
            if (melhor is not null) pts += 1;
            if (ctx.Latitude is not null && ctx.Longitude is not null) pts += 1;
            if (ctx.ClimaDisponivel) pts += 1;
            if (ctx.UsouRag && ctx.ResultadosRag > 0) pts += 1;
            return Math.Round(pts / max, 2);
        }
    }
}
