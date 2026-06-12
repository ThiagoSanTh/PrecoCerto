export function nomeProduto(produto) {
  return produto?.nome || produto?.nomeProduto || 'Produto';
}

/** Normaliza resposta da API (camelCase ou PascalCase) para uso em lista e mapa. */
export function normalizarProdutoApi(produto) {
  if (!produto) return produto;

  const imagemUrl = produto.imagemUrl ?? produto.ImagemUrl ?? null;
  const lat = produto.latitude ?? produto.Latitude;
  const lng = produto.longitude ?? produto.Longitude;

  return {
    ...produto,
    id: produto.id ?? produto.Id,
    lojaId: produto.lojaId ?? produto.LojaId ?? null,
    nome: produto.nome ?? produto.Nome ?? produto.nomeProduto ?? produto.NomeProduto,
    nomeProduto:
      produto.nomeProduto ?? produto.NomeProduto ?? produto.nome ?? produto.Nome ?? '',
    imagemUrl: imagemUrl && String(imagemUrl).trim() ? String(imagemUrl).trim() : null,
    lojaNomeFantasia:
      produto.lojaNomeFantasia ?? produto.LojaNomeFantasia ?? null,
    latitude: lat != null && lat !== '' ? Number(lat) : null,
    longitude: lng != null && lng !== '' ? Number(lng) : null,
    logradouro: produto.logradouro ?? produto.Logradouro ?? null,
    cidade: produto.cidade ?? produto.Cidade ?? null,
  };
}

export function normalizarListaProdutos(produtos) {
  return (produtos || []).map(normalizarProdutoApi);
}

export function filtrarProdutosPorTermo(produtos, termo) {
  const t = termo.trim().toLowerCase();
  if (!t) return produtos;

  return produtos.filter((p) => {
    const nome = nomeProduto(p).toLowerCase();
    const marca = (p.marca || '').toLowerCase();
    const descricao = (p.descricao || '').toLowerCase();
    const codigo = (p.codigoBarras || '').toLowerCase();
    return nome.includes(t) || marca.includes(t) || descricao.includes(t) || codigo.includes(t);
  });
}

/** Produto pertence à loja logada (somente dono pode editar/excluir). */
export function produtoPertenceALoja(produto, lojaId) {
  if (!produto || !lojaId) return false;
  const idLoja = produto.lojaId ?? produto.LojaId;
  return idLoja != null && String(idLoja) === String(lojaId);
}

/** Mescla produtos da loja (LojaId) com legado vinculado só por oferta. */
export function mesclarProdutosDaLoja(produtosPorLoja, todosProdutos, ofertas, lojaId) {
  const idsOferta = new Set(
    ofertas.filter((o) => o.lojaId === lojaId).map((o) => o.produtoId)
  );

  const map = new Map();
  produtosPorLoja.forEach((p) => map.set(p.id, p));
  todosProdutos
    .filter((p) => p.lojaId === lojaId || idsOferta.has(p.id))
    .forEach((p) => map.set(p.id, p));

  return [...map.values()];
}
