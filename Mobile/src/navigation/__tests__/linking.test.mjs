import test from 'node:test';
import assert from 'node:assert/strict';
import { getStateFromPath } from '../../../node_modules/@react-navigation/core/lib/module/getStateFromPath.js';
import { getPathFromState } from '../../../node_modules/@react-navigation/core/lib/module/getPathFromState.js';
import { linkingScreens, PUBLIC_ROUTE_NAMES } from '../linking.js';

function routeName(path) {
  const state = getStateFromPath(path, { screens: linkingScreens });
  assert.ok(state, `sem rota para ${path}`);
  return state.routes[state.routes.length - 1].name;
}

function leafRoute(state) {
  assert.ok(state, 'estado de navegação ausente');
  let atual = state.routes.at(-1);
  while (atual?.state?.routes?.length) {
    atual = atual.state.routes.at(-1);
  }
  return atual;
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

test('detalhe de produto e catálogo abertos pela vitrine preservam a screen no link e no refresh', () => {
  const cfg = { screens: linkingScreens };
  const prodState = {
    routes: [
      {
        name: 'Home',
        state: {
          routes: [
            {
              name: 'Buscar',
              state: {
                routes: [
                  { name: 'Search' },
                  { name: 'ProductDetail', params: { productId: '42' } },
                ],
              },
            },
          ],
        },
      },
    ],
  };
  const prodUrl = getPathFromState(prodState, cfg);
  const prodReload = leafRoute(getStateFromPath(prodUrl, cfg));
  assert.equal(prodReload.name, 'ProductDetail');
  assert.equal(prodReload.params?.productId, '42');

  const storeState = {
    routes: [
      {
        name: 'Home',
        state: {
          routes: [
            {
              name: 'Buscar',
              state: {
                routes: [
                  { name: 'Search' },
                  { name: 'StoreCatalog', params: { lojaId: '7' } },
                ],
              },
            },
          ],
        },
      },
    ],
  };
  const storeUrl = getPathFromState(storeState, cfg);
  const storeReload = leafRoute(getStateFromPath(storeUrl, cfg));
  assert.equal(storeReload.name, 'StoreCatalog');
  assert.equal(storeReload.params?.lojaId, '7');
});

test('abas do modo lojista e stack de mensagens direcionam para a screen correta sem cair em 404', () => {
  const cfg = { screens: linkingScreens };
  for (const [tabName, expectedPath] of [
    ['Painel', '/painel'],
    ['Produtos', '/produtos'],
    ['Loja', '/loja'],
    ['Conta', '/conta'],
  ]) {
    const url = getPathFromState({ routes: [{ name: 'Home', state: { routes: [{ name: tabName }] } }] }, cfg);
    assert.equal(url, expectedPath);
    const leaf = leafRoute(getStateFromPath(url, cfg));
    assert.equal(leaf.name, tabName);
    assert.equal(PUBLIC_ROUTE_NAMES.has(tabName), false);
  }

  const listaUrl = getPathFromState(
    {
      routes: [
        {
          name: 'Home',
          state: {
            routes: [{ name: 'Mensagens', state: { routes: [{ name: 'ConversasLista' }] } }],
          },
        },
      ],
    },
    cfg
  );
  assert.equal(listaUrl, '/mensagens');
  assert.equal(leafRoute(getStateFromPath(listaUrl, cfg)).name, 'ConversasLista');
  assert.equal(PUBLIC_ROUTE_NAMES.has('ConversasLista'), false);

  const chatUrl = getPathFromState(
    {
      routes: [
        {
          name: 'Home',
          state: {
            routes: [
              {
                name: 'Mensagens',
                state: {
                  routes: [
                    { name: 'ConversasLista' },
                    { name: 'Chat', params: { codigoAtendimento: 'abc' } },
                  ],
                },
              },
            ],
          },
        },
      ],
    },
    cfg
  );
  const chatLeaf = leafRoute(getStateFromPath(chatUrl, cfg));
  assert.equal(chatLeaf.name, 'Chat');
  assert.equal(chatLeaf.params?.codigoAtendimento, 'abc');
});

test('rota desconhecida cai em NotFound', () => {
  assert.equal(routeName('/qualquer-coisa'), 'NotFound');
  assert.equal(routeName('/rota-inexistente-qa-404'), 'NotFound');
  assert.equal(PUBLIC_ROUTE_NAMES.has('NotFound'), true);
});

