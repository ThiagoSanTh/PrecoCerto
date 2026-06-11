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
  const { data } = await api.get(`/HistoricoPesquisa/cliente/${clienteId}`);
  return data;
}

export async function limparHistoricoCliente(clienteId) {
  await api.delete(`/HistoricoPesquisa/cliente/${clienteId}/limpar`);
}
