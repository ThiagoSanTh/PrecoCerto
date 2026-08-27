export const MODO_CLIENTE = 'user';
export const MODO_LOJA = 'store';
export const CONTEXTO_CLIENTE = 'cliente';
export const CONTEXTO_LOJA = 'loja';

let contextoOperacional = CONTEXTO_CLIENTE;

export function getContextoOperacional() {
  return contextoOperacional;
}

const PAPEL_LOJISTA = 2;
const PAPEL_VENDEDOR = 3;
const TIPO_NUM_LOJISTA = 2;
const TIPO_NUM_VENDEDOR = 4;

function temIdLoja(valor) {
  if (valor == null || valor === '') return false;
  const texto = String(valor).trim().toLowerCase();
  return texto !== 'null' && texto !== 'undefined' && texto !== '0';
}

export function temVinculoLoja(session) {
  if (!session) return false;
  const tipo = String(session.tipo ?? '').toLowerCase();
  if (tipo === 'lojista' || tipo === 'vendedor') return true;

  const perfil = session.perfil || {};
  if (temIdLoja(perfil.lojaId) || temIdLoja(perfil.lojaVinculadaId)) return true;

  const papel = Number(perfil.papel);
  if (papel === PAPEL_LOJISTA || papel === PAPEL_VENDEDOR) return true;

  const tipoNum = Number(perfil.tipo);
  return tipoNum === TIPO_NUM_LOJISTA || tipoNum === TIPO_NUM_VENDEDOR;
}

export function podeCriarLoja(session) {
  return Boolean(session) && !temVinculoLoja(session);
}

export function podeUsarModoLoja(session) {
  return temVinculoLoja(session);
}

export function modoPadraoParaSessao(session) {
  if (!session) return MODO_CLIENTE;
  return podeUsarModoLoja(session) ? MODO_LOJA : MODO_CLIENTE;
}

export function resolverModoSalvo(savedMode, session) {
  if (savedMode === MODO_LOJA && podeUsarModoLoja(session)) return MODO_LOJA;
  if (savedMode === MODO_CLIENTE) return MODO_CLIENTE;
  return modoPadraoParaSessao(session);
}

export function emModoLoja(appMode, session) {
  return appMode === MODO_LOJA && podeUsarModoLoja(session);
}

export function emModoCliente(appMode, session) {
  return Boolean(session) && !emModoLoja(appMode, session);
}

export function clienteIdDaSessao(session, appMode) {
  if (!emModoCliente(appMode, session)) return null;
  return session?.perfil?.id ?? null;
}

export function sincronizarContextoOperacional(appMode, session) {
  contextoOperacional = emModoLoja(appMode, session) ? CONTEXTO_LOJA : CONTEXTO_CLIENTE;
  return contextoOperacional;
}

export function funcionalidadesClienteAtivas(session, appMode) {
  return emModoCliente(appMode, session);
}

export function funcionalidadesLojistaAtivas(session, appMode) {
  return emModoLoja(appMode, session);
}

export function mesmaIdentidade(session, userId) {
  if (!session?.perfil?.id || userId == null || userId === '') return false;
  return String(session.perfil.id) === String(userId);
}

export function aplicarGpsNaSessao(session, userId, latitude, longitude) {
  if (!mesmaIdentidade(session, userId)) return session;
  return {
    ...session,
    perfil: {
      ...session.perfil,
      latitudeAtual: latitude,
      longitudeAtual: longitude,
    },
  };
}
