function temFuso(valor) {
  return /[zZ]$|[+-]\d{2}:?\d{2}$/.test(String(valor));
}

/** EnviadaEm no back é UTC. Sem offset, o browser trataria como horário local. */
export function instanteMensagem(msg) {
  const raw = msg?.enviadaEm ?? msg?.EnviadaEm;
  if (!raw) return 0;
  const texto = String(raw);
  const iso = temFuso(texto) ? texto : `${texto}Z`;
  const t = Date.parse(iso);
  return Number.isNaN(t) ? 0 : t;
}

export function ordenarMensagens(mensagens) {
  return [...(mensagens || [])].sort(
    (a, b) => instanteMensagem(a) - instanteMensagem(b)
  );
}

function inicioDoDia(ms) {
  const d = new Date(ms);
  return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
}

export function rotuloDia(ms, agora = new Date()) {
  if (!ms) return '';
  const diff = Math.round((inicioDoDia(agora) - inicioDoDia(ms)) / 86400000);
  if (diff === 0) return 'Hoje';
  if (diff === 1) return 'Ontem';
  return new Intl.DateTimeFormat('pt-BR', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(ms));
}

export function itensComSeparadorDeDia(mensagens, agora = new Date()) {
  const itens = [];
  let dia = '';
  ordenarMensagens(mensagens).forEach((msg) => {
    const ms = instanteMensagem(msg);
    const label = rotuloDia(ms, agora);
    if (label && label !== dia) {
      dia = label;
      itens.push({ tipo: 'dia', id: `dia-${label}-${inicioDoDia(ms)}`, label });
    }
    itens.push({ tipo: 'msg', id: msg.codigoPublico || `msg-${ms}`, msg });
  });
  return itens;
}

export function enterEnviaMensagem(nativeEvent) {
  if (!nativeEvent) return false;
  if (nativeEvent.isComposing || nativeEvent.keyCode === 229) return false;
  return nativeEvent.key === 'Enter' && !nativeEvent.shiftKey;
}

export function valorAtualDoInput(estado, domValue) {
  return typeof domValue === 'string' ? domValue : String(estado ?? '');
}
