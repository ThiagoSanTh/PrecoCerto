const ALLOWED_IMAGE_MIME_TYPES = new Set([
  'image/jpeg',
  'image/jpg',
  'image/png',
  'image/webp',
]);

export const MAX_IMAGE_UPLOAD_BYTES = 10 * 1024 * 1024;

const SAFE_SEGMENT_RE = /^[A-Za-z0-9_-]{1,64}$/;
const SAFE_FILENAME_RE = /^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$/;
const JWT_FORMAT_RE = /^[A-Za-z0-9_-]{4,}\.[A-Za-z0-9_-]{4,}\.[A-Za-z0-9_-]{4,}$/;

export function escapeHtml(text) {
  return String(text ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

export function safeJsonForScript(obj) {
  const raw = JSON.stringify(obj);
  return (raw === undefined ? 'null' : raw)
    .replace(/</g, '\\u003c')
    .replace(/>/g, '\\u003e')
    .replace(/&/g, '\\u0026')
    .replace(/\u2028/g, '\\u2028')
    .replace(/\u2029/g, '\\u2029');
}

export function sanitizeHttpImageUrl(url) {
  if (typeof url !== 'string') return null;
  const trimmed = url.trim();
  if (!trimmed || /[\s"'<>\\]/.test(trimmed)) return null;
  try {
    const parsed = new URL(trimmed);
    if (parsed.protocol !== 'https:' && parsed.protocol !== 'http:') {
      return null;
    }
    return parsed.toString();
  } catch {
    return null;
  }
}

export function isValidJwtFormat(token) {
  if (typeof token !== 'string') return false;
  const trimmed = token.trim();
  if (trimmed.length < 16 || trimmed.length > 8192) return false;
  return JWT_FORMAT_RE.test(trimmed);
}

export function isValidSessionObject(obj) {
  if (!obj || typeof obj !== 'object' || Array.isArray(obj)) return false;
  const hasTipo = typeof obj.tipo === 'string' && obj.tipo.trim().length > 0;
  const perfil = obj.perfil;
  const hasPerfilId =
    perfil != null &&
    typeof perfil === 'object' &&
    !Array.isArray(perfil) &&
    (typeof perfil.id === 'string' || typeof perfil.id === 'number') &&
    String(perfil.id).trim().length > 0;
  return hasTipo || hasPerfilId;
}

export function parseValidSession(raw) {
  if (typeof raw !== 'string' || !raw.trim()) return null;
  try {
    const parsed = JSON.parse(raw);
    return isValidSessionObject(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

export function isLocalHostname(hostname) {
  const h = String(hostname || '').toLowerCase();
  return h === 'localhost' || h === '127.0.0.1' || h === '::1' || h === '[::1]';
}

export function normalizeApiBaseUrl(url, isDev = false) {
  if (!url || typeof url !== 'string') return null;

  let normalized = url.trim();
  if (!normalized) return null;

  normalized = normalized.replace(/^\/+/, '').replace(/\/+$/, '');
  if (!normalized) return null;

  if (!/^https?:\/\//i.test(normalized)) {
    normalized = `https://${normalized}`;
  }

  try {
    const parsed = new URL(normalized);
    if (parsed.hostname.includes('railway.internal')) {
      console.error(
        '[API] EXPO_PUBLIC_API_URL usa *.railway.internal — isso é rede privada. ' +
          'No Railway: Settings → Networking → Public Domain (algo como xxx.up.railway.app). ' +
          'Na Vercel: EXPO_PUBLIC_API_URL=https://xxx.up.railway.app/api e Redeploy.'
      );
      return null;
    }
    if (!isDev && parsed.protocol === 'http:' && !isLocalHostname(parsed.hostname)) {
      console.error(
        '[API] URL http:// não-local bloqueada em produção. Use https:// em EXPO_PUBLIC_API_URL.'
      );
      return null;
    }
    let path = parsed.pathname.replace(/\/+$/, '') || '';
    if (!path.endsWith('/api')) {
      path = `${path}/api`.replace(/\/+/g, '/');
    }
    return `${parsed.origin}${path}`;
  } catch {
    return null;
  }
}

export function sanitizeStorageId(value) {
  if (value == null) return null;
  const s = String(value).trim();
  if (!SAFE_SEGMENT_RE.test(s)) return null;
  return s;
}

export function sanitizeStorageFileName(fileName) {
  if (fileName == null || fileName === '') return null;
  const s = String(fileName).trim();
  if (
    !SAFE_FILENAME_RE.test(s) ||
    s.includes('..') ||
    s.includes('/') ||
    s.includes('\\') ||
    s.includes('%')
  ) {
    return null;
  }
  return s;
}

export function validateImageUpload({ lojaId, fileName, mimeType, byteLength } = {}) {
  const safeLojaId = sanitizeStorageId(lojaId);
  if (!safeLojaId) {
    throw new Error('Identificador de loja inválido para upload.');
  }

  const rawMime = mimeType ? String(mimeType).trim().toLowerCase() : 'image/jpeg';
  if (!ALLOWED_IMAGE_MIME_TYPES.has(rawMime)) {
    throw new Error('Tipo de imagem não permitido. Use JPEG, PNG ou WebP.');
  }

  const contentType = rawMime === 'image/jpg' ? 'image/jpeg' : rawMime;
  const ext = contentType === 'image/png' ? 'png' : contentType === 'image/webp' ? 'webp' : 'jpg';

  let safeFileName;
  if (fileName !== undefined && fileName !== null && fileName !== '') {
    safeFileName = sanitizeStorageFileName(fileName);
    if (!safeFileName) {
      throw new Error('Nome de arquivo inválido para upload.');
    }
  } else {
    safeFileName = `${Date.now()}.${ext}`;
  }

  if (byteLength !== undefined && byteLength !== null) {
    if (!Number.isFinite(byteLength) || byteLength <= 0 || byteLength > MAX_IMAGE_UPLOAD_BYTES) {
      throw new Error('Imagem vazia ou acima do limite permitido de 10 MB.');
    }
  }

  return {
    lojaId: safeLojaId,
    fileName: safeFileName,
    contentType,
    ext,
    path: `${safeLojaId}/${safeFileName}`,
  };
}

export function isValidEntityId(id) {
  if (id == null) return false;
  if (typeof id !== 'string' && typeof id !== 'number') return false;
  return SAFE_SEGMENT_RE.test(String(id).trim());
}

export function parseLojasMapMessage(rawData) {
  let data = rawData;
  if (typeof rawData === 'string') {
    if (rawData.length > 16384) return null;
    try {
      data = JSON.parse(rawData);
    } catch {
      return null;
    }
  }
  if (!data || typeof data !== 'object' || Array.isArray(data)) return null;

  switch (data.type) {
    case 'ready':
      return { type: 'ready' };
    case 'storeSelected':
    case 'store':
      return isValidEntityId(data.lojaId)
        ? { type: data.type, lojaId: String(data.lojaId).trim() }
        : null;
    case 'product':
      return isValidEntityId(data.productId)
        ? { type: 'product', productId: String(data.productId).trim() }
        : null;
    case 'routeCalculated':
      return isValidEntityId(data.lojaId)
        ? { type: 'routeCalculated', lojaId: String(data.lojaId).trim() }
        : null;
    default:
      return null;
  }
}

export function parsePickerMapMessage(rawData) {
  let data = rawData;
  if (typeof rawData === 'string') {
    if (rawData.length > 4096) return null;
    try {
      data = JSON.parse(rawData);
    } catch {
      return null;
    }
  }
  if (!data || typeof data !== 'object' || Array.isArray(data)) return null;
  if (data.type !== 'coords') return null;

  const latitude = Number(data.latitude);
  const longitude = Number(data.longitude);
  if (
    !Number.isFinite(latitude) ||
    !Number.isFinite(longitude) ||
    latitude < -90 ||
    latitude > 90 ||
    longitude < -180 ||
    longitude > 180
  ) {
    return null;
  }

  return { type: 'coords', latitude, longitude };
}
