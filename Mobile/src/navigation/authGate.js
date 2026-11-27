export const AUTH_REQUIRED_MESSAGE = 'Para acessar esse recurso é necessário se cadastrar.';

export const GUEST_ALLOWED_TABS = new Set(['Buscar']);

export function isGuestTabAllowed(routeName) {
  return GUEST_ALLOWED_TABS.has(routeName);
}

export function redirecionarParaCadastro(navigation, message = AUTH_REQUIRED_MESSAGE) {
  navigation?.navigate?.('Cadastro', { authNotice: message });
}
