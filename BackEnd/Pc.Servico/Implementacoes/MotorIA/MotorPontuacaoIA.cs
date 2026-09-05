using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    /// <summary>
    /// Score = Σ (criterio_i × peso_i) / Σ pesos_ativos
    /// Critérios normalizados em [0,1] onde 1 = melhor.
    /// </summary>
    public class MotorPontuacaoIA : IMotorPontuacaoIA
    {
        public void Pontuar(ContextoIA contexto)
        {
            var candidatos = contexto.Candidatos;
            if (candidatos.Count == 0)
                return;

            var precos = candidatos.Where(c => c.Preco.HasValue).Select(c => c.Preco!.Value).ToList();
            var dists = candidatos.Where(c => c.DistanciaKm.HasValue).Select(c => c.DistanciaKm!.Value).ToList();

            decimal precoMin = precos.Count > 0 ? precos.Min() : 0;
            decimal precoMax = precos.Count > 0 ? precos.Max() : 0;
            decimal distMin = dists.Count > 0 ? dists.Min() : 0;
            decimal distMax = dists.Count > 0 ? dists.Max() : 0;

            foreach (var c in candidatos)
            {
                var crit = new CriteriosNormalizadosIA
                {
                    Preco = NormalizarInvertido(c.Preco, precoMin, precoMax),
                    Distancia = c.DistanciaKm.HasValue
                        ? NormalizarInvertido(c.DistanciaKm, distMin, distMax)
                        : 0.5,
                    Promocao = c.EmPromocao ? 1.0 : 0.0,
                    Disponibilidade = c.Disponivel ? 1.0 : 0.0,
                    Avaliacao = c.MediaAvaliacao.HasValue
                        ? Math.Clamp((c.MediaAvaliacao.Value - 1.0) / 4.0, 0, 1)
                        : 0.5,
                    Entrega = contexto.NecessitaEntrega && c.DistanciaKm.HasValue
                        ? NormalizarInvertido(c.DistanciaKm, distMin, distMax)
                        : 0.5,
                    Conveniencia = 0.5
                };

                crit.Conveniencia = (crit.Disponibilidade + crit.Distancia) / 2.0;
                c.Criterios = crit;

                var pesos = contexto.Pesos;
                double somaPesos = 0;
                double soma = 0;
                void Acc(double peso, double valor)
                {
                    if (peso <= 0) return;
                    somaPesos += peso;
                    soma += valor * peso;
                }

                Acc(pesos.Preco, crit.Preco);
                Acc(pesos.Distancia, crit.Distancia);
                Acc(pesos.Promocao, crit.Promocao);
                Acc(pesos.Disponibilidade, crit.Disponibilidade);
                Acc(pesos.Entrega, crit.Entrega);
                Acc(pesos.Avaliacao, crit.Avaliacao);
                Acc(pesos.Conveniencia, crit.Conveniencia);

                c.Score = somaPesos > 0 ? Math.Round(soma / somaPesos, 4) : 0;
                c.Motivos = MontarMotivos(c, contexto);
            }

            contexto.Candidatos = candidatos.OrderByDescending(x => x.Score).ToList();
        }

        private static double NormalizarInvertido(decimal? valor, decimal min, decimal max)
        {
            if (valor is null) return 0.5;
            if (max <= min) return 1.0;
            var n = (double)((valor.Value - min) / (max - min));
            return Math.Clamp(1.0 - n, 0, 1);
        }

        private static List<string> MontarMotivos(CandidatoIA c, ContextoIA ctx)
        {
            var motivos = new List<string>();
            if (c.Preco.HasValue)
                motivos.Add($"preço R$ {c.Preco.Value:0.00}");
            if (c.DistanciaKm.HasValue)
                motivos.Add($"distância ~{c.DistanciaKm.Value:0.#} km");
            if (c.EmPromocao)
                motivos.Add("promoção ativa");
            if (c.MediaAvaliacao.HasValue && c.QuantidadeAvaliacoes > 0)
                motivos.Add($"avaliação {c.MediaAvaliacao.Value:0.0}/5");
            if (ctx.NecessitaEntrega)
                motivos.Add("preferência por não se deslocar (entrega não cadastrada no catálogo)");
            return motivos;
        }
    }
}
