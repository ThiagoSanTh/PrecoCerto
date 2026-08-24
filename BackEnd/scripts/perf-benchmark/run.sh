#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
RESULTS="$ROOT/results"
BASE_URL="${BASE_URL:-http://127.0.0.1:5132}"
K6_BIN="${K6_BIN:-$ROOT/bin/k6}"
mkdir -p "$RESULTS"

log() { printf '%s %s\n' "$(date -Iseconds)" "$*" | tee -a "$RESULTS/run.log"; }

need_k6() {
  if command -v "$K6_BIN" >/dev/null 2>&1; then
    return 0
  fi
  local local_bin="$ROOT/bin/k6"
  if [[ -x "$local_bin" ]]; then
    K6_BIN="$local_bin"
    return 0
  fi
  log "k6 ausente. baixando binário local..."
  mkdir -p "$ROOT/bin"
  local tmp
  tmp="$(mktemp -d)"
  curl -fsSL "https://github.com/grafana/k6/releases/download/v0.54.0/k6-v0.54.0-linux-amd64.tar.gz" -o "$tmp/k6.tgz"
  tar -xzf "$tmp/k6.tgz" -C "$tmp"
  mv "$tmp"/k6-*/k6 "$local_bin"
  chmod +x "$local_bin"
  rm -rf "$tmp"
  K6_BIN="$local_bin"
}

wait_api() {
  local i
  for i in $(seq 1 60); do
    if curl -fsS "$BASE_URL/api/health" >/dev/null 2>&1; then
      log "API ok $BASE_URL"
      return 0
    fi
    sleep 1
  done
  log "API não respondeu em 60s"
  return 1
}

curl_json() {
  curl -fsS "$1" -o "$2"
}

reset_bench() {
  curl -fsS -X POST "$BASE_URL/api/health/bench/reset" >/dev/null
}

snapshot() {
  local label="$1"
  curl -fsS "$BASE_URL/api/health/bench" -o "$RESULTS/bench-${label}.json"
  curl -fsS "$BASE_URL/api/health/ready" -o "$RESULTS/ready-${label}.json" || true
}

host_sample() {
  local label="$1"
  {
    echo "label=$label"
    echo "date=$(date -Iseconds)"
    nproc
    free -m | head -n 2
    ps -p "$(pgrep -f 'Pc.WebApi.dll' | head -n 1)" -o pid,pcpu,pmem,rss,vsz,nlwp --no-headers 2>/dev/null || true
  } > "$RESULTS/host-${label}.txt"
  local avail
  avail="$(free -m | awk 'NR==2 {print $7}')"
  if [[ "${avail:-0}" -lt 400 ]]; then
    log "STOP RAM disponível ${avail}MB < 400MB"
    echo "ram-$label" > "$RESULTS/stopped.txt"
    return 2
  fi
  return 0
}

should_stop() {
  local msg rc
  set +e
  msg="$(python3 - "$1" <<'PY'
import json, sys
path = sys.argv[1]
with open(path) as f:
    data = json.load(f)
metrics = data.get("metrics", {})

def metric(name):
    x = metrics.get(name) or {}
    inner = x.get("values") if isinstance(x.get("values"), dict) else x
    return inner or {}

failed_m = metric("http_req_failed")
failed = failed_m.get("value")
if failed is None:
    failed = failed_m.get("rate") or 0
duration = metric("http_req_duration")
p95 = duration.get("p(95)", 0) or 0
p99 = duration.get("p(99)", 0) or 0
reqs = metric("http_reqs")
rate = reqs.get("rate", 0) or 0
print(f"failed={failed:.4f} p95={p95:.1f} p99={p99:.1f} rps={rate:.2f}")
stop = float(failed) >= 0.08 or p95 >= 3000 or p99 >= 5000
sys.exit(2 if stop else 0)
PY
)"
  rc=$?
  set -e
  log "$msg"
  return "$rc"
}

run_k6() {
  local name="$1"
  local script="$2"
  shift 2
  local out="$RESULTS/${name}.json"
  log "k6 $name"
  "$K6_BIN" run --summary-export "$out" \
    -e BASE_URL="$BASE_URL" \
    -e LOJA_ID="${LOJA_ID:-}" \
    "$@" \
    "$script"
  if ! host_sample "$name"; then
    snapshot "$name"
    return 2
  fi
  snapshot "$name"
  if should_stop "$out"; then
    return 0
  fi
  log "STOP degradação em $name"
  echo "$name" > "$RESULTS/stopped.txt"
  return 2
}

need_k6
wait_api

log "inspeção DB/EXPLAIN (carga baixa)"
curl_json "$BASE_URL/api/health/bench/db" "$RESULTS/db.json" || log "db inspect falhou"
for q in feed feed_count feed_search lojas mapa ofertas_best avaliacoes_loja email_login; do
  curl -fsS "$BASE_URL/api/health/bench/explain?name=$q" -o "$RESULTS/explain-${q}.json" || log "explain $q falhou"
done

curl -sS -o "$RESULTS/produtos-missing.txt" -w "%{http_code}" "$BASE_URL/api/Produtos" > "$RESULTS/produtos-missing.status" || true
curl -sS -o "$RESULTS/ofertas-missing.txt" -w "%{http_code}" "$BASE_URL/api/Ofertas" > "$RESULTS/ofertas-missing.status" || true

LOJA_ID="$(python3 - <<PY
import json, urllib.request
try:
    with urllib.request.urlopen("$BASE_URL/api/Lojas?page=1&pageSize=1") as r:
        data = json.load(r)
    items = data.get("items") or []
    print(items[0]["id"] if items else "")
except Exception:
    print("")
PY
)"
export LOJA_ID
log "LOJA_ID=${LOJA_ID:-vazio}"

reset_bench
set +e
STOP=0
for vu in 1 5 10 25 50 100; do
  reset_bench
  run_k6 "mixed-${vu}vu" "$ROOT/mixed.js" -e VUS="$vu" -e DURATION=30s
  rc=$?
  if [[ $rc -eq 2 ]]; then STOP=1; break; fi
  if [[ $rc -ne 0 ]]; then log "k6 mixed falhou rc=$rc"; STOP=1; break; fi
done

if [[ $STOP -eq 0 ]]; then
  for ep in feed feed_search lojas mapa sugestoes avaliacoes media; do
    for vu in 10 25 50 100; do
      reset_bench
      run_k6 "ep-${ep}-${vu}vu" "$ROOT/endpoint.js" -e ENDPOINT="$ep" -e VUS="$vu" -e DURATION=30s
      rc=$?
      if [[ $rc -eq 2 ]]; then STOP=1; break 2; fi
      if [[ $rc -ne 0 ]]; then STOP=1; break 2; fi
    done
  done
fi
set -e

snapshot "final"
log "fim STOP=$STOP"
exit 0
