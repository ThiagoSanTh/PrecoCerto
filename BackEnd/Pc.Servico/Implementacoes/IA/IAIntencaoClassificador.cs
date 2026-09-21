using Pc.Dominio.Enums;

namespace Pc.Servico.Implementacoes.IA
{
    /// <summary>
    /// Classificador leve baseado em palavras-chave (Fase 1).
    /// Preparado para ser substituído/complementado por LLM na Fase 2.
    /// </summary>
    public static class IAIntencaoClassificador
    {
        public const int MaxMensagemChars = 500;

        private static readonly string[] ForaDominio =
        {
            "presidente", "neymar", "piada", "bolo", "receita", "como fazer",
            "capital da", "capital do", "previsao do tempo", "previsão do tempo",
            "clima amanha", "clima amanhã", "quem ganhou", "copa do mundo",
            "futebol", "politica", "política", "religiao", "religião",
            "chatgpt", "inteligencia artificial geral", "conte uma historia",
            "conte uma história", "traduza", "escreva um poema"
        };

        private static readonly string[] Dominio =
        {
            "produto", "produtos", "loja", "lojas", "mercado", "supermercado",
            "oferta", "ofertas", "promocao", "promoção", "preco", "preço",
            "quanto custa", "disponivel", "disponível", "vende", "vender",
            "tem ", "onde ", "perto", "distancia", "distância", "quilometro",
            "quilômetro", " km", "categoria", "arroz", "sabonete", "barato",
            "encontrar", "encontrei", "estoque", "catalogo", "catálogo"
        };

        public static IAIntencao Classificar(string mensagem)
        {
            if (string.IsNullOrWhiteSpace(mensagem))
                return IAIntencao.NaoEntendida;

            var t = TextoNormalizador.Normalizar(mensagem);

            if (t.Length > MaxMensagemChars)
                return IAIntencao.NaoEntendida;

            if (ForaDominio.Any(p => t.Contains(TextoNormalizador.Normalizar(p), StringComparison.Ordinal)))
                return IAIntencao.ForaDoDominio;

            // Distância / localização de loja
            if (ContemAlgum(t, "distancia", "distância", "quantos km", "quantos quilometros",
                    "quantos quilômetros", "fica longe", "fica perto", "a quantos", "km de mim",
                    "quilometros de mim", "quilômetros de mim", "onde fica"))
            {
                if (ContemAlgum(t, "loja", "mercado", "supermercado") || PareceNomeDeLoja(t))
                    return IAIntencao.DistanciaLoja;
            }

            // Ofertas / promoções
            if (ContemAlgum(t, "promocao", "promoção", "oferta", "desconto", "em promocao", "em promoção"))
                return IAIntencao.OfertaProduto;

            // Preço / mais barato
            if (ContemAlgum(t, "preco", "preço", "quanto custa", "custa", "mais barato", "barato", "valor"))
                return IAIntencao.PrecoProduto;

            // Produtos de uma loja
            if (ContemAlgum(t, "quais produtos", "que produtos", "produtos essa loja",
                    "produtos desta loja", "produtos dessa loja", "o que vende", "o que essa loja vende"))
                return IAIntencao.ProdutosDaLoja;

            // Informação da loja (existência)
            if (ContemAlgum(t, "loja existe", "existe a loja", "tem a loja", "conhece a loja",
                    "cadastrada", "loja chamada"))
                return IAIntencao.InformacaoLoja;

            if ((t.StartsWith("a loja ") || t.Contains(" a loja ")) &&
                ContemAlgum(t, "existe", "tem", "cadastrad"))
                return IAIntencao.InformacaoLoja;

            // Categoria
            if (ContemAlgum(t, "categoria", "tipo de produto", "qual categoria"))
                return IAIntencao.CategoriaProduto;

            // Lojas por produto
            if (ContemAlgum(t, "onde vende", "onde encontro", "onde encontrar", "quais lojas",
                    "que lojas", "qual loja", "em qual loja", "loja vende", "lojas vendem",
                    "onde tem", "onde achar"))
                return IAIntencao.LojasPorProduto;

            // Disponibilidade
            if (ContemAlgum(t, "tem ", "tem?", "disponivel", "disponível", "ainda tem",
                    "esta disponivel", "está disponível", "vende ", "vendem "))
                return IAIntencao.DisponibilidadeProduto;

            // Se menciona domínio do Preço Certo mas sem padrão claro
            if (Dominio.Any(p => t.Contains(TextoNormalizador.Normalizar(p), StringComparison.Ordinal)))
                return IAIntencao.NaoEntendida;

            // Sem sinais de domínio e sem match → fora
            return IAIntencao.ForaDoDominio;
        }

        private static bool ContemAlgum(string texto, params string[] termos) =>
            termos.Any(t => texto.Contains(TextoNormalizador.Normalizar(t), StringComparison.Ordinal));

        private static bool PareceNomeDeLoja(string t) =>
            t.Contains("mercado", StringComparison.Ordinal) ||
            t.Contains("supermercado", StringComparison.Ordinal) ||
            t.Contains("loja", StringComparison.Ordinal);
    }
}
