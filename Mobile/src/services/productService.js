import api from './api';

export async function listarProdutos() {
  const response = await api.get('/Produtos');
  return response.data;
}

export async function buscarProdutosPorNome(nome) {
  const response = await api.post('/Produtos/Buscar', { nome });
  return response.data;
}

export async function criarProduto(produto) {
  const response = await api.post('/Produtos', produto);
  return response.data;
}

export async function atualizarProduto(id, produto) {
  await api.put(`/Produtos/${id}`, produto);
}

export async function removerProduto(id) {
  await api.delete(`/Produtos/${id}`);
}