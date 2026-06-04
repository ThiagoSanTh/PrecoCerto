import api from './api';

export async function registrarLojista(dados) {
  const { data } = await api.post('/Lojistas/registrar', dados);
  return data;
}

export async function loginLojista(email, senha) {
  const { data } = await api.post('/Lojistas/login', { email, senha });
  return {
    token: data.token,
    perfil: data.perfil,
    tipo: data.tipo,
  };
}

export async function obterLojista(id) {
  const { data } = await api.get(`/Lojistas/${id}`);
  return data;
}

export async function atualizarLojista(id, dados) {
  const { data } = await api.put(`/Lojistas/${id}`, dados);
  return data;
}
