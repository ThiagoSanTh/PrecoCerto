import test from 'node:test';
import assert from 'node:assert/strict';
import { montarMapaBuscaPorLoja, separarCoordenadasCoincidentes } from '../mapaPinUtils.js';

test('conta ofertas da mesma loja e separa coordenadas iguais', () => {
  const produtos = [
    { id: 1, lojaId: 7, nome: 'Arroz', preco: 10 },
    { id: 2, lojaId: 7, nome: 'Feijão', preco: 8 },
    { id: 3, lojaId: 8, nome: 'Leite', preco: 5 },
  ];
  const mapa = montarMapaBuscaPorLoja(produtos, 'arroz', 4);
  assert.equal(mapa.contagemPorLoja['7'], 2);
  assert.equal(mapa.contagemPorLoja['8'], 1);

  const separados = separarCoordenadasCoincidentes([
    { id: 7, lat: -22.93, lng: -42.51 },
    { id: 8, lat: -22.93, lng: -42.51 },
  ]);
  assert.equal(separados.length, 2);
  assert.notEqual(separados[0].lat, separados[1].lat);
});
