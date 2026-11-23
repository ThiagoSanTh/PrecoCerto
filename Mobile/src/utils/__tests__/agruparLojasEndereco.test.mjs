import test from 'node:test';
import assert from 'node:assert/strict';
import {
  agruparLojasPorEndereco,
  buildLojasMapHtml,
  normalizarEnderecoMapa,
  normalizarLojaParaMapa,
  prepararDadosMapaLojas,
} from '../leafletMapHtml.js';

test('normalizarEnderecoMapa deixa minúsculas, sem acento e com espaços colapsados', () => {
  assert.equal(
    normalizarEnderecoMapa('  Rua   São   João,  10  '),
    'rua sao joao, 10'
  );
});

test('agruparLojasPorEndereco junta 3 lojas pelo endereço normalizado e mantém a isolada', () => {
  const grupos = agruparLojasPorEndereco([
    {
      id: 'a',
      lat: -22.9251,
      lng: -42.5102,
      nome: 'Padaria Sol',
      endereco: 'Rua São João, 10',
    },
    {
      id: 'b',
      lat: -22.926,
      lng: -42.511,
      nome: 'Mercado Sol',
      endereco: '  RUA   SAO   JOÃO,  10 ',
    },
    {
      id: 'c',
      lat: -22.9248,
      lng: -42.5099,
      nome: 'Farmácia Sol',
      endereco: 'rua sao joao, 10',
    },
    {
      id: 'd',
      lat: -22.93,
      lng: -42.52,
      nome: 'Hortifruti',
      endereco: 'Av. Iguaba, 50',
    },
  ]);

  assert.equal(grupos.length, 2);
  const predio = grupos.find((g) => g.lojas.length === 3);
  const isolada = grupos.find((g) => g.lojas.length === 1);
  assert.deepEqual(predio.lojas.map((l) => l.id), ['a', 'b', 'c']);
  assert.equal(isolada.lojas[0].id, 'd');
  assert.ok(Math.abs(predio.lat - ((-22.9251 + -22.926 + -22.9248) / 3)) < 1e-9);
});

test('agruparLojasPorEndereco junta coordenadas que caem na mesma 5ª casa', () => {
  const grupos = agruparLojasPorEndereco([
    { id: 'a', lat: -22.925551, lng: -42.510114, nome: 'A', endereco: 'Rua A, 1' },
    { id: 'b', lat: -22.925554, lng: -42.510113, nome: 'B', endereco: 'Rua B, 2' },
  ]);

  assert.equal(grupos.length, 1);
  assert.equal(grupos[0].lojas.length, 2);
});

test('agruparLojasPorEndereco separa endereços e coordenadas diferentes', () => {
  const grupos = agruparLojasPorEndereco([
    { id: 'a', lat: -22.92554, lng: -42.51011, nome: 'A', endereco: 'Rua A, 1' },
    { id: 'b', lat: -22.92556, lng: -42.51016, nome: 'B', endereco: 'Rua B, 2' },
  ]);

  assert.equal(grupos.length, 2);
  assert.ok(grupos.every((g) => g.lojas.length === 1));
});

test('agruparLojasPorEndereco ignora loja sem coordenada e lista vazia', () => {
  assert.deepEqual(agruparLojasPorEndereco([]), []);
  assert.deepEqual(agruparLojasPorEndereco(null), []);
  const grupos = agruparLojasPorEndereco([
    { id: 'sem', lat: null, lng: -42.5, nome: 'Sem', endereco: 'Rua A, 1' },
    { id: 'ok', lat: -22.92, lng: -42.51, nome: 'Ok', endereco: 'Rua B, 2' },
  ]);
  assert.equal(grupos.length, 1);
  assert.equal(grupos[0].lojas[0].id, 'ok');
});

test('lojas sem número no mesmo logradouro e cidade só se juntam na mesma coordenada', () => {
  function loja(id, lat, lng) {
    return normalizarLojaParaMapa({
      id,
      nomeFantasia: 'Loja ' + id,
      endereco: {
        logradouro: 'Rua 25 de Março',
        numero: '',
        cidade: 'Saquarema',
        latitude: lat,
        longitude: lng,
      },
    });
  }

  const separadas = agruparLojasPorEndereco([
    loja('a', -22.9, -42.5),
    loja('b', -22.91, -42.52),
  ]);
  assert.equal(separadas.length, 2);

  const mesmaCoord = agruparLojasPorEndereco([
    loja('c', -22.9, -42.5),
    loja('d', -22.900001, -42.500001),
  ]);
  assert.equal(mesmaCoord.length, 1);
  assert.equal(mesmaCoord[0].lojas.length, 2);
});

test('normalizarLojaParaMapa distingue o número no mesmo logradouro', () => {
  const a = normalizarLojaParaMapa({
    id: 'a',
    nomeFantasia: 'A',
    endereco: {
      logradouro: 'Rua do Sol',
      numero: '10',
      cidade: 'Saquarema',
      latitude: -22.9,
      longitude: -42.5,
    },
  });
  const b = normalizarLojaParaMapa({
    id: 'b',
    nomeFantasia: 'B',
    endereco: {
      logradouro: 'Rua do Sol',
      numero: '20',
      cidade: 'Saquarema',
      latitude: -22.91,
      longitude: -42.52,
    },
  });

  const grupos = agruparLojasPorEndereco([a, b]);
  assert.equal(grupos.length, 2);
  assert.match(a.enderecoGrupo, /10/);
  assert.match(b.enderecoGrupo, /20/);
});

test('buildLojasMapHtml embute o pin de prédio e só agrupa no cluster acima de 100 pins', () => {
  const html = buildLojasMapHtml(
    prepararDadosMapaLojas(null, [
      { id: 'a', lat: -22.92, lng: -42.51, nome: 'Padaria', endereco: 'Rua do Sol, 10', inicial: 'P' },
      { id: 'b', lat: -22.920001, lng: -42.510001, nome: 'Mercado', endereco: 'Rua do Sol, 10', inicial: 'M' },
      { id: 'c', lat: -22.94, lng: -42.53, nome: 'Hortifruti', endereco: 'Av. Iguaba, 50', inicial: 'H' },
    ])
  );

  assert.match(html, /pin-predio-selo/);
  assert.match(html, /lojas neste endereço/);
  assert.match(html, /grupos\.length > 100/);
  assert.match(html, /"ids":\["a","b"\]/);
});
