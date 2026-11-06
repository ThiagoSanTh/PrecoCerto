import test from 'node:test';
import assert from 'node:assert/strict';
import { buildLojasMapHtml, prepararDadosMapaLojas } from '../leafletMapHtml.js';

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

  // Verifica suporte a mensagem selecionarLoja e desenho de rota (aplicarRotaParaLoja / OSRM)
  assert.ok(html.includes('aplicarRotaParaLoja'), 'Deve conter a funcao aplicarRotaParaLoja');
  assert.ok(html.includes('router.project-osrm.org/route/v1'), 'Deve consultar rota real via OSRM com fallback');
  assert.ok(html.includes('selecionarLoja'), 'Deve escutar evento selecionarLoja do app');
  assert.ok(html.includes('limparRota'), 'Deve permitir limpar a rota ativa');
});
