using Pc.Dominio.Entities.Estabelecimentos;
using Pc.Servico.Interfaces;
using Pc.WebApi.DTOs.Estabelecimentos;

namespace Pc.WebApi.Mappings
{
    public static class LojaMapper
    {
        public static LojaRespostaDto ParaRespostaDto(Loja loja, IIdCodificador? codificador = null)
        {
            return new LojaRespostaDto
            {
                Id = loja.Id,
                CodigoPublico = codificador?.Codificar(loja.Id) ?? loja.Id.ToString(),
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

        public static LojaMapaDto ParaMapaDto(Loja loja, IIdCodificador? codificador = null)
        {
            return new LojaMapaDto
            {
                Id = loja.Id,
                CodigoPublico = codificador?.Codificar(loja.Id) ?? loja.Id.ToString(),
                NomeFantasia = loja.NomeFantasia,
                Latitude = loja.Endereco?.Latitude,
                Longitude = loja.Endereco?.Longitude
            };
        }
    }
}
