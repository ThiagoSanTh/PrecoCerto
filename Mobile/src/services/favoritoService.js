import api from './api';

export async function listarFavoritosCliente(clienteId) {
  const { data } = await api.get(`/Favoritos/cliente/${clienteId}`);
  return data;
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
