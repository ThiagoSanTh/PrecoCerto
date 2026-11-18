let pending = null;
let compraId = null;
const falhas = new Set();

export function setPendingAuthAction(action) {
  pending = action;
}

export function clearPendingAuthAction() {
  pending = null;
  compraId = null;
}

export function setCompraPendente(id) {
  compraId = id ? String(id) : null;
}

export function peekCompraPendente() {
  return compraId;
}

export function consumeCompraPendente() {
  const id = compraId;
  compraId = null;
  return id;
}

export function peekPendingAuthAction() {
  return pending;
}

export function consumePendingAuthAction() {
  const atual = pending;
  pending = null;
  return atual;
}

export function onAuthFailure(listener) {
  falhas.add(listener);
  return () => falhas.delete(listener);
}

export function emitAuthFailure() {
  falhas.forEach((listener) => {
    try {
      listener();
    } catch {
      /* um ouvinte não impede os outros */
    }
  });
}

export function irParaLogin(navigation) {
  let atual = navigation;
  while (atual) {
    const nomes = atual.getState?.()?.routeNames;
    if (Array.isArray(nomes) && nomes.includes('Login')) {
      atual.navigate('Login');
      return;
    }
    atual = typeof atual.getParent === 'function' ? atual.getParent() : null;
  }
  navigation.navigate('Login');
}
