import test from 'node:test';
import assert from 'node:assert/strict';
import { getStateFromPath } from '../../../node_modules/@react-navigation/core/lib/module/getStateFromPath.js';
import { linkingScreens, PUBLIC_ROUTE_NAMES } from '../linking.js';

function routeName(path) {
  const state = getStateFromPath(path, { screens: linkingScreens });
  assert.ok(state, `sem rota para ${path}`);
  return state.routes[state.routes.length - 1].name;
}

test('caminhos públicos legíveis e aliases', () => {
  assert.equal(routeName('/'), 'Login');
  assert.equal(routeName('/login'), 'Login');
  assert.equal(routeName('/Login'), 'Login');
  assert.equal(routeName('/cadastro'), 'Cadastro');
  assert.equal(routeName('/Cadastro'), 'Cadastro');
  assert.equal(routeName('/esqueci-senha'), 'EsqueciSenha');
  assert.equal(routeName('/EsqueciSenha'), 'EsqueciSenha');
  for (const name of ['Login', 'Cadastro', 'EsqueciSenha']) {
    assert.equal(PUBLIC_ROUTE_NAMES.has(name), true);
  }
});

test('telas autenticadas não são públicas', () => {
  const paths = ['/home', '/Home', '/Buscar', '/Search', '/criar-loja', '/CreateStore', '/chat', '/assistente'];
  for (const path of paths) {
    const name = routeName(path);
    assert.equal(PUBLIC_ROUTE_NAMES.has(name), false, path);
  }
});

test('rota desconhecida cai em NotFound', () => {
  assert.equal(routeName('/qualquer-coisa'), 'NotFound');
  assert.equal(routeName('/rota-inexistente-qa-404'), 'NotFound');
  assert.equal(PUBLIC_ROUTE_NAMES.has('NotFound'), true);
});
