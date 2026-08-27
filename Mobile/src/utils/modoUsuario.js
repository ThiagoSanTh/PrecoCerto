export const MODO_CLIENTE = 'user';
export const MODO_LOJA = 'store';
export const CONTEXTO_CLIENTE = 'cliente';
export const CONTEXTO_LOJA = 'loja';

let contextoOperacional = CONTEXTO_CLIENTE;

export function getContextoOperacional() {
  return contextoOperacional;
}

export function podeUsarModoLoja(session) {
  if (!session) return false;
  return (
    session.tipo === 'lojista' ||
    session.tipo === 'vendedor' ||
    !!session.perfil?.lojaId
  );
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
