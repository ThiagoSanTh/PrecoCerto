import api from './api';
import { obterLocalizacaoAtual } from './locationService';

/**
 * Assistente Preço Certo — MotorIA v3 (`POST /IA/analisar`).
 * Histórico fica só na sessão do app.
 */
export async function enviarPerguntaIA(
  mensagem,
  {
    incluirLocalizacao = true,
    usuarioId = null,
    lojaId = null,
    modoOperacional = null,
    historico = [],
  } = {}
) {
  const body = { mensagem: String(mensagem || '').trim() };

  if (usuarioId) {
    body.usuarioId = usuarioId;
  }

  if (lojaId) {
    body.lojaIdUsuario = lojaId;
  }

  if (modoOperacional) {
    body.modoOperacional = modoOperacional;
  }

  if (Array.isArray(historico) && historico.length > 0) {
    body.historico = historico
      .filter((m) => m && m.id !== 'welcome' && m.texto)
      .slice(-6)
      .map((m) => ({
        papel: m.papel === 'user' ? 'user' : 'ia',
        texto: String(m.texto).slice(0, 500),
      }));
  }

  if (incluirLocalizacao) {
    try {
      const loc = await obterLocalizacaoAtual({ allowCached: true });
      if (loc?.latitude != null && loc?.longitude != null) {
        body.latitude = loc.latitude;
        body.longitude = loc.longitude;
      }
    } catch {
      /* sem GPS — MotorIA segue com fallback sem_localizacao */
    }
  }

  const { data } = await api.post('/IA/analisar', body);
  return data;
}

export function perguntaPrecisaLocalizacao(texto) {
  const t = String(texto || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
  return (
    t.includes('km') ||
    t.includes('distancia') ||
    t.includes('longe') ||
    t.includes('perto') ||
    t.includes('proxima') ||
    t.includes('mais perto') ||
    t.includes('quantos quilometros') ||
    t.includes('onde encontro') ||
    t.includes('onde vende') ||
    t.includes('onde tem') ||
    t.includes('mais barato') ||
    t.includes('entrega') ||
    t.includes('delivery') ||
    t.includes('loja')
  );
}

export function isAdminSession(session) {
  return String(session?.tipo ?? '').toLowerCase() === 'admin';
}
