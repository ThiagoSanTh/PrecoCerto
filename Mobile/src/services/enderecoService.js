const GOOGLE_MAPS_API_KEY = process.env.EXPO_PUBLIC_GOOGLE_MAPS_API_KEY;
const NOMINATIM_USER_AGENT = 'PrecoCerto-Mobile/1.0 (cadastro-loja)';

/**
 * Consulta o ViaCEP para preencher endereço a partir do CEP informado.
 */
export async function buscarEnderecoPorCep(cep) {
  const cepLimpo = cep.replace(/\D/g, '');

  if (cepLimpo.length !== 8) {
    throw new Error('CEP deve ter 8 dígitos.');
  }

  const response = await fetch(`https://viacep.com.br/ws/${cepLimpo}/json/`);
  const data = await response.json();

  if (data.erro) {
    throw new Error('CEP não encontrado.');
  }

  return {
    cep: cepLimpo,
    logradouro: data.logradouro || '',
    bairro: data.bairro || '',
    cidade: data.localidade || '',
    estado: data.uf || '',
  };
}

function chaveGoogleValida() {
  const k = (GOOGLE_MAPS_API_KEY || '').trim();
  if (!k || k.length < 20) return false;
  if (k.includes('<') || k.toLowerCase().includes('sua chave')) return false;
  return k.startsWith('AIza');
}

function cepLimpo(endereco) {
  return (endereco.cep || '').replace(/\D/g, '');
}

function montarTextoEndereco(endereco) {
  const partes = [
    endereco.logradouro,
    endereco.numero,
    endereco.bairro,
    endereco.cidade,
    endereco.estado,
    cepLimpo(endereco),
  ].filter(Boolean);

  return partes.join(', ');
}

function montarRua(endereco) {
  const log = (endereco.logradouro || '').trim();
  const num = (endereco.numero || '').trim();
  if (log && num) return `${log}, ${num}`;
  return log || num || '';
}

async function nominatimBuscar(params) {
  const query = new URLSearchParams();
  query.set('format', 'json');
  query.set('limit', '1');
  query.set('countrycodes', 'br');
  Object.entries(params).forEach(([key, value]) => {
    if (value != null && String(value).trim() !== '') {
      query.set(key, String(value).trim());
    }
  });

  const response = await fetch(
    `https://nominatim.openstreetmap.org/search?${query.toString()}`,
    {
      headers: {
        'User-Agent': NOMINATIM_USER_AGENT,
        Accept: 'application/json',
      },
    }
  );

  if (!response.ok) {
    return null;
  }

  const data = await response.json();
  if (!Array.isArray(data) || data.length === 0) {
    return null;
  }

  const lat = Number(data[0].lat);
  const lng = Number(data[0].lon);
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
    return null;
  }

  return { latitude: lat, longitude: lng };
}

/** BrasilAPI CEP v2 — boa cobertura no Brasil, retorna coordenadas. */
async function geocodificarPorCepBrasilApi(cep) {
  const response = await fetch(`https://brasilapi.com.br/api/cep/v2/${cep}`);
  if (!response.ok) {
    return null;
  }

  const data = await response.json();
  const lat = Number(data?.location?.coordinates?.latitude);
  const lng = Number(data?.location?.coordinates?.longitude);

  if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
    return null;
  }

  return { latitude: lat, longitude: lng };
}

async function geocodificarNominatim(endereco) {
  const cep = cepLimpo(endereco);
  const rua = montarRua(endereco);
  const cidade = (endereco.cidade || '').trim();
  const estado = (endereco.estado || '').trim().toUpperCase();

  if (cep.length === 8) {
    const porCep = await nominatimBuscar({
      postalcode: cep,
      country: 'Brazil',
    });
    if (porCep) return porCep;
  }

  if (rua && cidade) {
    const estruturado = await nominatimBuscar({
      street: rua,
      city: cidade,
      state: estado,
      postalcode: cep || undefined,
      country: 'Brazil',
    });
    if (estruturado) return estruturado;
  }

  if (cidade && estado) {
    const cidadeUf = await nominatimBuscar({
      city: cidade,
      state: estado,
      country: 'Brazil',
    });
    if (cidadeUf) return cidadeUf;
  }

  const texto = montarTextoEndereco(endereco);
  if (texto) {
    const livre = await nominatimBuscar({ q: `${texto}, Brasil` });
    if (livre) return livre;
  }

  throw new Error(
    'Não foi possível localizar este endereço no mapa. Confira CEP, cidade e UF, ou toque no mapa para marcar manualmente.'
  );
}

async function geocodificarGoogle(endereco) {
  const textoEndereco = montarTextoEndereco(endereco);

  const url = `https://maps.googleapis.com/maps/api/geocode/json?address=${encodeURIComponent(
    `${textoEndereco}, Brasil`
  )}&region=br&key=${GOOGLE_MAPS_API_KEY}`;

  const response = await fetch(url);
  const data = await response.json();

  if (data.status !== 'OK' || !data.results?.length) {
    throw new Error('Google Maps não encontrou coordenadas para este endereço.');
  }

  const location = data.results[0].geometry.location;
  return {
    latitude: location.lat,
    longitude: location.lng,
  };
}

/**
 * Converte endereço em latitude/longitude.
 * Ordem: BrasilAPI (CEP) → Google (se chave válida) → Nominatim (várias tentativas).
 */
export async function geocodificarEndereco(endereco) {
  const cep = cepLimpo(endereco);
  const temEndereco =
    montarRua(endereco) || endereco.cidade || endereco.estado || cep;

  if (!temEndereco) {
    throw new Error('Preencha o endereço antes de buscar no mapa.');
  }

  if (cep.length === 8) {
    try {
      const porCep = await geocodificarPorCepBrasilApi(cep);
      if (porCep) return porCep;
    } catch {
      /* tenta próximos provedores */
    }
  }

  if (chaveGoogleValida()) {
    try {
      return await geocodificarGoogle(endereco);
    } catch {
      /* fallback nominatim */
    }
  }

  return geocodificarNominatim(endereco);
}
