import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  escolherLojaDestino,
  buscarRota,
  coordenadasDaRotaOsrm,
  formatarDistanciaKm,
} from './rotaUtils.js';
import { SAQUAREMA, coordenadaDentroDeSaquarema } from './saquaremaLock.js';

const cliente = { latitude: -22.94, longitude: -42.45 };
const lojas = [
  { id: '1', lat: -22.933, lng: -42.51, nome: 'Centro' },
  { id: '2', lat: -22.936, lng: -42.47, nome: 'Itaúna' },
];

test('sem toque, a loja de destino é a mais próxima do resultado', () => {
  const destino = escolherLojaDestino(lojas, ['1', '2'], cliente, null);
  assert.equal(destino.id, '2');
});

test('toque no pin escolhe a loja de destino', () => {
  const destino = escolherLojaDestino(lojas, ['2'], cliente, '1');
  assert.equal(destino.id, '1');
});

test('busca sem resultado não inventa destino', () => {
  assert.equal(escolherLojaDestino(lojas, [], cliente, null), null);
});

test('formata distância curta em metros', () => {
  assert.equal(formatarDistanciaKm(0.42), '420 m');
  assert.equal(formatarDistanciaKm(1.25), '1,3 km');
});

test('rota OSRM vira coordenadas latitude/longitude', () => {
  const pontos = coordenadasDaRotaOsrm({
    routes: [{ geometry: { coordinates: [[-42.51, -22.93], [-42.47, -22.94]] } }],
  });
  assert.deepEqual(pontos, [
    { latitude: -22.93, longitude: -42.51 },
    { latitude: -22.94, longitude: -42.47 },
  ]);
});

test('falha de rede desenha a linha reta dentro de Saquarema', async () => {
  const rota = await buscarRota(
    { latitude: -23.55, longitude: -46.63 },
    { latitude: -22.933, longitude: -42.51 },
    async () => {
      throw new Error('offline');
    }
  );
  assert.equal(rota.length, 2);
  assert.equal(coordenadaDentroDeSaquarema(rota[0]), true);
  assert.equal(rota[0].latitude, SAQUAREMA.bounds.south);
  assert.equal(rota[1].latitude, -22.933);
});

test('resposta OSRM é usada no lugar da linha reta', async () => {
  const rota = await buscarRota(cliente, { latitude: -22.933, longitude: -42.51 }, async () => ({
    ok: true,
    json: async () => ({
      routes: [{ geometry: { coordinates: [[-42.45, -22.94], [-42.48, -22.935], [-42.51, -22.933]] } }],
    }),
  }));
  assert.equal(rota.length, 3);
  assert.equal(rota[1].longitude, -42.48);
});
