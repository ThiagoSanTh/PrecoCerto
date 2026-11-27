import api from './api';

export async function listarAvaliacoesLoja(lojaId) {
  const { data } = await api.get(`/Avaliacoes/loja/${encodeURIComponent(String(lojaId))}`);
  return data;
}

export async function obterMediaAvaliacoesLoja(lojaId) {
  const { data } = await api.get(`/Avaliacoes/loja/${encodeURIComponent(String(lojaId))}/media`);
  return data;
}

export async function criarAvaliacao(avaliacao) {
  const { data } = await api.post('/Avaliacoes', avaliacao);
  return data;
}
