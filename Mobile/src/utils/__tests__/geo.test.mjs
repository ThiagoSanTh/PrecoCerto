import test from 'node:test';
import assert from 'node:assert/strict';
import {
  calcularDistanciaKm,
  formatarDistancia,
  estimarCaminhada,
  montarUrlRotaGoogleMaps,
} from '../geo.js';

test('calcularDistanciaKm returns null for invalid coordinates and 0 for identical points', () => {
  assert.equal(calcularDistanciaKm(null, -42.5, -22.9, -42.5), null);
  assert.equal(calcularDistanciaKm(0, 0, -22.9, -42.5), null);
  assert.equal(calcularDistanciaKm(-22.9329, -42.5114, -22.9329, -42.5114), 0);
});

test('calcularDistanciaKm computes realistic distance in Saquarema (~1.2km)', () => {
  const km = calcularDistanciaKm(-22.9329, -42.5114, -22.9221, -42.5114);
  assert.ok(km > 1.15 && km < 1.25, `Expected ~1.2km, got ${km}`);
});

test('formatarDistancia formats meters under 1km and km with comma otherwise', () => {
  assert.equal(formatarDistancia(0.35), '350 m');
  assert.equal(formatarDistancia(1.24), '1,2 km');
  assert.equal(formatarDistancia(null), '');
});

test('estimarCaminhada returns walking minutes for short distances and driving estimate for longer ones', () => {
  assert.equal(estimarCaminhada(0.35), '~5 min a pé');
  assert.equal(estimarCaminhada(2.4), '~6 min de carro');
  assert.equal(estimarCaminhada(null), '');
});

test('montarUrlRotaGoogleMaps builds Google Maps directions URL when destination coordinates are valid', () => {
  const url = montarUrlRotaGoogleMaps({
    destinoLat: -22.9329,
    destinoLng: -42.5114,
    origemLat: -22.92,
    origemLng: -42.5,
  });
  assert.ok(url.includes('https://www.google.com/maps/dir/?api=1'));
  assert.ok(url.includes('destination=-22.9329,-42.5114'));
  assert.ok(url.includes('origin=-22.92,-42.5'));
  assert.equal(montarUrlRotaGoogleMaps({ destinoLat: null, destinoLng: null }), null);
});
