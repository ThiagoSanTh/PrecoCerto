const RAIO_TERRA_KM = 6371;

function coordenadaValida(lat, lng) {
  const nLat = Number(lat);
  const nLng = Number(lng);
  if (!Number.isFinite(nLat) || !Number.isFinite(nLng)) return false;
  if (lat == null || lng == null || lat === '' || lng === '') return false;
  if (nLat === 0 && nLng === 0) return false;
  return true;
}

export function calcularDistanciaKm(lat1, lng1, lat2, lng2) {
  if (!coordenadaValida(lat1, lng1) || !coordenadaValida(lat2, lng2)) {
    return null;
  }

  const nLat1 = Number(lat1);
  const nLng1 = Number(lng1);
  const nLat2 = Number(lat2);
  const nLng2 = Number(lng2);

  if (nLat1 === nLat2 && nLng1 === nLng2) return 0;

  const toRad = (v) => (v * Math.PI) / 180;
  const dLat = toRad(nLat2 - nLat1);
  const dLng = toRad(nLng2 - nLng1);
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(toRad(nLat1)) * Math.cos(toRad(nLat2)) * Math.sin(dLng / 2) ** 2;

  return RAIO_TERRA_KM * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

export function formatarDistancia(km) {
  if (km == null || !Number.isFinite(Number(km)) || Number(km) < 0) return '';
  const n = Number(km);
  if (n < 1) {
    return `${Math.max(10, Math.round(n * 1000))} m`;
  }
  return `${n.toFixed(1).replace('.', ',')} km`;
}

export function estimarCaminhada(km) {
  if (km == null || !Number.isFinite(Number(km)) || Number(km) < 0) return '';
  const n = Number(km);
  if (n <= 1.6) {
    // ~4.5 km/h -> ~13.3 min por km
    const min = Math.max(1, Math.round(n * 13.5));
    return `~${min} min a pé`;
  }
  // Acima de 1.6 km estima trajeto urbano de carro (~25 km/h)
  const minCarro = Math.max(2, Math.round(n * 2.5));
  return `~${minCarro} min de carro`;
}

export function montarUrlRotaGoogleMaps({
  destinoLat,
  destinoLng,
  origemLat = null,
  origemLng = null,
} = {}) {
  if (!coordenadaValida(destinoLat, destinoLng)) return null;
  const dest = `${Number(destinoLat)},${Number(destinoLng)}`;
  let url = `https://www.google.com/maps/dir/?api=1&destination=${dest}`;
  if (coordenadaValida(origemLat, origemLng)) {
    url += `&origin=${Number(origemLat)},${Number(origemLng)}`;
  }
  return url;
}
