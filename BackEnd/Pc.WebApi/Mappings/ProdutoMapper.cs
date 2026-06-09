using Pc.Dominio.Entities.Catalogo;
using Pc.WebApi.DTOs.Catalogo;

namespace Pc.WebApi.Mappings
{
    public static class ProdutoMapper
    {
        public static ProdutoRespostaDto ParaRespostaDto(Produto p)
        {
            var dto = new ProdutoRespostaDto
            {
                Id = p.Id,
                Nome = p.NomeProduto,
                Descricao = p.Descricao,
                Marca = p.Marca ?? string.Empty,
                CodigoBarras = p.CodigoBarras ?? string.Empty,
                Preco = p.Preco,
                LojaId = p.LojaId,
                ImagemUrl = p.ImagemUrl,
                Categoria = p.Categoria,
                CategoriaNome = p.Categoria.ToString()
            };

            if (p.Loja == null)
                return dto;

            dto.LojaNomeFantasia = p.Loja.NomeFantasia;

            if (p.Loja.Endereco == null)
                return dto;

            dto.Latitude = p.Loja.Endereco.Latitude;
            dto.Longitude = p.Loja.Endereco.Longitude;
            dto.Logradouro = p.Loja.Endereco.Logradouro;
            dto.Cidade = p.Loja.Endereco.Cidade;

            return dto;
        }
    }
}
