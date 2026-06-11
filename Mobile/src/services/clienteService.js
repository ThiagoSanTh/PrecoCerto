import api from './api';

export async function registrarCliente(dados) {
  const { data } = await api.post('/Clientes/registrar', dados);
  return data;
}

export async function loginCliente(email, senha) {
  const { data } = await api.post('/Clientes/login', { email, senha });
  return data;
}

export async function obterCliente(id) {
  const { data } = await api.get(`/Clientes/${id}`);
  return data;
}

export async function atualizarCliente(id, dados) {
  const { data } = await api.put(`/Clientes/${id}`, {
    nomeUsuario: dados.nomeUsuario,
    email: dados.email || 'nao-alterar@placeholder.com',
    telefone: dados.telefone,
    senha: dados.senha || 'nao-alterar',
  });
  return data;
}

export async function alterarEmailCliente(id, senhaAtual, novoEmail) {
  const { data } = await api.put(`/Clientes/${id}/email`, { senhaAtual, novoEmail });
  return data;
}

/** Atualiza localização no servidor com coordenadas do GPS */
export async function atualizarLocalizacao(clienteId, latitude, longitude) {
  await api.put(`/Clientes/${clienteId}/localizacao`, { latitude, longitude });
}

export async function alterarSenha(clienteId, senhaAtual, novaSenha) {
  await api.put(`/Clientes/${clienteId}/senha`, { senhaAtual, novaSenha });
}
