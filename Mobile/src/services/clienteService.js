import api from './api';

export async function registrarCliente(dados) {
  const { data } = await api.post('/Clientes/registrar', dados);
  return data;
}

export async function loginCliente(email, senha) {
  const { data } = await api.post('/Clientes/login', { email, senha });
  return {
    token: data.token,
    perfil: data.perfil,
    tipo: data.tipo,
  };
}

export async function alterarSenha(clienteId, senhaAtual, novaSenha) {
  await api.put(`/Clientes/${clienteId}/senha`, { senhaAtual, novaSenha });
}
