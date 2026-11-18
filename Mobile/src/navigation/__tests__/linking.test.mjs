import test from 'node:test';
import assert from 'node:assert/strict';
import { getStateFromPath } from '../../../node_modules/@react-navigation/core/lib/module/getStateFromPath.js';
import { getPathFromState } from '../../../node_modules/@react-navigation/core/lib/module/getPathFromState.js';
import { ajustarCaminho, linkingScreens, PUBLIC_ROUTE_NAMES, tituloDocumento } from '../linking.js';

function folha(state) {
  let route = state.routes[state.index ?? state.routes.length - 1];
  while (route?.state) {
    route = route.state.routes[route.state.index ?? route.state.routes.length - 1];
  }
  return route?.name;
}

function routeName(path) {
  const state = getStateFromPath(path, { screens: linkingScreens });
  assert.ok(state, `sem rota para ${path}`);
  return state.routes[state.routes.length - 1].name;
}

test('a vitrine fica em / e o refresh não cai no 404', () => {
  const state = getStateFromPath('/', { screens: linkingScreens });
  assert.equal(state.routes.at(-1).name, 'Home');
  const buscar = state.routes.at(-1).state.routes[0];
  assert.equal(buscar.name, 'Buscar');
  assert.equal(buscar.state.routes[0].name, 'Search');
  assert.equal(routeName('/Buscar/Search'), 'NotFound');
  const ia = getStateFromPath('/ia', { screens: linkingScreens });
  assert.equal(ia.routes.at(-1).name, 'Home');
  assert.equal(ia.routes.at(-1).state.routes.at(-1).name, 'IA');
});

test('caminhos públicos legíveis e aliases', () => {
  assert.equal(routeName('/'), 'Home');
  assert.equal(routeName('/home'), 'Home');
  assert.equal(routeName('/buscar'), 'Home');
  assert.equal(routeName('/login'), 'Login');
  assert.equal(routeName('/Login'), 'Login');
  assert.equal(routeName('/cadastro'), 'Cadastro');
  assert.equal(routeName('/Cadastro'), 'Cadastro');
  assert.equal(routeName('/esqueci-senha'), 'EsqueciSenha');
  assert.equal(routeName('/EsqueciSenha'), 'EsqueciSenha');
  for (const name of ['Login', 'Cadastro', 'EsqueciSenha', 'Home', 'Search', 'ProductDetail', 'StoreCatalog']) {
    assert.equal(PUBLIC_ROUTE_NAMES.has(name), true);
  }
});

test('telas autenticadas não são públicas', () => {
  const paths = ['/criar-loja', '/CreateStore', '/chat', '/assistente', '/editar-perfil'];
  for (const path of paths) {
    const name = routeName(path);
    assert.equal(PUBLIC_ROUTE_NAMES.has(name), false, path);
  }
});

test('rotas do modo loja sobrevivem ao reload', () => {
  const config = { screens: linkingScreens };
  const esperados = {
    '/Painel': 'Painel',
    '/home/Painel': 'Painel',
    '/painel': 'Painel',
    '/Produtos': 'Produtos',
    '/home/Produtos': 'Produtos',
    '/Loja': 'Loja',
    '/home/Loja': 'Loja',
    '/Conta': 'Conta',
    '/home/Conta': 'Conta',
    '/mensagens': 'Mensagens',
    '/home/Mensagens': 'Mensagens',
    '/ia': 'IA',
    '/home/IA': 'IA',
  };
  for (const [path, nome] of Object.entries(esperados)) {
    const state = getStateFromPath(ajustarCaminho(path), config);
    assert.equal(state.routes.at(-1).name, 'Home', path);
    assert.equal(folha(state), nome, path);
  }
  const gerado = getPathFromState({
    routes: [{ name: 'Home', state: { routes: [{ name: 'Painel' }], index: 0 } }],
    index: 0,
  }, config);
  assert.equal(folha(getStateFromPath(gerado, config)), 'Painel');
});

test('título da aba em pt-BR', () => {
  assert.equal(tituloDocumento('Search'), 'Preço Certo · Buscar');
  assert.equal(tituloDocumento('Painel'), 'Preço Certo · Painel');
  assert.equal(tituloDocumento('NotFound'), 'Preço Certo · Página não encontrada');
  assert.equal(tituloDocumento('Chat'), 'Preço Certo · Conversa');
});

test('rota desconhecida cai em NotFound', () => {
  assert.equal(routeName('/qualquer-coisa'), 'NotFound');
  assert.equal(routeName('/rota-inexistente-qa-404'), 'NotFound');
  assert.equal(PUBLIC_ROUTE_NAMES.has('NotFound'), true);
});
