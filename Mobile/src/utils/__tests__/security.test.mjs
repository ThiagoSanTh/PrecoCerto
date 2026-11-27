import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {
  MAPBOX_PUBLIC_TOKEN,
  buildLeafletMapHtml,
  buildLeafletPickerMapHtml,
  buildLojasMapHtml,
  prepararDadosMapaLeaflet,
  sanitizarMarcadoresParaHtml,
} from '../leafletMapHtml.js';
import {
  MAX_IMAGE_UPLOAD_BYTES,
  escapeHtml,
  isValidJwtFormat,
  normalizeApiBaseUrl,
  parseLojasMapMessage,
  parsePickerMapMessage,
  parseValidSession,
  safeJsonForScript,
  sanitizeHttpImageUrl,
  validateImageUpload,
} from '../securityUtils.js';

test('safeJsonForScript e builders de mapa neutralizam script breakout </script><script>', () => {
  const xss = '</script><script>alert(1)</script>&"\'';
  const serialized = safeJsonForScript({ nome: xss });
  assert.equal(serialized.includes('</script>'), false);
  assert.equal(serialized.includes('<script>'), false);
  assert.ok(serialized.includes('\\u003c/script\\u003e'));
  assert.ok(serialized.includes('\\u0026'));

  const html1 = buildLeafletMapHtml({
    marcadores: [{ id: '1', lat: -22.9, lng: -42.5, nome: xss, preco: 'R$ 1', inicial: 'X' }],
    view: { center: [-22.9, -42.5], zoom: 12, bounds: null },
  });
  const html2 = buildLojasMapHtml({
    marcadores: [{ id: '1', lat: -22.9, lng: -42.5, nome: xss, endereco: xss, inicial: '<' }],
    view: { center: [-22.9, -42.5], zoom: 12, bounds: null },
  });
  const html3 = buildLeafletPickerMapHtml({
    latitude: -22.9,
    longitude: -42.5,
    titulo: xss,
  });

  for (const html of [html1, html2, html3]) {
    const dataLine = html.split('\n').find((l) => l.includes('const DATA ='));
    assert.ok(dataLine, 'DATA line deve existir');
    assert.equal(dataLine.includes('</script>'), false, 'DATA nao pode fechar tag script');
    assert.equal(dataLine.includes('<script>'), false, 'DATA nao pode abrir tag script');
  }
});

test('escapeHtml e builders escapam <img onerror=...> e aspas simples/duplas', () => {
  const payload = `<img src=x onerror=alert(1)>' onmouseover='alert(2)`;
  const escaped = escapeHtml(payload);
  assert.equal(escaped.includes('<'), false);
  assert.equal(escaped.includes('>'), false);
  assert.equal(escaped.includes("'"), false);
  assert.ok(escaped.includes('&lt;img'));
  assert.ok(escaped.includes('&#39;'));

  const html = buildLeafletMapHtml({
    marcadores: [{ id: '1', lat: -22.9, lng: -42.5, nome: payload, preco: 'R$ 1', inicial: '<' }],
    view: { center: [-22.9, -42.5], zoom: 12, bounds: null },
  });
  assert.ok(html.includes('function esc(text)'));
  assert.ok(html.includes('esc(m.nome)'));
  assert.ok(html.includes('esc(m.inicial)'));
});

test('sanitizeHttpImageUrl bloqueia javascript: e data: e aceita apenas http(s)://', () => {
  assert.equal(sanitizeHttpImageUrl('javascript:alert(1)'), null);
  assert.equal(sanitizeHttpImageUrl('JAVASCRIPT:alert(1)'), null);
  assert.equal(sanitizeHttpImageUrl('data:text/html,<script>alert(1)</script>'), null);
  assert.equal(sanitizeHttpImageUrl('vbscript:msgbox(1)'), null);
  assert.equal(sanitizeHttpImageUrl('https://cdn.exemplo.com/img.png" onclick="alert(1)'), null);
  assert.equal(
    sanitizeHttpImageUrl('https://cdn.exemplo.com/foto.jpg'),
    'https://cdn.exemplo.com/foto.jpg'
  );

  const dados = prepararDadosMapaLeaflet(null, [
    { id: 'p1', nome: 'Arroz', preco: 10, latitude: -22.9, longitude: -42.5, imagemUrl: 'javascript:alert(1)' },
  ]);
  assert.equal(dados.marcadores[0].imagemUrl, null);

  const san = sanitizarMarcadoresParaHtml([
    { id: 'p1', nome: '<b>X</b>', preco: 'R$ 1', loja: 'L', endereco: 'E', inicial: '<', imagemUrl: 'data:image/svg+xml,<svg/>' },
  ]);
  assert.equal(san[0].imagemUrl, null);
  assert.equal(san[0].nome, '&lt;b&gt;X&lt;/b&gt;');
});

test('MAPBOX_PUBLIC_TOKEN nao usa segredo hardcoded e assets unpkg tem SRI sha384', () => {
  if (!process.env.EXPO_PUBLIC_MAPBOX_TOKEN) {
    assert.equal(MAPBOX_PUBLIC_TOKEN, '');
  }
  const html = buildLojasMapHtml({
    marcadores: [],
    view: { center: [-22.9, -42.5], zoom: 12, bounds: null },
  });
  assert.ok(html.includes('api.mapbox.com/styles/v1/mapbox/streets-v12/tiles'));
  assert.ok(html.includes('integrity="sha384-'));
});

