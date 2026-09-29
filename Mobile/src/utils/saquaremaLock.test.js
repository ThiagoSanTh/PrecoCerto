import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  SAQUAREMA,
  regiaoInicialSaquarema,
  coordenadaDentroDeSaquarema,
  travarCoordenadaEmSaquarema,
  travarRegiaoEmSaquarema,
  regiaoEscapou,
  zoomDeRegiao,
  MIN_ZOOM_SAQUAREMA,
} from './saquaremaLock.js';

test('região inicial cobre Saquarema e nada além do município', () => {
  const regiao = regiaoInicialSaquarema();
  assert.ok(coordenadaDentroDeSaquarema(regiao));
  assert.ok(regiao.latitudeDelta < SAQUAREMA.bounds.north - SAQUAREMA.bounds.south);
  assert.ok(regiao.longitudeDelta < SAQUAREMA.bounds.east - SAQUAREMA.bounds.west);
  assert.ok(zoomDeRegiao(regiao) >= MIN_ZOOM_SAQUAREMA);
});

test('ponto dentro de Saquarema permanece', () => {
  const centro = { latitude: -22.9329, longitude: -42.5096 };
  assert.deepEqual(travarCoordenadaEmSaquarema(centro), centro);
});

test('ponto fora da cidade é puxado para a borda', () => {
  const travada = travarCoordenadaEmSaquarema({ latitude: -23.5505, longitude: -46.6333 });
  assert.equal(travada.latitude, SAQUAREMA.bounds.south);
  assert.equal(travada.longitude, SAQUAREMA.bounds.west);
  assert.equal(coordenadaDentroDeSaquarema(travada), true);
});

test('GPS inválido cai no centro de Saquarema', () => {
  assert.deepEqual(travarCoordenadaEmSaquarema(null), SAQUAREMA.center);
  assert.deepEqual(travarCoordenadaEmSaquarema({ latitude: 0, longitude: 0 }), SAQUAREMA.center);
});

test('câmera em São Paulo volta para dentro de Saquarema', () => {
  const travada = travarRegiaoEmSaquarema({
    latitude: -23.55,
    longitude: -46.63,
    latitudeDelta: 0.5,
    longitudeDelta: 0.5,
  });
  const metadeLat = travada.latitudeDelta / 2;
  const metadeLng = travada.longitudeDelta / 2;
  assert.ok(travada.latitude + metadeLat <= SAQUAREMA.bounds.north + 0.0001);
  assert.ok(travada.latitude - metadeLat >= SAQUAREMA.bounds.south - 0.0001);
  assert.ok(travada.longitude + metadeLng <= SAQUAREMA.bounds.east + 0.0001);
  assert.ok(travada.longitude - metadeLng >= SAQUAREMA.bounds.west - 0.0001);
  assert.equal(regiaoEscapou({ latitude: -23.55, longitude: -46.63, latitudeDelta: 0.5, longitudeDelta: 0.5 }, travada), true);
});

test('viewport alto dentro da cidade não recentraliza', () => {
  const inicial = regiaoInicialSaquarema();
  const alta = { ...inicial, latitudeDelta: inicial.latitudeDelta * 2.2 };
  const travada = travarRegiaoEmSaquarema(alta);
  assert.equal(regiaoEscapou(alta, travada), false);
});

test('zoom arredondado dentro da cidade não dispara nova trava', () => {
  const inicial = regiaoInicialSaquarema();
  const zoom = Math.round(Math.log2(360 / inicial.longitudeDelta));
  const longitudeDelta = 360 / 2 ** zoom;
  const reportada = {
    latitude: inicial.latitude,
    longitude: inicial.longitude,
    latitudeDelta: inicial.latitudeDelta * (longitudeDelta / inicial.longitudeDelta),
    longitudeDelta,
  };
  const travada = travarRegiaoEmSaquarema(reportada);
  assert.equal(regiaoEscapou(reportada, travada), false);
});
