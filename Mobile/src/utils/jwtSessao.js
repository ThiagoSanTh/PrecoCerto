function decodificarBase64Url(segmento) {
  const base = String(segmento).replace(/-/g, '+').replace(/_/g, '/');
  const pad = base.length % 4 === 0 ? '' : '='.repeat(4 - (base.length % 4));
  const binario = globalThis.atob(base + pad);
  const bytes = Uint8Array.from(binario, (c) => c.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}

/** JWT ilegível ou com exp vencido. Token ausente não entra aqui. */
export function tokenInutilizavel(token, agoraMs = Date.now()) {
  const partes = String(token ?? '').split('.');
  if (partes.length !== 3 || partes.some((parte) => parte.length === 0)) return true;

  try {
    const payload = JSON.parse(decodificarBase64Url(partes[1]));
    if (!payload || typeof payload !== 'object' || Array.isArray(payload)) return true;
    if (payload.exp == null) return false;
    const expMs = Number(payload.exp) * 1000;
    return !Number.isFinite(expMs) || expMs <= agoraMs;
  } catch {
    return true;
  }
}
