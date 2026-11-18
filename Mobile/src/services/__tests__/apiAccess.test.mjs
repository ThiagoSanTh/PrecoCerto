import test from 'node:test';
import assert from 'node:assert/strict';
import { deveAbrirLoginNo401, deveEncaminharAoLogin, isPedidoPublicoDeCatalogo, pageSizeSeguro } from '../apiAccess.js';

test('leitura de catálogo, mapa e busca segue sem sessão', () => {
  const publicos = [
    { method: 'get', url: '/Feed' },
    { method: 'get', url: '/Lojas' },
    { method: 'get', url: '/Lojas/mapa' },
    { method: 'get', url: '/Lojas/abc' },
    { method: 'get', url: '/Produtos' },
    { method: 'get', url: '/Produtos/abc' },
    { method: 'get', url: '/Ofertas' },
    { method: 'get', url: '/Ofertas/produto/abc' },
    { method: 'get', url: '/Weather' },
    { method: 'get', url: '/Avaliacoes/loja/abc' },
    { method: 'get', url: '/Avaliacoes/loja/abc/media' },
    { method: 'get', url: '/HistoricoPesquisa/sugestoes' },
    { method: 'get', url: '/Ofertas/abc' },
    { method: 'post', url: '/Produtos/Buscar' },
    { method: 'post', url: '/Lojas/buscar' },
    { method: 'GET', url: 'https://api.exemplo.com/api/Feed?page=1' },
  ];
  for (const config of publicos) {
    assert.equal(isPedidoPublicoDeCatalogo(config), true, config.url);
  }
});

test('compra, IA, chat, perfil e escrita continuam autenticados', () => {
  const privados = [
    { method: 'post', url: '/IA/analisar' },
    { method: 'post', url: '/IA/chat' },
    { method: 'post', url: '/Conversas/abrir' },
    { method: 'get', url: '/Conversas' },
    { method: 'get', url: '/Favoritos/cliente/1' },
    { method: 'post', url: '/Favoritos' },
    { method: 'get', url: '/Clientes/1' },
    { method: 'put', url: '/Clientes/1' },
    { method: 'post', url: '/Produtos' },
    { method: 'put', url: '/Ofertas/1' },
    { method: 'post', url: '/Lojas' },
    { method: 'post', url: '/HistoricoPesquisa' },
    { method: 'post', url: '/Avaliacoes' },
    { method: 'get', url: '/Avaliacoes/abc' },
    { method: 'get', url: '/Avaliacoes/cliente/abc' },
    { method: 'get', url: '/HistoricoPesquisa/cliente/abc/ultimos' },
    { method: 'get', url: '/Favoritos/verificar/abc' },
  ];
  for (const config of privados) {
    assert.equal(isPedidoPublicoDeCatalogo(config), false, `${config.method} ${config.url}`);
  }
});

test('só 401 de rota protegida leva ao login', () => {
  assert.equal(deveEncaminharAoLogin(401, { method: 'post', url: '/IA/analisar' }), true);
  assert.equal(deveEncaminharAoLogin(401, { method: 'get', url: '/Favoritos/verificar/1' }), true);
  assert.equal(deveEncaminharAoLogin(401, { method: 'get', url: '/Avaliacoes/abc' }), true);
  assert.equal(deveEncaminharAoLogin(401, { method: 'get', url: '/Feed' }), false);
  assert.equal(deveEncaminharAoLogin(401, { method: 'get', url: '/Lojas' }), false);
  assert.equal(deveEncaminharAoLogin(401, { method: 'get', url: '/HistoricoPesquisa/sugestoes' }), false);
  assert.equal(deveEncaminharAoLogin(401, { method: 'post', url: '/Auth/login' }), false);
  assert.equal(deveEncaminharAoLogin(403, { method: 'post', url: '/IA/analisar' }), false);
  assert.equal(deveEncaminharAoLogin(429, { method: 'get', url: '/Feed' }), false);
  assert.equal(deveEncaminharAoLogin(429, { method: 'post', url: '/IA/chat' }), false);
});

test('401 na vitrine não abre o login', () => {
  const conversas = { method: 'get', url: '/Conversas' };
  assert.equal(deveAbrirLoginNo401(401, conversas, 'Search'), false);
  assert.equal(deveAbrirLoginNo401(401, conversas, 'Home'), false);
  assert.equal(deveAbrirLoginNo401(401, conversas, 'Mensagens'), true);
  assert.equal(deveAbrirLoginNo401(401, conversas, 'ConversasLista'), true);
  assert.equal(deveAbrirLoginNo401(401, { method: 'get', url: '/Feed' }, 'Search'), false);
  assert.equal(deveAbrirLoginNo401(403, conversas, 'Mensagens'), false);
});

test('pageSize não passa de 100', () => {
  assert.equal(pageSizeSeguro(20), 20);
  assert.equal(pageSizeSeguro(100), 100);
  assert.equal(pageSizeSeguro(200), 100);
  assert.equal(pageSizeSeguro(0), 20);
});
