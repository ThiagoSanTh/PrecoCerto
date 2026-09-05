using Pc.Servico.Implementacoes.IA;

namespace Pc.Servico.Implementacoes.MotorIA.Vocabulario
{
    /// <summary>
    /// Vocabulário controlado e expansível (PT-BR) para o MotorIA v2.
    /// </summary>
    public static class VocabularioIA
    {
        public static readonly string[] PrecoBaixo =
        {
            "barato", "barata", "em conta", "economico", "econômica", "economica",
            "gastando pouco", "menor preco", "menor preço", "mais barato", "mais barata",
            "preco baixo", "preço baixo", "economizar", "economia"
        };

        public static readonly string[] DistanciaBaixa =
        {
            "perto", "perto de mim", "proximo", "próximo", "proxima", "próxima",
            "aqui perto", "nas redondezas", "nao quero ir longe", "não quero ir longe",
            "mais perto", "mais proximo", "mais próximo", "vizinho", "perto daqui"
        };

        public static readonly string[] Urgencia =
        {
            "agora", "rapido", "rápido", "urgente", "urgencia", "urgência",
            "preciso logo", "com urgencia", "com urgência", "imediatamente", "hoje"
        };

        public static readonly string[] Entrega =
        {
            "entregar", "delivery", "entrega", "nao quero sair", "não quero sair",
            "sem sair", "em casa", "recebo em casa"
        };

        public static readonly string[] Promocao =
        {
            "promocao", "promoção", "oferta", "desconto", "em promocao", "em promoção"
        };

        public static readonly string[] Qualidade =
        {
            "melhor", "qualidade", "bem avaliado", "bem avaliada", "nota alta", "recomendado"
        };

        public static readonly string[] ForaDominio =
        {
            "presidente", "neymar", "piada", "receita", "como fazer", "capital da",
            "capital do", "previsao do tempo", "previsão do tempo", "futebol", "chatgpt",
            "conte uma historia", "conte uma história", "traduza", "poema"
        };

        public static readonly Dictionary<string, string> SinonimosCategoria = new(StringComparer.OrdinalIgnoreCase)
        {
            ["limpeza"] = "Limpeza",
            ["higiene"] = "HigienePessoal",
            ["bebida"] = "Bebidas",
            ["bebidas"] = "Bebidas",
            ["alimento"] = "Alimentos",
            ["alimentos"] = "Alimentos",
            ["padaria"] = "Padaria",
            ["acougue"] = "Acougue",
            ["açougue"] = "Acougue",
            ["hortifruti"] = "Hortifruti",
            ["farmacia"] = "Farmacia",
            ["farmácia"] = "Farmacia"
        };

        public static bool ContemAlgum(string textoNormalizado, IEnumerable<string> termos) =>
            termos.Any(t => textoNormalizado.Contains(TextoNormalizador.Normalizar(t), StringComparison.Ordinal));
    }
}
