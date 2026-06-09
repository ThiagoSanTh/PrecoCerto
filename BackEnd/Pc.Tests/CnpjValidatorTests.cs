using Pc.Dominio.Validacoes;
using Xunit;

namespace Pc.Tests
{
    public class CnpjValidatorTests
    {
        [Theory]
        [InlineData("11.222.333/0001-81")]
        [InlineData("11222333000181")]
        [InlineData("04.252.011/0001-10")]
        public void IsValido_DeveAceitarCnpjValido(string cnpj)
        {
            Assert.True(CnpjValidator.IsValido(cnpj));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("123")]
        [InlineData("11222333000180")]   // DV incorreto
        [InlineData("00000000000000")]   // sequência repetida
        [InlineData("11.222.333/0001-00")]
        public void IsValido_DeveRejeitarCnpjInvalido(string? cnpj)
        {
            Assert.False(CnpjValidator.IsValido(cnpj));
        }

        [Fact]
        public void ApenasDigitos_DeveRemoverFormatacao()
        {
            Assert.Equal("11222333000181", CnpjValidator.ApenasDigitos("11.222.333/0001-81"));
        }
    }
}
