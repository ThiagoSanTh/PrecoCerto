const DEFAULT_TTL_MS = 60_000;

const store = new Map();

function chave(namespace, parts) {
  return `${namespace}:${JSON.stringify(parts)}`;
}

export function obterCache(namespace, parts, ttlMs = DEFAULT_TTL_MS) {
  const key = chave(namespace, parts);
  const entry = store.get(key);
  if (!entry) return null;
  if (Date.now() - entry.ts > ttlMs) return null;
  return entry.data;
}

export function obterCacheStale(namespace, parts) {
  const key = chave(namespace, parts);
  return store.get(key)?.data ?? null;
}

export function salvarCache(namespace, parts, data) {
  const key = chave(namespace, parts);
  store.set(key, { data, ts: Date.now() });
}

export function invalidarCache(namespace) {
  for (const key of store.keys()) {
    if (key.startsWith(`${namespace}:`)) store.delete(key);
  }
}

export function invalidarTodoCache() {
  store.clear();
}
