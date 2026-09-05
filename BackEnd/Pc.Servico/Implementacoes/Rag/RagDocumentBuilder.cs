using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pc.Dominio.Enums;
using Pc.Infraestrutura;
using Pc.Servico.Interfaces;
using Pc.Servico.Modelos.Rag;

namespace Pc.Servico.Implementacoes.Rag
{
    public class RagDocumentBuilder : IRagDocumentBuilder
    {
        private readonly AppDbContext _db;

        public RagDocumentBuilder(AppDbContext db)
        {
            _db = db;
        }

        public async Task<RagDocumentoConstruido?> ConstruirAsync(
            RagDocumentoTipo tipo,
            Guid entidadeId,
            CancellationToken cancellationToken = default)
        {
            return tipo switch
            {
                RagDocumentoTipo.Produto => await ConstruirProdutoAsync(entidadeId, cancellationToken),
                RagDocumentoTipo.Loja => await ConstruirLojaAsync(entidadeId, cancellationToken),
                RagDocumentoTipo.Oferta => await ConstruirOfertaAsync(entidadeId, cancellationToken),
                RagDocumentoTipo.Avaliacao => await ConstruirAvaliacaoAsync(entidadeId, cancellationToken),
                _ => null
            };
        }

        private async Task<RagDocumentoConstruido?> ConstruirProdutoAsync(Guid id, CancellationToken ct)
        {
            var p = await _db.Produtos.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.NomeProduto,
                    x.Marca,
                    x.Descricao,
                    x.Categoria,
                    x.CodigoBarras,
                    x.Ativo,
                    LojaNome = x.Loja != null ? x.Loja.NomeFantasia : null
                })
                .FirstOrDefaultAsync(ct);

            if (p is null || !p.Ativo)
                return null;

            var linhas = new List<string>
            {
                $"Produto: {p.NomeProduto}."
            };
            if (!string.IsNullOrWhiteSpace(p.Marca))
                linhas.Add($"Marca: {p.Marca}.");
            linhas.Add($"Categoria: {p.Categoria}.");
            if (!string.IsNullOrWhiteSpace(p.Descricao))
                linhas.Add($"Descrição: {p.Descricao}.");
            if (!string.IsNullOrWhiteSpace(p.CodigoBarras))
                linhas.Add($"Código de barras: {p.CodigoBarras}.");
            if (!string.IsNullOrWhiteSpace(p.LojaNome))
                linhas.Add($"Loja: {p.LojaNome}.");

            return new RagDocumentoConstruido
            {
                Tipo = RagDocumentoTipo.Produto,
                EntidadeId = p.Id,
                Titulo = p.NomeProduto,
                Conteudo = string.Join(' ', linhas),
                Metadata = JsonSerializer.Serialize(new { categoria = p.Categoria.ToString(), marca = p.Marca })
            };
        }

        private async Task<RagDocumentoConstruido?> ConstruirLojaAsync(Guid id, CancellationToken ct)
        {
            var l = await _db.Lojas.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.NomeFantasia,
                    x.Descricao,
                    x.Ativo,
                    Cidade = x.Endereco.Cidade,
                    Bairro = x.Endereco.Bairro,
                    Estado = x.Endereco.Estado,
                    Logradouro = x.Endereco.Logradouro,
                    Numero = x.Endereco.Numero
                })
                .FirstOrDefaultAsync(ct);

            if (l is null || !l.Ativo)
                return null;

            var linhas = new List<string> { $"Loja: {l.NomeFantasia}." };
            if (!string.IsNullOrWhiteSpace(l.Descricao))
                linhas.Add($"Descrição: {l.Descricao}.");

            var endereco = string.Join(", ", new[] { l.Logradouro, l.Numero, l.Bairro, l.Cidade, l.Estado }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(endereco))
                linhas.Add($"Endereço: {endereco}.");

            return new RagDocumentoConstruido
            {
                Tipo = RagDocumentoTipo.Loja,
                EntidadeId = l.Id,
                Titulo = l.NomeFantasia,
                Conteudo = string.Join(' ', linhas),
                Metadata = JsonSerializer.Serialize(new { cidade = l.Cidade, estado = l.Estado })
            };
        }

        private async Task<RagDocumentoConstruido?> ConstruirOfertaAsync(Guid id, CancellationToken ct)
        {
            var o = await _db.Ofertas.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Preco,
                    x.PrecoAnterior,
                    x.EmPromocao,
                    x.Disponivel,
                    x.Ativo,
                    ProdutoNome = x.Produto != null ? x.Produto.NomeProduto : null,
                    Categoria = x.Produto != null ? x.Produto.Categoria : CategoriaProduto.Outros,
                    LojaNome = x.Loja != null ? x.Loja.NomeFantasia : null
                })
                .FirstOrDefaultAsync(ct);

            if (o is null || !o.Ativo)
                return null;

            var preco = o.Preco.ToString("0.00", CultureInfo.InvariantCulture);
            var linhas = new List<string>
            {
                $"Oferta do produto {o.ProdutoNome ?? "desconhecido"} na loja {o.LojaNome ?? "desconhecida"}.",
                $"Categoria: {o.Categoria}.",
                $"Preço: R$ {preco}.",
                o.EmPromocao ? "Em promoção." : "Sem promoção ativa.",
                o.Disponivel ? "Disponível." : "Indisponível."
            };

            return new RagDocumentoConstruido
            {
                Tipo = RagDocumentoTipo.Oferta,
                EntidadeId = o.Id,
                Titulo = $"{o.ProdutoNome} — {o.LojaNome}",
                Conteudo = string.Join(' ', linhas),
                Metadata = JsonSerializer.Serialize(new
                {
                    emPromocao = o.EmPromocao,
                    disponivel = o.Disponivel,
                    categoria = o.Categoria.ToString()
                })
            };
        }

        private async Task<RagDocumentoConstruido?> ConstruirAvaliacaoAsync(Guid id, CancellationToken ct)
        {
            var a = await _db.Avaliacoes.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Nota,
                    x.Comentario,
                    x.Ativo,
                    LojaNome = x.Loja != null ? x.Loja.NomeFantasia : null
                })
                .FirstOrDefaultAsync(ct);

            if (a is null || !a.Ativo || string.IsNullOrWhiteSpace(a.Comentario))
            {
                return new RagDocumentoConstruido
                {
                    Tipo = RagDocumentoTipo.Avaliacao,
                    EntidadeId = id,
                    DeveIndexar = false,
                    Titulo = string.Empty,
                    Conteudo = string.Empty
                };
            }

            var titulo = $"Avaliação {a.Nota}/5 — {a.LojaNome}";
            var conteudo = $"Avaliação da loja {a.LojaNome}: nota {a.Nota}. Comentário: {a.Comentario}";

            return new RagDocumentoConstruido
            {
                Tipo = RagDocumentoTipo.Avaliacao,
                EntidadeId = a.Id,
                Titulo = titulo,
                Conteudo = conteudo,
                Metadata = JsonSerializer.Serialize(new { nota = a.Nota })
            };
        }
    }
}
