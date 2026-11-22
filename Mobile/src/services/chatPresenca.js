export const HEARTBEAT_MS = 120_000;

function instanteValido(valor) {
  const ms = Date.parse(valor);
  return Number.isFinite(ms) ? ms : null;
}

export function calcularDesde(registro) {
  if (!registro) return null;
  const check = instanteValido(registro.ultimoCheckEm);
  const fechado = instanteValido(registro.chatFechadoEm);
  if (check == null && fechado == null) return null;
  if (check == null) return new Date(fechado).toISOString();
  if (fechado == null) return new Date(check).toISOString();
  return new Date(Math.max(check, fechado)).toISOString();
}

export function createPresencaCiclo({
  abrir,
  fechar,
  heartbeatMs = HEARTBEAT_MS,
  setInterval: setIntervalFn = globalThis.setInterval,
  clearInterval: clearIntervalFn = globalThis.clearInterval,
  setTimeout: setTimeoutFn = globalThis.setTimeout,
  clearTimeout: clearTimeoutFn = globalThis.clearTimeout,
}) {
  let focoCount = 0;
  let emPrimeiroPlano = true;
  let sessaoAberta = false;
  let suporte = true;
  let heartbeatId = null;
  let fecharTimer = null;

  function deveEstarAberto() {
    return suporte && focoCount > 0 && emPrimeiroPlano;
  }

  function pararHeartbeat() {
    if (heartbeatId == null) return;
    clearIntervalFn(heartbeatId);
    heartbeatId = null;
  }

  function cancelarFecharAdiado() {
    if (fecharTimer == null) return;
    clearTimeoutFn(fecharTimer);
    fecharTimer = null;
  }

  function marcarSemSuporte() {
    suporte = false;
    sessaoAberta = false;
    pararHeartbeat();
    cancelarFecharAdiado();
  }

  function iniciarHeartbeat() {
    if (heartbeatId != null || !suporte) return;
    heartbeatId = setIntervalFn(() => {
      if (!deveEstarAberto()) return;
      Promise.resolve()
        .then(() => abrir())
        .then((ok) => {
          if (ok === false) marcarSemSuporte();
        })
        .catch(() => {});
    }, heartbeatMs);
  }

  function pedirAbrir() {
    if (!deveEstarAberto()) return;
    iniciarHeartbeat();
    if (sessaoAberta) return;
    sessaoAberta = true;
    Promise.resolve()
      .then(() => abrir())
      .then((ok) => {
        if (ok === false) marcarSemSuporte();
        else if (ok == null) {
          sessaoAberta = false;
          pararHeartbeat();
        }
      })
      .catch(() => {
        sessaoAberta = false;
      });
  }

  function enviarFechar(keepalive) {
    if (!sessaoAberta || !suporte) return;
    sessaoAberta = false;
    let resultado;
    try {
      resultado = fechar({ keepalive });
    } catch {
      return;
    }
    if (resultado === false) marcarSemSuporte();
    else if (resultado && typeof resultado.then === 'function') {
      resultado.then((ok) => {
        if (ok === false) marcarSemSuporte();
      }).catch(() => {});
    }
  }

  function agendarFechar(keepalive, adiar) {
    pararHeartbeat();
    cancelarFecharAdiado();
    if (!sessaoAberta || !suporte) return;
    if (adiar) {
      fecharTimer = setTimeoutFn(() => {
        fecharTimer = null;
        enviarFechar(false);
      }, 0);
      return;
    }
    enviarFechar(keepalive);
  }

  return {
    foco() {
      focoCount += 1;
      cancelarFecharAdiado();
      if (emPrimeiroPlano) pedirAbrir();
    },
    blur() {
      focoCount = Math.max(0, focoCount - 1);
      if (focoCount > 0) return;
      agendarFechar(false, true);
    },
    background() {
      emPrimeiroPlano = false;
      agendarFechar(false, false);
    },
    ocultar() {
      emPrimeiroPlano = false;
      agendarFechar(false, true);
    },
    foreground() {
      emPrimeiroPlano = true;
      cancelarFecharAdiado();
      if (focoCount > 0) pedirAbrir();
    },
    pagehide() {
      emPrimeiroPlano = false;
      pararHeartbeat();
      cancelarFecharAdiado();
      enviarFechar(true);
    },
    reconciliar(chatAberto) {
      if (!suporte || !chatAberto || sessaoAberta) return;
      sessaoAberta = true;
      if (focoCount > 0 && emPrimeiroPlano) iniciarHeartbeat();
      else agendarFechar(false, true);
    },
    marcarSemSuporte,
    reset() {
      const fecharAgora = sessaoAberta && suporte;
      focoCount = 0;
      emPrimeiroPlano = true;
      pararHeartbeat();
      cancelarFecharAdiado();
      sessaoAberta = false;
      suporte = true;
      if (fecharAgora) {
        try {
          fechar({ keepalive: false });
        } catch {
          // logout não pode quebrar o encerramento local
        }
      }
    },
  };
}

export async function enviarPresenca(url, token, { method = 'POST', keepalive = false } = {}) {
  if (!token || typeof fetch !== 'function') return null;
  try {
    const init = method === 'GET'
      ? { method: 'GET', headers: { Authorization: `Bearer ${token}` } }
      : {
          method: 'POST',
          keepalive,
          headers: { Authorization: `Bearer ${token}` },
        };
    const response = await fetch(url, init);
    if (response.status === 401 || response.status === 404) return false;
    if (!response.ok) return null;
    if (method === 'GET') {
      const data = await response.json();
      return {
        chatAberto: Boolean(data?.chatAberto),
        chatFechadoEm: data?.chatFechadoEm ?? null,
      };
    }
    return true;
  } catch {
    return null;
  }
}

export function enviarFecharKeepalive(url, token) {
  if (!token || typeof fetch !== 'function') return false;
  try {
    fetch(url, {
      method: 'POST',
      keepalive: true,
      headers: { Authorization: `Bearer ${token}` },
    });
    return true;
  } catch {
    return false;
  }
}
