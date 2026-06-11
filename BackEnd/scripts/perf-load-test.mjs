import { appendFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const base = process.argv[2] || 'http://127.0.0.1:5132/api';
const logPath = resolve(dirname(fileURLToPath(import.meta.url)), '../../debug-1a1352.log');
const sessionId = '1a1352';
const runId = 'perf-growth-v1';

const GROWTH_TARGETS = {
  feedP95Ms: 300,
  feedLoadP95Ms: 800,
  feedLoadErrors: 0,
};

function log(message, data) {
  appendFileSync(
    logPath,
    JSON.stringify({
      sessionId,
      runId,
      hypothesisId: 'GROWTH',
      location: 'perf-load-test.mjs',
      message,
      data,
      timestamp: Date.now(),
    }) + '\n'
  );
}

async function measure(name, fn, iterations = 30) {
  await fn();
  const times = [];
  let errors = 0;
  for (let i = 0; i < iterations; i++) {
    const start = performance.now();
    try {
      await fn();
      times.push(performance.now() - start);
    } catch {
      errors++;
    }
  }
  times.sort((a, b) => a - b);
  return {
    name,
    iterations,
    success: times.length,
    errors,
    avgMs: Math.round((times.reduce((a, b) => a + b, 0) / times.length) * 100) / 100,
    p50Ms: Math.round(times[Math.floor(times.length * 0.5)] * 100) / 100,
    p95Ms: Math.round(times[Math.floor(times.length * 0.95)] * 100) / 100,
    maxMs: Math.round(times[times.length - 1] * 100) / 100,
  };
}

async function concurrentLoad(path, total = 100, concurrency = 10) {
  const url = `${base}${path}`;
  let idx = 0;
  const times = [];
  let errors = 0;
  const startAll = performance.now();

  async function worker() {
    while (true) {
      const i = idx++;
      if (i >= total) break;
      const start = performance.now();
      try {
        const res = await fetch(url);
        if (!res.ok) throw new Error(String(res.status));
        await res.json();
        times.push(performance.now() - start);
      } catch {
        errors++;
      }
    }
  }

  await Promise.all(Array.from({ length: concurrency }, () => worker()));
  times.sort((a, b) => a - b);
  const durationMs = Math.round(performance.now() - startAll);
  return {
    endpoint: path,
    totalRequests: total,
    concurrency,
    durationMs,
    rps: Math.round((total / (durationMs / 1000)) * 100) / 100,
    success: times.length,
    errors,
    avgMs: times.length ? Math.round((times.reduce((a, b) => a + b, 0) / times.length) * 100) / 100 : 0,
    p50Ms: times.length ? Math.round(times[Math.floor(times.length * 0.5)] * 100) / 100 : 0,
    p95Ms: times.length ? Math.round(times[Math.floor(times.length * 0.95)] * 100) / 100 : 0,
    maxMs: times.length ? Math.round(times[times.length - 1] * 100) / 100 : 0,
  };
}

const health = await measure('Health', () => fetch(`${base}/Health`).then((r) => r.json()));
const feed = await measure('Feed', () =>
  fetch(`${base}/Feed?page=1&pageSize=20`).then((r) => r.json())
);
const produtos = await measure('ListarProdutos', () =>
  fetch(`${base}/Produtos?page=1&pageSize=20`).then((r) => r.json())
);
const busca = await measure('BuscarProdutos', () =>
  fetch(`${base}/Produtos/Buscar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nome: 'arroz', lojaId: null, page: 1, pageSize: 20 }),
  }).then((r) => r.json())
);
const loadFeed = await concurrentLoad('/Feed?page=1&pageSize=20', 100, 10);

const [feedData, prodData, lojaData] = await Promise.all([
  fetch(`${base}/Feed?page=1&pageSize=20`).then((r) => r.json()),
  fetch(`${base}/Produtos?page=1&pageSize=20`).then((r) => r.json()),
  fetch(`${base}/Lojas?page=1&pageSize=20`).then((r) => r.json()),
]);

const counts = {
  feedItems: feedData?.items?.length ?? 0,
  feedTotal: feedData?.total ?? 0,
  produtosPage: prodData?.items?.length ?? 0,
  produtosTotal: prodData?.total ?? 0,
  lojasPage: lojaData?.items?.length ?? 0,
  lojasTotal: lojaData?.total ?? 0,
};

const acceptance = {
  feedP95Ok: feed.p95Ms <= GROWTH_TARGETS.feedP95Ms,
  feedLoadP95Ok: loadFeed.p95Ms <= GROWTH_TARGETS.feedLoadP95Ms,
  feedLoadErrorsOk: loadFeed.errors <= GROWTH_TARGETS.feedLoadErrors,
};

for (const r of [health, feed, produtos, busca, loadFeed]) log('benchmark', r);
log('dataset_size', counts);
log('acceptance', { targets: GROWTH_TARGETS, results: acceptance });

console.log(JSON.stringify({ health, feed, produtos, busca, loadFeed, counts, acceptance }, null, 2));
