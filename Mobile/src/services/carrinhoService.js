import api from './api';

export async function obterCarrinho(clienteId) {
  const { data } = await api.get(`/Carrinho/${clienteId}`);
  return data;
}

export async function adicionarItemCarrinho(clienteId, item) {
  const { data } = await api.post(`/Carrinho/${clienteId}/itens`, {
    produtoId: item.produtoId,
    quantidade: item.quantidade ?? 1,
    precoUnitario: item.precoUnitario,
    ofertaId: item.ofertaId ?? null,
  });
  return data;
}

export async function atualizarItemCarrinho(clienteId, itemId, quantidade) {
  const { data } = await api.put(`/Carrinho/${clienteId}/itens/${itemId}`, { quantidade });
  return data;
}

export async function removerItemCarrinho(clienteId, itemId) {
  const { data } = await api.delete(`/Carrinho/${clienteId}/itens/${itemId}`);
  return data;
}

export async function limparCarrinho(clienteId) {
  await api.delete(`/Carrinho/${clienteId}`);
}
