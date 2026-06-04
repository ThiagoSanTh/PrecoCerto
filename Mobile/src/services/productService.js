import api from './api';
import { listarOfertas } from './ofertaService';
import {
  mesclarProdutosDaLoja,
  normalizarListaProdutos,
  normalizarProdutoApi,
} from '../utils/produtoUtils';

export async function listarProdutos(lojaId) {
  const params = lojaId ? { lojaId } : {};
  const { data } = await api.get('/Produtos', { params });
  return normalizarListaProdutos(Array.isArray(data) ? data : []);
}

export async function buscarProdutosPorNome(nome, lojaId) {
  const { data } = await api.post('/Produtos/Buscar', {
    nome: nome.trim(),
    lojaId: lojaId || null,
  });
  return normalizarListaProdutos(Array.isArray(data) ? data : []);
}

/**
 * Cliente: todos os produtos.
 * Loja: apenas produtos da loja (com fallback para ofertas já publicadas).
 */
export async function listarProdutosParaFeed(lojaId) {
  if (!lojaId) {
    return listarProdutos();
  }

  const [porLoja, todos, ofertas] = await Promise.all([
    listarProdutos(lojaId),
    listarProdutos(),
    listarOfertas(),
  ]);

  return mesclarProdutosDaLoja(porLoja, todos, ofertas, lojaId);
}

export async function buscarProdutoPorId(id) {
  const { data } = await api.get(`/Produtos/${id}`);
  return normalizarProdutoApi(data);
}

export async function criarProduto(produto) {
  const { data } = await api.post('/Produtos', produto);
  return data;
}

export async function atualizarProduto(id, produto) {
  await api.put(`/Produtos/${id}`, produto);
}

export async function removerProduto(id, lojaId) {
  await api.delete(`/Produtos/${id}`, { params: { lojaId } });
}
