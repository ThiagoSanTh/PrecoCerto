import { sleep } from 'k6';
import {
  feed, feedSearch, lojas, mapa, sugestoes, health, ready, avaliacoes, media, lojaDetalhe, ok2xx,
} from './lib.js';

const NAME = __ENV.ENDPOINT || 'feed';

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURATION || '30s',
};

const runners = {
  feed,
  feed_search: feedSearch,
  lojas,
  mapa,
  sugestoes,
  health,
  ready,
  avaliacoes,
  media,
  loja_id: lojaDetalhe,
};

export default function () {
  const fn = runners[NAME] || feed;
  ok2xx(fn());
  sleep(0.05);
}
