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