test('validateImageUpload bloqueia path traversal ../../admin, MIME perigoso e excesso de bytes', () => {
  assert.throws(() => validateImageUpload({ lojaId: '../../admin', mimeType: 'image/jpeg' }));
  assert.throws(() => validateImageUpload({ lojaId: 'loja-1', fileName: '../../secret.jpg', mimeType: 'image/jpeg' }));
  assert.throws(() => validateImageUpload({ lojaId: 'loja-1', fileName: 'a/b.png', mimeType: 'image/png' }));
  assert.throws(() => validateImageUpload({ lojaId: 'loja-1', mimeType: 'image/svg+xml' }));
  assert.throws(() => validateImageUpload({ lojaId: 'loja-1', mimeType: 'text/html' }));
  assert.throws(() =>
    validateImageUpload({
      lojaId: 'loja-1',
      mimeType: 'image/png',
      byteLength: MAX_IMAGE_UPLOAD_BYTES + 1,
    })
  );

  const ok = validateImageUpload({
    lojaId: 'loja-123',
    fileName: 'produto_1.webp',
    mimeType: 'image/webp',
    byteLength: 1024,
  });
  assert.equal(ok.path, 'loja-123/produto_1.webp');
  assert.equal(ok.contentType, 'image/webp');
});

test('encodeURIComponent neutraliza SQLi e path traversal em segmentos de URL de services', () => {
  const sqli = "' OR 1=1 --";
  const traversal = '../../admin/users?role=admin';

  const encSqli = encodeURIComponent(String(sqli));
  const encTrav = encodeURIComponent(String(traversal));

  assert.equal(encSqli.includes('='), false);
  assert.equal(encSqli.includes(' '), false);
  assert.equal(encTrav.includes('/'), false);
  assert.equal(encTrav.includes('?'), false);

  const servicesDir = path.resolve('Mobile/src/services');
  const serviceFiles = [
    'avaliacaoService.js',
    'chatService.js',
    'clienteService.js',
    'consultaService.js',
    'favoritoService.js',
    'historicoService.js',
    'lojaService.js',
    'lojistaService.js',
    'ofertaService.js',
    'productService.js',
  ];
  for (const file of serviceFiles) {
    const src = fs.readFileSync(path.join(servicesDir, file), 'utf8');
    assert.ok(src.includes('encodeURIComponent('), `${file} deve usar encodeURIComponent`);
  }
});

test('isValidJwtFormat e parseValidSession validam token JWT e schema minimo de sessao', () => {
  assert.equal(isValidJwtFormat(''), false);
  assert.equal(isValidJwtFormat('not-a-jwt'), false);
  assert.equal(isValidJwtFormat('aaa.bbb'), false);
  assert.equal(isValidJwtFormat('<script>.bbb.ccc'), false);
  assert.equal(isValidJwtFormat('eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.sig_12345'), true);

  assert.equal(parseValidSession('{invalid'), null);
  assert.equal(parseValidSession('[]'), null);
  assert.equal(parseValidSession('{}'), null);
  assert.deepEqual(parseValidSession('{"tipo":"cliente","perfil":{"id":"u1"}}'), {
    tipo: 'cliente',
    perfil: { id: 'u1' },
  });
});

test('normalizeApiBaseUrl bloqueia http:// nao-local em producao (!__DEV__) e permite https://', () => {
  assert.equal(normalizeApiBaseUrl('http://api.precocerto.com/api', false), null);
  assert.equal(normalizeApiBaseUrl('http://localhost:5132/api', false), 'http://localhost:5132/api');
  assert.equal(normalizeApiBaseUrl('https://api.precocerto.com', false), 'https://api.precocerto.com/api');
  assert.equal(normalizeApiBaseUrl('https://priv.railway.internal/api', false), null);
});

test('parseLojasMapMessage e parsePickerMapMessage rejeitam payloads postMessage invalidos', () => {
  assert.equal(parseLojasMapMessage('not json'), null);
  assert.equal(parseLojasMapMessage(JSON.stringify({ type: 'eval', code: 'alert(1)' })), null);
  assert.equal(parseLojasMapMessage(JSON.stringify({ type: 'product', productId: '../../admin' })), null);
  assert.equal(parseLojasMapMessage(JSON.stringify({ type: 'store', lojaId: '<script>' })), null);
  assert.deepEqual(parseLojasMapMessage(JSON.stringify({ type: 'storeSelected', lojaId: 'loja-42' })), {
    type: 'storeSelected',
    lojaId: 'loja-42',
  });

  assert.equal(parsePickerMapMessage(JSON.stringify({ type: 'coords', latitude: 95, longitude: 0 })), null);
  assert.equal(parsePickerMapMessage(JSON.stringify({ type: 'coords', latitude: 'NaN', longitude: -42 })), null);
  assert.deepEqual(parsePickerMapMessage(JSON.stringify({ type: 'coords', latitude: -22.92, longitude: -42.51 })), {
    type: 'coords',
    latitude: -22.92,
    longitude: -42.51,
  });
});

test('iaService nao envia usuarioId spoofing e tokenStorage web usa sessionStorage', () => {
  const iaSrc = fs.readFileSync(path.resolve('Mobile/src/services/iaService.js'), 'utf8');
  assert.equal(iaSrc.includes('body.usuarioId'), false);

  const tokenSrc = fs.readFileSync(path.resolve('Mobile/src/services/tokenStorage.js'), 'utf8');
  assert.ok(tokenSrc.includes('sessionStorage.setItem(TOKEN_KEY'));
  assert.ok(tokenSrc.includes('sessionStorage.getItem(TOKEN_KEY)'));
  assert.equal(tokenSrc.includes('localStorage.setItem('), false);
});
