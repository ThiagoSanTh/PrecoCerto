using Pc.Dominio.Enums;
using Pc.Servico.Implementacoes.IA;
using Xunit;

namespace Pc.Servico.Tests
{
    public class IAIntencaoClassificadorTests
    {
        [Theory]
        [InlineData("tem arroz?", IAIntencao.DisponibilidadeProduto)]
        [InlineData("Tem ARROZ disponível?", IAIntencao.DisponibilidadeProduto)]
        [InlineData("onde vende arroz?", IAIntencao.LojasPorProduto)]
        [InlineData("Onde encontro arroz?", IAIntencao.LojasPorProduto)]
        [InlineData("quanto custa arroz?", IAIntencao.PrecoProduto)]
        [InlineData("Qual o preço do arroz?", IAIntencao.PrecoProduto)]
        [InlineData("qual a distância até o mercado?", IAIntencao.DistanciaLoja)]
        [InlineData("A loja Mercado Central fica a quantos km de mim?", IAIntencao.DistanciaLoja)]
        [InlineData("tem promoção de sabonete?", IAIntencao.OfertaProduto)]
        [InlineData("qual a capital do Brasil?", IAIntencao.ForaDoDominio)]
        [InlineData("me conte uma piada", IAIntencao.ForaDoDominio)]
        [InlineData("Quem é Neymar?", IAIntencao.ForaDoDominio)]
        [InlineData("Como fazer arroz?", IAIntencao.ForaDoDominio)]
        [InlineData("Qual a previsão do tempo?", IAIntencao.ForaDoDominio)]
        public void Classifica_intencoes_principais(string mensagem, IAIntencao esperada)
        {
            Assert.Equal(esperada, IAIntencaoClassificador.Classificar(mensagem));
        }

        [Fact]
        public void Mensagem_vazia_nao_entendida()
        {
            Assert.Equal(IAIntencao.NaoEntendida, IAIntencaoClassificador.Classificar(""));
            Assert.Equal(IAIntencao.NaoEntendida, IAIntencaoClassificador.Classificar("   "));
        }

        [Fact]
        public void Mensagem_muito_longa_nao_entendida()
        {
            var longa = new string('a', IAIntencaoClassificador.MaxMensagemChars + 1);
            Assert.Equal(IAIntencao.NaoEntendida, IAIntencaoClassificador.Classificar(longa));
        }

        [Fact]
        public void Acentos_e_plural_nao_quebram_classificacao()
        {
            Assert.Equal(IAIntencao.DisponibilidadeProduto, IAIntencaoClassificador.Classificar("Tem sabonetes disponíveis?"));
            Assert.Equal(IAIntencao.OfertaProduto, IAIntencaoClassificador.Classificar("Tem promoção de sabonetes?"));
        }

        [Fact]
        public void Caracteres_especiais_nao_quebram()
        {
            var r = IAIntencaoClassificador.Classificar("tem arroz??? !!!");
            Assert.Equal(IAIntencao.DisponibilidadeProduto, r);
        }
    }

    public class IAEntidadeExtratorTests
    {
        [Fact]
        public void Extrai_produto_de_disponibilidade()
        {
            var e = IAEntidadeExtrator.Extrair("Tem arroz disponível?", IAIntencao.DisponibilidadeProduto);
            Assert.NotNull(e.Produto);
            Assert.Contains("arroz", e.Produto!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Extrai_loja_de_distancia()
        {
            var e = IAEntidadeExtrator.Extrair(
                "A loja Mercado Central fica a quantos km?",
                IAIntencao.DistanciaLoja);
            Assert.NotNull(e.Loja);
            Assert.Contains("Mercado", e.Loja!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Extrai_produto_de_promocao()
        {
            var e = IAEntidadeExtrator.Extrair("tem promoção de sabonete?", IAIntencao.OfertaProduto);
            Assert.NotNull(e.Produto);
            Assert.Contains("sabonete", e.Produto!, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class TextoNormalizadorTests
    {
        [Fact]
        public void Remove_acentos_e_lowercase()
        {
            Assert.Equal("arroz", TextoNormalizador.Normalizar("  Árroz "));
            Assert.True(TextoNormalizador.Contem("Arroz 5kg", "arroz"));
        }
    }

    public class RespostaDeterministicaServicoTests
    {
        private readonly RespostaDeterministicaServico _sut = new();

        [Fact]
        public void Fora_do_dominio_usa_mensagem_padrao()
        {
            var texto = _sut.Gerar(new Pc.Servico.Modelos.IA.IAContextoResposta
            {
                Intencao = IAIntencao.ForaDoDominio
            });
            Assert.Equal(RespostaDeterministicaServico.MensagemForaDominio, texto);
        }

        [Fact]
        public void Produto_inexistente_nao_inventa()
        {
            var texto = _sut.Gerar(new Pc.Servico.Modelos.IA.IAContextoResposta
            {
                Intencao = IAIntencao.DisponibilidadeProduto,
                ProdutoMencionado = "xyzabc",
                EntidadeNaoEncontrada = true
            });
            Assert.Contains("xyzabc", texto);
            Assert.Contains("Não encontrei", texto);
        }

        [Fact]
        public void Loja_inexistente_nao_inventa()
        {
            var texto = _sut.Gerar(new Pc.Servico.Modelos.IA.IAContextoResposta
            {
                Intencao = IAIntencao.InformacaoLoja,
                LojaMencionada = "Loja Fantasma",
                EntidadeNaoEncontrada = true
            });
            Assert.Contains("Loja Fantasma", texto);
            Assert.Contains("Não encontrei", texto);
        }
    }
}
