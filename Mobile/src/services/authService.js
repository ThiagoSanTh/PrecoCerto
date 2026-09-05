import api from './api';
import { saveToken } from './tokenStorage';

export async function login(email, senha, tipo = undefined) {
  let emailNorm = String(email || '').trim().toLowerCase();
  // Atalho de testes: "admin" → e-mail seed do AdminSeedHostedService
  if (emailNorm === 'admin') {
    emailNorm = 'admin@precocerto.local';
  }
  const payload = {
    email: emailNorm,
    senha,
  };
  if (tipo !== undefined && tipo !== null && String(tipo).trim() !== '') {
    payload.tipo = tipo;
  }

  const { data } = await api.post('/Auth/login', payload);

  if (data.token) {
    await saveToken(data.token);
  }

  return data;
}

export async function logoutApi() {
  // Stateless JWT — nada no servidor; token removido no cliente.
}

export async function solicitarRecuperacaoSenha(email) {
  const { data } = await api.post('/Auth/esqueci-senha', { email: email.trim() });
  return data;
}

export async function redefinirSenha(token, novaSenha) {
  const { data } = await api.post('/Auth/redefinir-senha', { token, novaSenha });
  return data;
}
