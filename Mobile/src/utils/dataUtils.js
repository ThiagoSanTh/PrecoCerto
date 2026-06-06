export function formatarDataBr(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  const dia = String(d.getDate()).padStart(2, '0');
  const mes = String(d.getMonth() + 1).padStart(2, '0');
  const ano = d.getFullYear();
  return `${dia}/${mes}/${ano}`;
}

export function parseDataBr(texto) {
  const t = (texto || '').trim();
  if (!t) return null;

  const partes = t.split('/');
  if (partes.length !== 3) return null;

  const dia = parseInt(partes[0], 10);
  const mes = parseInt(partes[1], 10);
  const ano = parseInt(partes[2], 10);

  if (!Number.isFinite(dia) || !Number.isFinite(mes) || !Number.isFinite(ano)) return null;

  const d = new Date(ano, mes - 1, dia);
  if (
    d.getFullYear() !== ano ||
    d.getMonth() !== mes - 1 ||
    d.getDate() !== dia
  ) {
    return null;
  }

  return d.toISOString();
}
