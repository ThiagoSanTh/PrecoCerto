using System.Globalization;
using Pc.Dominio.Enums;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.IA;

namespace Pc.Servico.Implementacoes.IA
{
    public class RespostaDeterministicaServico : IRespostaIAServico
    {
        public const string MensagemForaDominio =
            "Posso ajudar apenas com informações sobre produtos, lojas, ofertas e serviços disponíveis no Preço Certo.";

        public const string MensagemNaoEncontrada =
            "Não encontrei essa informação entre os dados disponíveis no Preço Certo.";

        public const string MensagemNaoEntendida =
            "Não entendi sua pergunta. Tente perguntar sobre produtos, lojas, preços ou ofertas.";

        public const string MensagemMuitoLonga =
            "Sua pergunta é muito longa. Tente resumir sua dúvida.";

        public const string MensagemPrecisaLocalizacao =
            "Para calcular a distância até as lojas, preciso da sua localização.";

        public string Gerar(IAContextoResposta ctx)
        {
            if (!string.IsNullOrWhiteSpace(ctx.MensagemFixa))
                return ctx.MensagemFixa;

            if (ctx.Intencao == IAIntencao.ForaDoDominio)
                return MensagemForaDominio;

            if (ctx.Intencao == IAIntencao.NaoEntendida)
                return MensagemNaoEntendida;

            if (ctx.PrecisaLocalizacao)
                return MensagemPrecisaLocalizacao;

            if (ctx.EntidadeNaoEncontrada)
            {
                if (!string.IsNullOrWhiteSpace(ctx.LojaMencionada) && string.IsNullOrWhiteSpace(ctx.LojaEncontrada))
                    return $"Não encontrei a loja \"{ctx.LojaMencionada}\" cadastrada no Preço Certo.";

                if (!string.IsNullOrWhiteSpace(ctx.ProdutoMencionado) && string.IsNullOrWhiteSpace(ctx.ProdutoEncontrado))
                    return $"Não encontrei \"{ctx.ProdutoMencionado}\" entre os produtos disponíveis no Preço Certo.";

                return MensagemNaoEncontrada;
            }

            return ctx.Intencao switch
            {
                IAIntencao.DisponibilidadeProduto => Disponibilidade(ctx),
                IAIntencao.LojasPorProduto => LojasPorProduto(ctx),
                IAIntencao.PrecoProduto => Preco(ctx),
                IAIntencao.DistanciaLoja => Distancia(ctx),
                IAIntencao.OfertaProduto => Oferta(ctx),
                IAIntencao.ProdutosDaLoja => ProdutosDaLoja(ctx),
                IAIntencao.InformacaoLoja => InformacaoLoja(ctx),
                IAIntencao.CategoriaProduto => Categoria(ctx),
                _ => MensagemNaoEntendida
            };
        }

        private static string Disponibilidade(IAContextoResposta ctx)
        {
            var nome = ctx.ProdutoEncontrado ?? ctx.ProdutoMencionado ?? "o produto";
            if (ctx.Disponivel == true)
            {
                var qtd = ctx.QuantidadeLojas ?? 0;
                if (qtd <= 0)
                    return $"Sim. Encontrei {nome} cadastrado no Preço Certo.";
                if (qtd == 1)
                    return $"Sim. Encontrei {nome} disponível em 1 loja.";
                return $"Sim. Encontrei {nome} disponível em {qtd} lojas.";
            }

            return $"Encontrei {nome}, mas não há disponibilidade registrada nas ofertas atuais.";
        }

        private static string LojasPorProduto(IAContextoResposta ctx)
        {
            var nome = ctx.ProdutoEncontrado ?? ctx.ProdutoMencionado ?? "o produto";
            var lojas = ctx.NomesLojas ?? Array.Empty<string>();
            if (lojas.Count == 0)
                return $"Encontrei {nome}, mas não encontrei lojas com disponibilidade nos dados atuais.";

            var lista = string.Join(", ", lojas.Take(5));
            if (lojas.Count == 1)
                return $"Você encontra {nome} em: {lista}.";
            return $"Encontrei {nome} em {lojas.Count} lojas. Exemplos: {lista}.";
        }

        private static string Preco(IAContextoResposta ctx)
        {
            var nome = ctx.ProdutoEncontrado ?? ctx.ProdutoMencionado ?? "o produto";
            if (ctx.Preco is null)
                return $"Encontrei {nome}, mas não há preço disponível nos dados atuais.";

            var preco = ctx.Preco.Value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
            var loja = ctx.LojaEncontrada;
            if (!string.IsNullOrWhiteSpace(loja))
                return $"O menor preço encontrado para {nome} é {preco} na loja {loja}.";
            return $"O menor preço encontrado para {nome} é {preco}.";
        }

        private static string Distancia(IAContextoResposta ctx)
        {
            var loja = ctx.LojaEncontrada ?? ctx.LojaMencionada ?? "a loja";
            if (ctx.DistanciaKm is null)
                return $"Encontrei a loja {loja}, mas não há coordenadas cadastradas para calcular a distância.";

            var km = ctx.DistanciaKm.Value.ToString("0.#", CultureInfo.GetCultureInfo("pt-BR"));
            return $"A loja {loja} fica aproximadamente {km} km de você.";
        }

        private static string Oferta(IAContextoResposta ctx)
        {
            var nome = ctx.ProdutoEncontrado ?? ctx.ProdutoMencionado ?? "o produto";
            if (!ctx.EmPromocao)
                return $"Não encontrei promoção ativa de {nome} nos dados atuais do Preço Certo.";

            var lojas = ctx.NomesLojas ?? Array.Empty<string>();
            if (lojas.Count == 0)
                return $"Sim. Há promoção de {nome} cadastrada no Preço Certo.";

            var lista = string.Join(", ", lojas.Take(3));
            return $"Sim. Encontrei promoção de {nome} em: {lista}.";
        }

        private static string ProdutosDaLoja(IAContextoResposta ctx)
        {
            var loja = ctx.LojaEncontrada ?? ctx.LojaMencionada ?? "a loja";
            var produtos = ctx.NomesProdutos ?? Array.Empty<string>();
            if (produtos.Count == 0)
                return $"Encontrei a loja {loja}, mas não encontrei produtos disponíveis nos dados atuais.";

            var lista = string.Join(", ", produtos.Take(8));
            var total = ctx.QuantidadeProdutos ?? produtos.Count;
            return $"Na loja {loja} encontrei {total} produto(s). Exemplos: {lista}.";
        }

        private static string InformacaoLoja(IAContextoResposta ctx)
        {
            var loja = ctx.LojaEncontrada ?? "a loja";
            if (ctx.DistanciaKm is decimal km)
            {
                var kmTxt = km.ToString("0.#", CultureInfo.GetCultureInfo("pt-BR"));
                return $"Sim. A loja {loja} está cadastrada no Preço Certo e fica aproximadamente {kmTxt} km de você.";
            }

            return $"Sim. A loja {loja} está cadastrada no Preço Certo.";
        }

        private static string Categoria(IAContextoResposta ctx)
        {
            var nome = ctx.ProdutoEncontrado ?? ctx.ProdutoMencionado ?? "o produto";
            if (string.IsNullOrWhiteSpace(ctx.Categoria))
                return $"Encontrei {nome}, mas não há categoria registrada.";
            return $"O produto {nome} está na categoria {ctx.Categoria}.";
        }
    }
}
