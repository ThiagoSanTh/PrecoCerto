import api from './api';

export async function registrarPesquisa(clienteId, termoPesquisa, opcoes = {}) {
  const { produtoId = null, lojaId = null } = opcoes;
  const { data } = await api.post('/HistoricoPesquisa', {
    clienteId,
    termoPesquisa,
    produtoId,
    lojaId,
  });
  return data;
}

export async function listarHistoricoCliente(clienteId) {
  const { data } = await api.get(`/HistoricoPesquisa/cliente/${encodeURIComponent(String(clienteId))}`);
  return data;
}

export async function limparHistoricoCliente(clienteId) {
  await api.delete(`/HistoricoPesquisa/cliente/${encodeURIComponent(String(clienteId))}/limpar`);
}

export async function removerItemHistorico(id) {
  await api.delete(`/HistoricoPesquisa/${encodeURIComponent(String(id))}`);
}

export async function listarUltimosTermosCliente(clienteId, quantidade = 6) {
  const { data } = await api.get(`/HistoricoPesquisa/cliente/${encodeURIComponent(String(clienteId))}/ultimos`, {
    params: { quantidade },
  });
  return Array.isArray(data) ? data : [];
}

export async function obterRelatorioPesquisasLoja(lojaId) {
  const { data } = await api.get(`/HistoricoPesquisa/loja/${encodeURIComponent(String(lojaId))}/relatorio`);
  return Array.isArray(data) ? data : [];
}
