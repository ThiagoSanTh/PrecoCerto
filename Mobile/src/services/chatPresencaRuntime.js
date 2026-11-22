import AsyncStorage from '@react-native-async-storage/async-storage';
import api from './api';
import { getToken } from './tokenStorage';
import {
  calcularDesde,
  createPresencaCiclo,
  enviarFecharKeepalive,
  enviarPresenca,
} from './chatPresenca';

const TOKEN_KEY = '@auth_token';

let usuarioAtual = null;
let tokenCache = null;
let marcacao = { ultimoCheckEm: null, chatFechadoEm: null };

function chave(usuarioId) {
  return `chatDesde:v1:${usuarioId}`;
}

function url(acao) {
  const base = String(api.defaults.baseURL || '').replace(/\/$/, '');
  return `${base}/Conversas/chat/${acao}`;
}

function tokenSincrono() {
  if (tokenCache) return tokenCache;
  try {
    if (typeof localStorage !== 'undefined') return localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
  return null;
}

async function ler(usuarioId) {
  try {
    const raw = await AsyncStorage.getItem(chave(usuarioId));
    if (!raw) return null;
    const data = JSON.parse(raw);
    return {
      ultimoCheckEm: data?.ultimoCheckEm ?? null,
      chatFechadoEm: data?.chatFechadoEm ?? null,
    };
  } catch {
    return null;
  }
}

function gravar(usuarioId, valor) {
  const corpo = {
    ultimoCheckEm: valor?.ultimoCheckEm ?? null,
    chatFechadoEm: valor?.chatFechadoEm ?? null,
  };
  return AsyncStorage.setItem(chave(usuarioId), JSON.stringify(corpo)).catch(() => {});
}

function lembrarFechamento() {
  marcacao = { ...marcacao, chatFechadoEm: new Date().toISOString() };
  if (usuarioAtual) void gravar(usuarioAtual, marcacao);
}

async function abrirChat() {
  const token = tokenSincrono() || (await getToken());
  if (!token) return null;
  tokenCache = token;
  return enviarPresenca(url('abrir'), token);
}

function fecharChat({ keepalive }) {
  lembrarFechamento();
  const token = tokenSincrono();
  if (!token) return null;
  if (keepalive) return enviarFecharKeepalive(url('fechar'), token);
  return enviarPresenca(url('fechar'), token);
}

const ciclo = createPresencaCiclo({
  abrir: abrirChat,
  fechar: fecharChat,
});

export function desdeAtual() {
  return calcularDesde(marcacao);
}

export function presencaRegistrarCheck() {
  marcacao = { ...marcacao, ultimoCheckEm: new Date().toISOString() };
  if (usuarioAtual) void gravar(usuarioAtual, marcacao);
}

export async function presencaAoEntrar(usuarioId) {
  usuarioAtual = String(usuarioId);
  const salvo = await ler(usuarioAtual);
  marcacao = {
    ultimoCheckEm: salvo?.ultimoCheckEm ?? null,
    chatFechadoEm: salvo?.chatFechadoEm ?? null,
  };
  const token = tokenSincrono() || (await getToken());
  if (!token) return desdeAtual();
  tokenCache = token;
  const estado = await enviarPresenca(url('estado'), token, { method: 'GET' });
  if (estado === false) {
    ciclo.marcarSemSuporte();
    return desdeAtual();
  }
  if (!estado) return desdeAtual();
  marcacao = {
    ultimoCheckEm: marcacao.ultimoCheckEm,
    chatFechadoEm: estado.chatFechadoEm ?? marcacao.chatFechadoEm,
  };
  void gravar(usuarioAtual, marcacao);
  ciclo.reconciliar(Boolean(estado.chatAberto));
  return desdeAtual();
}

export function presencaEncerrar() {
  ciclo.reset();
  usuarioAtual = null;
  tokenCache = null;
  marcacao = { ultimoCheckEm: null, chatFechadoEm: null };
}

export function presencaFoco() {
  ciclo.foco();
}

export function presencaBlur() {
  ciclo.blur();
}

export function presencaBackground() {
  ciclo.background();
}

export function presencaOcultar() {
  ciclo.ocultar();
}

export function presencaForeground() {
  ciclo.foreground();
}

export function presencaPageHide() {
  ciclo.pagehide();
}

export function presencaUsuario() {
  return usuarioAtual;
}
