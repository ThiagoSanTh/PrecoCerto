using Pc.Dominio.Entities.Estabelecimentos;
using Pc.WebApi.DTOs.Estabelecimentos;

namespace Pc.WebApi.Mappings
{
    /// <summary>
    /// Converte entidade Loja para DTO de resposta (evita duplicação nos controllers).
    /// </summary>
    public static class LojaMapper
    {
        public static LojaRespostaDto ParaRespostaDto(Loja loja)
        {
            return new LojaRespostaDto
            {
                Id = loja.Id,
                NomeFantasia = loja.NomeFantasia,
                RazaoSocial = loja.RazaoSocial ?? string.Empty,
                Cnpj = loja.Cnpj ?? string.Empty,
                Telefone = loja.Telefone ?? string.Empty,
                Email = loja.Email ?? string.Empty,
                Endereco = loja.Endereco == null ? new EnderecoDto() : new EnderecoDto
                {
                    Cep = loja.Endereco.Cep ?? string.Empty,
                    Logradouro = loja.Endereco.Logradouro ?? string.Empty,
                    Numero = loja.Endereco.Numero ?? string.Empty,
                    Bairro = loja.Endereco.Bairro ?? string.Empty,
                    Cidade = loja.Endereco.Cidade ?? string.Empty,
                    Estado = loja.Endereco.Estado ?? string.Empty,
                    Latitude = loja.Endereco.Latitude,
                    Longitude = loja.Endereco.Longitude
                }
            };
        }
    }
}
