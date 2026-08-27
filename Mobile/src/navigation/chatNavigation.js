/**
 * Abre a tela de Chat a partir de qualquer stack aninhada.
 * Prefere a aba Mensagens quando existir; senão usa Chat da raiz.
 */
export function navegarParaChat(navigation, params) {
  let atual = navigation;
  while (atual) {
    const nomes = atual.getState?.()?.routeNames;
    if (Array.isArray(nomes) && nomes.includes('Mensagens')) {
      atual.navigate('Mensagens', { screen: 'Chat', params });
      return;
    }
    if (Array.isArray(nomes) && nomes.includes('Chat')) {
      atual.navigate('Chat', params);
      return;
    }
    atual = typeof atual.getParent === 'function' ? atual.getParent() : null;
  }
  navigation.navigate('Chat', params);
}
