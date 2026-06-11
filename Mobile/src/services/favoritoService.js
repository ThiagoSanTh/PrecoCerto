import api from './api';

function normalizarFavorito(item) {
  if (!item) return null;
  return {
    id: item.id,
    clienteId: item.clienteId ?? item.ClienteId,
    produtoId: item.produtoId ?? item.ProdutoId,
    nomeProduto: item.nomeProduto ?? item.NomeProduto,
    lojaId: item.lojaId ?? item.LojaId,
    nomeLoja: item.nomeLoja ?? item.NomeLoja,
    dataCriacao: item.dataCriacao ?? item.DataCriacao,
    imagemUrl: item.imagemUrl ?? item.ImagemUrl,
    precoBase: item.precoBase ?? item.PrecoBase,
    precoExibicao: item.precoExibicao ?? item.PrecoExibicao,
    precoAnterior: item.precoAnterior ?? item.PrecoAnterior,
    emPromocao: item.emPromocao ?? item.EmPromocao ?? false,
    produto: item.produtoId
      ? {
          id: item.produtoId ?? item.ProdutoId,
          nome: item.nomeProduto ?? item.NomeProduto,
          imagemUrl: item.imagemUrl ?? item.ImagemUrl,
          preco: item.precoBase ?? item.PrecoBase,
          lojaId: item.lojaId ?? item.LojaId,
        }
      : null,
    oferta: item.precoExibicao != null
      ? {
          preco: item.precoExibicao ?? item.PrecoExibicao,
          precoAnterior: item.precoAnterior ?? item.PrecoAnterior,
          emPromocao: item.emPromocao ?? item.EmPromocao,
        }
      : null,
  };
}

function normalizarPaginado(data) {
  const items = (Array.isArray(data?.items) ? data.items : Array.isArray(data) ? data : [])
    .map(normalizarFavorito)
    .filter(Boolean);
  return {
    items,
    page: data?.page ?? 1,
    pageSize: data?.pageSize ?? 20,
    total: data?.total ?? items.length,
    hasNext: data?.hasNext ?? false,
  };
}

export async function listarFavoritosCliente(clienteId, page = 1, pageSize = 20) {
  const { data } = await api.get(`/Favoritos/cliente/${clienteId}`, {
    params: { page, pageSize },
  });
  return normalizarPaginado(data);
}

export async function adicionarFavorito(favorito) {
  const { data } = await api.post('/Favoritos', favorito);
  return data;
}

export async function removerFavorito(id) {
  await api.delete(`/Favoritos/${id}`);
}

export async function removerFavoritoProduto(clienteId, produtoId) {
  await api.delete(`/Favoritos/cliente/${clienteId}/produto/${produtoId}`);
}

export async function verificarFavorito(clienteId, produtoId = null, lojaId = null) {
  const params = new URLSearchParams();
  if (produtoId) params.append('produtoId', produtoId);
  if (lojaId) params.append('lojaId', lojaId);
  const { data } = await api.get(`/Favoritos/verificar/${clienteId}?${params}`);
  return data.ehFavorito;
}
