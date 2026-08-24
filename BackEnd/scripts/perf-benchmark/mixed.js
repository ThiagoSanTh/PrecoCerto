import { sleep } from 'k6';
import {
  feed, feedSearch, lojas, mapa, sugestoes, health, avaliacoes, media, lojaDetalhe, ok2xx,
} from './lib.js';

export const options = {
  vus: Number(__ENV.VUS || 1),
  duration: __ENV.DURATION || '30s',
  noConnectionReuse: false,
};

export default function () {
  const roll = Math.random();
  let res;
  if (roll < 0.40) res = feed();
  else if (roll < 0.55) res = feedSearch();
  else if (roll < 0.70) res = lojas();
  else if (roll < 0.82) res = mapa();
  else if (roll < 0.88) res = sugestoes();
  else if (roll < 0.93) res = lojaDetalhe();
  else if (roll < 0.96) res = avaliacoes();
  else if (roll < 0.98) res = media();
  else res = health();

  ok2xx(res);
  sleep(0.05);
}
