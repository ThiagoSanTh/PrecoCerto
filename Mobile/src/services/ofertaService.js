import api from './api';

function normalizarPaginado(data) {
  if (Array.isArray(data)) {
    return { items: data, page: 1, pageSize: data.length, total: data.length, hasNext: false };
  }
  return {
    items: Array.isArray(data?.items) ? data.items : [],
    page: data?.page ?? 1,
    pageSize: data?.pageSize ?? 20,
    total: data?.total ?? 0,
    hasNext: data?.hasNext ?? false,
  };
}

export async function listarOfertas(page = 1, pageSize = 50) {
  const { data } = await api.get('/Ofertas', { params: { page, pageSize } });
  return normalizarPaginado(data);
}

export async function listarOfertasPorProduto(produtoId) {
  const { data } = await api.get(`/Ofertas/produto/${produtoId}`);
  return data;
}

export async function criarOferta(oferta) {
  const { data } = await api.post('/Ofertas', oferta);
  return data;
}

export async function atualizarOferta(id, oferta) {
  await api.put(`/Ofertas/${id}`, oferta);
}

export async function removerOferta(id) {
  await api.delete(`/Ofertas/${id}`);
}
