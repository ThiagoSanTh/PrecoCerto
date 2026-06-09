import api from './api';

export async function obterLojista(id) {
  const { data } = await api.get(`/Lojistas/${id}`);
  return data;
}

export async function atualizarLojista(id, dados) {
  const { data } = await api.put(`/Lojistas/${id}`, {
    nomeUsuario: dados.nomeUsuario,
    email: dados.email,
    telefone: dados.telefone,
    cargo: dados.cargo,
    // A API valida MinLength(6); placeholder não altera a senha.
    senha: dados.senha || 'nao-alterar',
  });
  return data;
}

export async function alterarSenhaLojista(lojistaId, senhaAtual, novaSenha) {
  await api.put(`/Lojistas/${lojistaId}/senha`, { senhaAtual, novaSenha });
}

// 🧑‍💼 Gestão de vendedores (controle de estoque) — apenas o lojista dono da loja.
export async function listarVendedores(lojaId) {
  const { data } = await api.get(`/Lojistas/loja/${lojaId}/vendedores`);
  return data;
}

export async function promoverVendedor(lojaId, { usuarioId, email, cargo }) {
  const { data } = await api.post(`/Lojistas/loja/${lojaId}/vendedores`, {
    usuarioId: usuarioId || null,
    email: email || null,
    cargo: cargo || null,
  });
  return data;
}

export async function removerVendedor(lojaId, usuarioId) {
  await api.delete(`/Lojistas/loja/${lojaId}/vendedores/${usuarioId}`);
}
