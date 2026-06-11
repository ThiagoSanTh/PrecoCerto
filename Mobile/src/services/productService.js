import api from './api';
import { listarOfertas } from './ofertaService';
import {
  mesclarProdutosDaLoja,
  normalizarListaProdutos,
  normalizarProdutoApi,
} from '../utils/produtoUtils';

function normalizarPaginado(data) {
  if (Array.isArray(data)) {
    return {
      items: normalizarListaProdutos(data),
      page: 1,
      pageSize: data.length,
      total: data.length,
      hasNext: false,
    };
  }
  return {
    items: normalizarListaProdutos(data?.items ?? []),
    page: data?.page ?? 1,
    pageSize: data?.pageSize ?? 20,
    total: data?.total ?? 0,
    hasNext: data?.hasNext ?? false,
  };
}

export async function listarProdutos(lojaId, page = 1, pageSize = 50) {
  const params = { page, pageSize };
  if (lojaId) params.lojaId = lojaId;
  const { data } = await api.get('/Produtos', { params });
  return normalizarPaginado(data);
}

export async function buscarProdutosPorNome(nome, lojaId, page = 1, pageSize = 20) {
  const { data } = await api.post('/Produtos/Buscar', {
    nome: nome.trim(),
    lojaId: lojaId || null,
    page,
    pageSize,
  });
  return normalizarPaginado(data);
}

export async function listarProdutosParaFeed(lojaId) {
  if (!lojaId) {
    const res = await listarProdutos(null, 1, 50);
    return res.items;
  }

  const [porLoja, ofertasRes] = await Promise.all([
    listarProdutos(lojaId, 1, 100),
    listarOfertas(1, 100),
  ]);

  return mesclarProdutosDaLoja(porLoja.items, [], ofertasRes.items, lojaId);
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
