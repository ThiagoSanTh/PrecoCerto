using System.Globalization;
using Pc.Dominio.Enums.MotorIA;
using Pc.Servico.Interfaces.MotorIA;
using Pc.Servico.Modelos.MotorIA;

namespace Pc.Servico.Implementacoes.MotorIA
{
    public class GeradorRespostaIA : IGeradorRespostaIA
    {
        public string Gerar(ContextoIA contexto)
        {
            if (contexto.Intencao == IntencaoIA.ForaDoDominio)
            {
                return "Posso ajudar apenas com informações sobre produtos, lojas, ofertas e serviços disponíveis no Preço Certo.";
            }

            if (contexto.Intencao == IntencaoIA.NaoEntendida)
            {
                return "Não entendi sua solicitação. Tente perguntar sobre produtos, preços, lojas ou ofertas.";
            }

            if (contexto.Intencao == IntencaoIA.ConsultarEntrega)
            {
                var baseEntrega =
                    "Ainda não temos informação de entrega/delivery cadastrada por loja no Preço Certo. ";
                if (contexto.Decisao?.Melhor is { } m && !string.IsNullOrWhiteSpace(m.NomeLoja))
                {
                    return baseEntrega +
                           $"Enquanto isso, a opção mais conveniente encontrada foi {m.NomeLoja}" +
                           (m.DistanciaKm is decimal d ? $" (~{FormatKm(d)} km)." : ".");
                }

                return baseEntrega + "Consulte as opções próximas quando a localização estiver disponível.";
            }

            if (contexto.Decisao?.Melhor is null || contexto.Candidatos.Count == 0)
            {
                return "Não encontrei informações suficientes para recomendar uma opção.";
            }

            var melhor = contexto.Decisao.Melhor;
            var produto = melhor.NomeProduto ?? contexto.ProdutoTermo;
            var loja = melhor.NomeLoja ?? "a loja";
            var qtd = contexto.Candidatos.Count;

            if (contexto.Intencao is IntencaoIA.CompararPrecos && contexto.Decisao.Ranking.Count >= 2)
            {
                var a = contexto.Decisao.Ranking[0];
                var b = contexto.Decisao.Ranking[1];
                return $"A opção mais bem avaliada pelo motor foi {a.NomeLoja} " +
                       $"(score {a.Score:0.00})" +
                       (a.Preco is decimal pa ? $" com {a.NomeProduto} por {FormatPreco(pa)}" : "") +
                       $", enquanto {b.NomeLoja} ficou em segundo" +
                       (b.DistanciaKm is decimal db ? $" (~{FormatKm(db)} km)." : ".");
            }

            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(produto))
                partes.Add($"Encontrei {qtd} opção(ões) de {produto}.");
            else
                partes.Add($"Encontrei {qtd} opção(ões) de loja.");

            var rec = $"A melhor opção encontrada foi {loja}";
            if (!string.IsNullOrWhiteSpace(melhor.NomeProduto))
                rec += $", com {melhor.NomeProduto}";
            if (melhor.Preco is decimal p)
                rec += $" por {FormatPreco(p)}";
            if (melhor.DistanciaKm is decimal km)
                rec += $", a aproximadamente {FormatKm(km)} km";
            rec += ".";
            partes.Add(rec);

            if (melhor.EmPromocao)
                partes.Add("Há promoção ativa nessa opção.");

            return string.Join(' ', partes);
        }

        private static string FormatPreco(decimal preco) =>
            preco.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

        private static string FormatKm(decimal km) =>
            km.ToString("0.#", CultureInfo.GetCultureInfo("pt-BR"));
    }
}
