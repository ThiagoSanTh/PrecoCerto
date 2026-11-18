let mostrado = false;
let bloqueado = false;
const avisos = new Set();
const paradas = new Set();

export function lojaClaimBloqueado() {
  return bloqueado;
}

export function observarAvisoLoja(ouvinte) {
  avisos.add(ouvinte);
  return () => avisos.delete(ouvinte);
}

export function observarParadaHub(ouvinte) {
  paradas.add(ouvinte);
  return () => paradas.delete(ouvinte);
}

/** Uma vez só: para o hub e pede o aviso. Chamadas seguintes não repetem. */
export function sinalizarLojaClaimAusente() {
  if (!bloqueado) {
    bloqueado = true;
    paradas.forEach((ouvinte) => {
      try {
        ouvinte();
      } catch {
        /* um ouvinte não impede os outros */
      }
    });
  }
  if (mostrado) return false;
  mostrado = true;
  avisos.forEach((ouvinte) => {
    try {
      ouvinte();
    } catch {
      /* um ouvinte não impede os outros */
    }
  });
  return true;
}

export function resetLojaClaim() {
  mostrado = false;
  bloqueado = false;
}
