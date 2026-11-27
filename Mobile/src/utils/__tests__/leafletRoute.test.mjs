import test from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import {
  buildLeafletMapHtml,
  buildLeafletPickerMapHtml,
  buildLojasMapHtml,
  prepararDadosMapaLojas,
} from '../leafletMapHtml.js';

test('buildLojasMapHtml aplica a identidade visual do Preco Certo (#0E7C66, #0F1D22, #B6F035) e inclui motor de rotas OSRM + Polyline', () => {
  const dados = prepararDadosMapaLojas(
    { latitude: -22.92, longitude: -42.51 },
    [
      {
        id: 'loja-1',
        lat: -22.925,
        lng: -42.505,
        nome: 'Supermercado Sol',
        endereco: 'Av. Saquarema, 100',
        inicial: 'S',
      },
    ]
  );

  const html = buildLojasMapHtml(dados);

  // Verifica paleta original do projeto nos pins, rota e controles
  assert.ok(html.includes('#0E7C66'), 'Deve conter a cor primaria #0E7C66');
  assert.ok(html.includes('#0F1D22'), 'Deve conter a cor ink #0F1D22');
  assert.ok(html.includes('#B6F035'), 'Deve conter a cor highlight #B6F035');

  // Verifica suporte a mensagem selecionarLoja e desenho de rota (aplicarRotaParaLoja / Mapbox / OSRM)
  assert.ok(html.includes('aplicarRotaParaLoja'), 'Deve conter a funcao aplicarRotaParaLoja');
  assert.ok(html.includes('api.mapbox.com/directions/v5/mapbox'), 'Deve consultar rota profissional via Mapbox Directions');
  assert.ok(html.includes('router.project-osrm.org/route/v1'), 'Deve manter fallback para OSRM');
  assert.ok(html.includes('api.mapbox.com/styles/v1/mapbox/streets-v12/tiles'), 'Deve utilizar tiles retina Mapbox Streets v12');
  assert.ok(html.includes('selecionarLoja'), 'Deve escutar evento selecionarLoja do app');
  assert.ok(html.includes('limparRota'), 'Deve permitir limpar a rota ativa');
});

test('scripts inline gerados pelos builders do Leaflet compilam sem SyntaxError e validam safeImgUrl corretamente', () => {
  for (const [nome, builder] of [
    ['buildLeafletMapHtml', buildLeafletMapHtml],
    ['buildLeafletPickerMapHtml', buildLeafletPickerMapHtml],
    ['buildLojasMapHtml', buildLojasMapHtml],
  ]) {
    const html = builder({});
    const blocos = [...html.matchAll(/<script(?![^>]*src=)[^>]*>([\s\S]*?)<\/script>/gi)];
    assert.ok(blocos.length > 0, `${nome} deve gerar ao menos um bloco <script>`);

    for (const [idx, match] of blocos.entries()) {
      assert.doesNotThrow(
        () => new vm.Script(match[1], { filename: `${nome}_${idx}.js` }),
        `${nome} bloco #${idx} possui erro de sintaxe JS`
      );
    }

    if (nome !== 'buildLeafletPickerMapHtml') {
      const codigo = blocos[0][1];
      const matchFn = codigo.match(/function safeImgUrl\(url\)\s*\{[\s\S]*?\n\s*\}/);
      assert.ok(matchFn, `${nome} deve definir safeImgUrl`);
      const ctx = vm.createContext({});
      vm.runInContext(`${matchFn[0]}; this.safeImgUrl = safeImgUrl;`, ctx);
      assert.equal(
        ctx.safeImgUrl('https://cdn.exemplo.com/imagens/produto.webp'),
        'https://cdn.exemplo.com/imagens/produto.webp',
        `${nome}: safeImgUrl deve aceitar URL https:// válida com letra s`
      );
      assert.equal(ctx.safeImgUrl('javascript:alert(1)'), null);
      assert.equal(ctx.safeImgUrl('https://malicioso.com/a" onerror="alert(1)'), null);
      assert.equal(ctx.safeImgUrl('https://malicioso.com/a\\b'), null);
    }
  }
});
