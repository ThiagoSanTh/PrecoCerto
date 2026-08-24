import { formatarPrecoBrl, inicialPlaceholder } from './mapaUtils';
import { nomeProduto } from './produtoUtils';

function escapeHtml(text) {
  return String(text ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

export function prepararDadosMapaLeaflet(localizacaoCliente, produtos) {
  const marcadores = (produtos || []).map((p) => {
    const nome = nomeProduto(p);
    return {
      id: p.id,
      lat: Number(p.latitude),
      lng: Number(p.longitude),
      nome,
      preco: formatarPrecoBrl(p.preco),
      loja: p.lojaNomeFantasia || '',
      endereco: [p.logradouro, p.cidade].filter(Boolean).join(' — '),
      imagemUrl: p.imagemUrl || null,
      inicial: inicialPlaceholder(nome),
    };
  });

  const pontos = marcadores.map((m) => [m.lat, m.lng]);

  if (
    localizacaoCliente?.latitude != null &&
    localizacaoCliente?.longitude != null
  ) {
    pontos.push([
      Number(localizacaoCliente.latitude),
      Number(localizacaoCliente.longitude),
    ]);
  }

  let view = { center: [-23.5505, -46.6333], zoom: 12, bounds: null };

  if (pontos.length === 1) {
    view = { center: pontos[0], zoom: 14, bounds: null };
  } else if (pontos.length > 1) {
    let minLat = pontos[0][0];
    let maxLat = pontos[0][0];
    let minLng = pontos[0][1];
    let maxLng = pontos[0][1];
    pontos.forEach(([lat, lng]) => {
      minLat = Math.min(minLat, lat);
      maxLat = Math.max(maxLat, lat);
      minLng = Math.min(minLng, lng);
      maxLng = Math.max(maxLng, lng);
    });
    view = {
      center: [(minLat + maxLat) / 2, (minLng + maxLng) / 2],
      zoom: 12,
      bounds: [
        [minLat, minLng],
        [maxLat, maxLng],
      ],
    };
  }

  const cliente =
    localizacaoCliente?.latitude != null &&
    localizacaoCliente?.longitude != null
      ? {
          lat: Number(localizacaoCliente.latitude),
          lng: Number(localizacaoCliente.longitude),
        }
      : null;

  return { marcadores, cliente, view };
}

export function buildLeafletMapHtml(dadosMapa) {
  const payload = JSON.stringify(dadosMapa);

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no" />
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" crossorigin="" />
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" crossorigin=""></script>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    html, body, #map { width: 100%; height: 100%; }
    .pin {
      width: 44px;
      height: 44px;
      border-radius: 50%;
      border: 2px solid #2DD4BF;
      background: #14B8A6;
      color: #fff;
      font-weight: bold;
      font-size: 18px;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 2px 8px rgba(0,0,0,0.25);
    }
    .pin-img {
      background-color: #fff;
      overflow: hidden;
      border-radius: 50%;
    }
    .pin-img img {
      display: block;
      width: 40px;
      height: 40px;
      object-fit: cover;
      border-radius: 50%;
      border: 2px solid #2DD4BF;
    }
    .pin-cliente {
      width: 16px;
      height: 16px;
      border-radius: 50%;
      background: #2563eb;
      border: 3px solid #fff;
      box-shadow: 0 0 0 2px #2563eb;
    }
    .leaflet-popup-content { margin: 10px 12px; font-family: system-ui, sans-serif; font-size: 14px; line-height: 1.4; }
    .popup-title { font-weight: 700; font-size: 15px; color: #0f172a; }
    .popup-price { color: #14B8A6; font-weight: 600; margin-top: 4px; }
    .popup-meta { color: #64748b; font-size: 12px; margin-top: 4px; }
    .popup-btn {
      display: block;
      width: 100%;
      margin-top: 10px;
      padding: 8px 12px;
      background: #14B8A6;
      color: #fff;
      font-weight: 600;
      font-size: 13px;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      text-align: center;
    }
  </style>
</head>
<body>
  <div id="map"></div>
  <script>
    const DATA = ${payload};

    function postToApp(data) {
      var json = JSON.stringify(data);
      if (window.ReactNativeWebView) {
        window.ReactNativeWebView.postMessage(json);
      } else if (window.parent && window.parent !== window) {
        window.parent.postMessage(json, '*');
      }
    }

    const map = L.map('map', { zoomControl: true });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    function popupHtml(m) {
      const parts = [
        '<div class="popup-title">' + m.nome + '</div>',
        '<div class="popup-price">' + m.preco + '</div>'
      ];
      if (m.loja) parts.push('<div class="popup-meta">' + m.loja + '</div>');
      if (m.endereco) parts.push('<div class="popup-meta">' + m.endereco + '</div>');
      parts.push(
        '<button type="button" class="popup-btn" data-id="' + m.id + '">Mais informações</button>'
      );
      return parts.join('');
    }

    function pinHtml(m) {
      if (m.imagemUrl) {
        var src = encodeURI(m.imagemUrl);
        return '<div class="pin pin-img"><img src="' + src + '" alt="" width="40" height="40" /></div>';
      }
      return '<div class="pin">' + m.inicial + '</div>';
    }

    DATA.marcadores.forEach(function(m) {
      const icon = L.divIcon({
        html: pinHtml(m),
        className: '',
        iconSize: [44, 44],
        iconAnchor: [22, 44],
        popupAnchor: [0, -44]
      });
      const marker = L.marker([m.lat, m.lng], { icon: icon })
        .addTo(map)
        .bindPopup(popupHtml(m));

      marker.on('popupopen', function() {
        var btn = document.querySelector('.popup-btn[data-id="' + m.id + '"]');
        if (btn) {
          btn.onclick = function(e) {
            e.stopPropagation();
            postToApp({ type: 'product', productId: m.id });
          };
        }
      });
    });

    if (DATA.cliente) {
      const clienteIcon = L.divIcon({
        html: '<div class="pin-cliente"></div>',
        className: '',
        iconSize: [16, 16],
        iconAnchor: [8, 8]
      });
      L.marker([DATA.cliente.lat, DATA.cliente.lng], { icon: clienteIcon })
        .addTo(map)
        .bindPopup('Você está aqui');
    }

    if (DATA.view.bounds) {
      map.fitBounds(DATA.view.bounds, { padding: [48, 48], maxZoom: 16 });
    } else {
      map.setView(DATA.view.center, DATA.view.zoom);
    }
  </script>
</body>
</html>`;
}

/** Normaliza loja da API para o marcador plano usado no mapa do feed. */
export function normalizarLojaParaMapa(loja) {
  const lat =
    loja?.latitude ??
    loja?.Latitude ??
    loja?.endereco?.latitude ??
    loja?.Endereco?.Latitude;
  const lng =
    loja?.longitude ??
    loja?.Longitude ??
    loja?.endereco?.longitude ??
    loja?.Endereco?.Longitude;
  const nome = loja?.nomeFantasia ?? loja?.NomeFantasia ?? 'Loja';
  const logradouro = loja?.endereco?.logradouro ?? loja?.Endereco?.Logradouro ?? '';
  const cidade = loja?.endereco?.cidade ?? loja?.Endereco?.Cidade ?? '';

  return {
    id: loja?.id ?? loja?.Id,
    lat: lat != null && lat !== '' ? Number(lat) : null,
    lng: lng != null && lng !== '' ? Number(lng) : null,
    nome,
    endereco: [logradouro, cidade].filter(Boolean).join(' — '),
    media: loja?.mediaAvaliacoes ?? loja?.MediaAvaliacoes ?? null,
    inicial: inicialPlaceholder(nome),
  };
}

export function prepararDadosMapaLojas(localizacaoCliente, lojas) {
  const marcadores = (lojas || []).filter(
    (l) =>
      Number.isFinite(l.lat) && Number.isFinite(l.lng) && !(l.lat === 0 && l.lng === 0)
  );

  const pontos = marcadores.map((m) => [m.lat, m.lng]);

  if (
    localizacaoCliente?.latitude != null &&
    localizacaoCliente?.longitude != null
  ) {
    pontos.push([
      Number(localizacaoCliente.latitude),
      Number(localizacaoCliente.longitude),
    ]);
  }

  let view = { center: [-23.5505, -46.6333], zoom: 12, bounds: null };

  if (pontos.length === 1) {
    view = { center: pontos[0], zoom: 14, bounds: null };
  } else if (pontos.length > 1) {
    let minLat = pontos[0][0];
    let maxLat = pontos[0][0];
    let minLng = pontos[0][1];
    let maxLng = pontos[0][1];
    pontos.forEach(([lat, lng]) => {
      minLat = Math.min(minLat, lat);
      maxLat = Math.max(maxLat, lat);
      minLng = Math.min(minLng, lng);
      maxLng = Math.max(maxLng, lng);
    });
    view = {
      center: [(minLat + maxLat) / 2, (minLng + maxLng) / 2],
      zoom: 12,
      bounds: [
        [minLat, minLng],
        [maxLat, maxLng],
      ],
    };
  }

  const cliente =
    localizacaoCliente?.latitude != null &&
    localizacaoCliente?.longitude != null
      ? {
          lat: Number(localizacaoCliente.latitude),
          lng: Number(localizacaoCliente.longitude),
        }
      : null;

  return { marcadores, cliente, view };
}

/** Sanitiza strings das lojas exibidas no HTML do mapa do feed. */
export function sanitizarLojasParaHtml(marcadores) {
  return marcadores.map((m) => ({
    ...m,
    nome: escapeHtml(m.nome),
    endereco: escapeHtml(m.endereco),
    inicial: escapeHtml(m.inicial),
  }));
}

/**
 * Mapa do feed: pins por LOJA, com busca seletiva em tempo real (issue #36).
 * O app envia mensagens de destaque sem recriar o HTML:
 *   { type: 'destaques', payload: { lojaIds: null | [ids], produtosPorLoja: { id: [{ id, nome, preco }] }, imagemPinPorLoja: { id: url } } }
 * lojaIds = null restaura todos os pins; com lista, pins fora dela somem e os
 * presentes ganham destaque + popup com os produtos encontrados.
 */
export function buildLojasMapHtml(dadosMapa) {
  const payload = JSON.stringify(dadosMapa);

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no" />
  ${LEAFLET_ASSETS}
  <style>
    ${LEAFLET_BASE_CSS}
    .pin-loja-feed {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      border: 2px solid #2DD4BF;
      background: #14B8A6;
      color: #fff;
      font-weight: bold;
      font-size: 17px;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 2px 8px rgba(0,0,0,0.25);
      transition: transform 0.15s ease;
    }
    .pin-loja-feed.destaque {
      width: 52px;
      height: 52px;
      font-size: 22px;
      background: #0D9488;
      border: 3px solid #F59E0B;
      box-shadow: 0 0 0 4px rgba(245, 158, 11, 0.35), 0 2px 10px rgba(0,0,0,0.35);
    }
    .pin-loja-feed.pin-loja-feed-img {
      background: #fff;
      padding: 0;
      overflow: hidden;
    }
    .pin-loja-feed.pin-loja-feed-img img {
      display: block;
      width: 100%;
      height: 100%;
      object-fit: cover;
      border-radius: 50%;
    }
    .pin-cliente {
      width: 16px;
      height: 16px;
      border-radius: 50%;
      background: #2563eb;
      border: 3px solid #fff;
      box-shadow: 0 0 0 2px #2563eb;
    }
    .leaflet-popup-content { margin: 10px 12px; font-family: system-ui, sans-serif; font-size: 14px; line-height: 1.4; }
    .popup-title { font-weight: 700; font-size: 15px; color: #0f172a; }
    .popup-meta { color: #64748b; font-size: 12px; margin-top: 4px; }
    .popup-produto {
      display: flex;
      justify-content: space-between;
      gap: 8px;
      margin-top: 6px;
      font-size: 13px;
    }
    .popup-produto .preco { color: #14B8A6; font-weight: 600; white-space: nowrap; }
    .popup-btn {
      display: block;
      width: 100%;
      margin-top: 8px;
      padding: 7px 10px;
      background: #14B8A6;
      color: #fff;
      font-weight: 600;
      font-size: 13px;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      text-align: center;
    }
  </style>
</head>
<body>
  <div id="map"></div>
  <script>
    const DATA = ${payload};

    function esc(text) {
      return String(text == null ? '' : text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
    }

    function postToApp(data) {
      var json = JSON.stringify(data);
      if (window.ReactNativeWebView) {
        window.ReactNativeWebView.postMessage(json);
      } else if (window.parent && window.parent !== window) {
        window.parent.postMessage(json, '*');
      }
    }

    const map = L.map('map', { zoomControl: true });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    // Estado de destaque corrente (atualizado pelo app enquanto o usuário digita).
    var destaques = { lojaIds: null, produtosPorLoja: {}, imagemPinPorLoja: {} };
    var markers = {};

    function pinHtml(m, comDestaque, imagemUrl) {
      var classe = 'pin-loja-feed' + (comDestaque ? ' destaque' : '');
      if (imagemUrl) {
        var src = encodeURI(imagemUrl);
        return '<div class="' + classe + ' pin-loja-feed-img"><img src="' + src + '" alt="" /></div>';
      }
      return '<div class="' + classe + '">' + m.inicial + '</div>';
    }

    function criarIcon(m, comDestaque, imagemUrl) {
      var size = comDestaque ? 52 : 40;
      return L.divIcon({
        html: pinHtml(m, comDestaque, imagemUrl),
        className: '',
        iconSize: [size, size],
        iconAnchor: [size / 2, size],
        popupAnchor: [0, -size]
      });
    }

    function imagemPinParaLoja(lojaId, comDestaque) {
      if (!comDestaque || !destaques.imagemPinPorLoja) return null;
      return destaques.imagemPinPorLoja[lojaId] || null;
    }

    function popupHtml(m) {
      var parts = ['<div class="popup-title">' + m.nome + '</div>'];
      if (m.endereco) parts.push('<div class="popup-meta">' + m.endereco + '</div>');
      if (m.media != null) {
        parts.push('<div class="popup-meta">Avaliação: ' + Number(m.media).toFixed(1) + ' / 5</div>');
      }

      var produtos = (destaques.produtosPorLoja && destaques.produtosPorLoja[m.id]) || [];
      produtos.forEach(function(p) {
        parts.push(
          '<div class="popup-produto"><span>' + esc(p.nome) + '</span>' +
          '<span class="preco">' + esc(p.preco) + '</span></div>' +
          '<button type="button" class="popup-btn" data-id="' + esc(p.id) + '">Mais informações</button>'
        );
      });

      return parts.join('');
    }

    function ligarBotoesPopup() {
      var botoes = document.querySelectorAll('.popup-btn');
      botoes.forEach(function(btn) {
        btn.onclick = function(e) {
          e.stopPropagation();
          postToApp({ type: 'product', productId: btn.getAttribute('data-id') });
        };
      });
    }

    var usarCluster = DATA.marcadores.length > 100;
    var clusterGroup = usarCluster ? L.markerClusterGroup({ maxClusterRadius: 50 }) : null;

    DATA.marcadores.forEach(function(m) {
      var marker = L.marker([m.lat, m.lng], { icon: criarIcon(m, false, null) })
        .bindPopup(popupHtml(m));
      marker.on('popupopen', ligarBotoesPopup);
      if (usarCluster) {
        clusterGroup.addLayer(marker);
      } else {
        marker.addTo(map);
      }
      markers[m.id] = { marker: marker, dados: m, visivel: true, destaque: false };
    });

    if (usarCluster) {
      map.addLayer(clusterGroup);
    }

    function aplicarDestaques(payload) {
      destaques = {
        lojaIds: payload && payload.lojaIds ? payload.lojaIds : null,
        produtosPorLoja: (payload && payload.produtosPorLoja) || {},
        imagemPinPorLoja: (payload && payload.imagemPinPorLoja) || {}
      };

      var ids = destaques.lojaIds === null ? null : {};
      if (ids !== null) {
        destaques.lojaIds.forEach(function(id) { ids[id] = true; });
      }

      Object.keys(markers).forEach(function(id) {
        var entry = markers[id];
        var deveMostrar = ids === null || ids[id] === true;
        var deveDestacar = ids !== null && ids[id] === true;
        var imagemUrl = imagemPinParaLoja(id, deveDestacar);

        if (deveMostrar && !entry.visivel) {
          entry.marker.addTo(map);
          entry.visivel = true;
        } else if (!deveMostrar && entry.visivel) {
          entry.marker.closePopup();
          map.removeLayer(entry.marker);
          entry.visivel = false;
        }

        if (entry.visivel) {
          entry.marker.setIcon(criarIcon(entry.dados, deveDestacar, imagemUrl));
          entry.destaque = deveDestacar;
          entry.marker.setPopupContent(popupHtml(entry.dados));
        }
      });
    }

    var clienteMarker = null;
    function aplicarCliente(c) {
      if (!c || c.lat == null || c.lng == null) return;
      if (clienteMarker) {
        clienteMarker.setLatLng([c.lat, c.lng]);
        return;
      }
      var clienteIcon = L.divIcon({
        html: '<div class="pin-cliente"></div>',
        className: '',
        iconSize: [16, 16],
        iconAnchor: [8, 8]
      });
      clienteMarker = L.marker([c.lat, c.lng], { icon: clienteIcon })
        .addTo(map)
        .bindPopup('Você está aqui');
    }

    // Canal app -> mapa (injectJavaScript no nativo, postMessage no web).
    window.__onAppMessage = function(json) {
      try {
        var data = typeof json === 'string' ? JSON.parse(json) : json;
        if (data && data.type === 'destaques') aplicarDestaques(data.payload);
        if (data && data.type === 'cliente') aplicarCliente(data.payload);
      } catch (e) { /* ignore */ }
    };

    window.addEventListener('message', function(event) {
      window.__onAppMessage(event.data);
    });

    if (DATA.cliente) {
      aplicarCliente(DATA.cliente);
    }

    if (DATA.view.bounds) {
      map.fitBounds(DATA.view.bounds, { padding: [48, 48], maxZoom: 16 });
    } else {
      map.setView(DATA.view.center, DATA.view.zoom);
    }

    // Avisa o app que o mapa está pronto para receber destaques.
    postToApp({ type: 'ready' });
  </script>
</body>
</html>`;
}

/** Sanitiza strings exibidas no HTML do mapa (camada RN antes do JSON). */
export function sanitizarMarcadoresParaHtml(marcadores) {
  return marcadores.map((m) => ({
    ...m,
    nome: escapeHtml(m.nome),
    preco: escapeHtml(m.preco),
    loja: escapeHtml(m.loja),
    endereco: escapeHtml(m.endereco),
    inicial: escapeHtml(m.inicial),
  }));
}

const LEAFLET_ASSETS = `
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" crossorigin="" />
  <link rel="stylesheet" href="https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.css" crossorigin="" />
  <link rel="stylesheet" href="https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.Default.css" crossorigin="" />
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" crossorigin=""></script>
  <script src="https://unpkg.com/leaflet.markercluster@1.5.3/dist/leaflet.markercluster.js" crossorigin=""></script>
`;

const LEAFLET_BASE_CSS = `
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body, #map { width: 100%; height: 100%; }
`;

/**
 * Mapa Leaflet para escolher a localização da loja (pin arrastável + toque).
 */
export function buildLeafletPickerMapHtml({ latitude, longitude, titulo }) {
  const hasPin = latitude != null && longitude != null;
  const data = {
    marker: hasPin
      ? { lat: Number(latitude), lng: Number(longitude) }
      : null,
    view: {
      center: hasPin
        ? [Number(latitude), Number(longitude)]
        : [-23.5505, -46.6333],
      zoom: hasPin ? 16 : 12,
    },
    titulo: escapeHtml(titulo || 'Local da loja'),
  };
  const payload = JSON.stringify(data);

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no" />
  ${LEAFLET_ASSETS}
  <style>
    ${LEAFLET_BASE_CSS}
    .pin-loja {
      width: 36px;
      height: 36px;
      border-radius: 50% 50% 50% 0;
      transform: rotate(-45deg);
      background: #14B8A6;
      border: 2px solid #2DD4BF;
      box-shadow: 0 2px 8px rgba(0,0,0,0.3);
    }
    .leaflet-popup-content { margin: 8px 10px; font-family: system-ui, sans-serif; font-size: 13px; }
  </style>
</head>
<body>
  <div id="map"></div>
  <script>
    const DATA = ${payload};

    function postToApp(data) {
      var json = JSON.stringify(data);
      if (window.ReactNativeWebView) {
        window.ReactNativeWebView.postMessage(json);
      } else if (window.parent && window.parent !== window) {
        window.parent.postMessage(json, '*');
      }
    }

    const map = L.map('map', { zoomControl: true });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    map.setView(DATA.view.center, DATA.view.zoom);

    let marker = null;

    function enviarCoords(lat, lng) {
      postToApp({ type: 'coords', latitude: lat, longitude: lng });
    }

    function colocarMarcador(lat, lng, centralizar) {
      if (marker) {
        marker.setLatLng([lat, lng]);
      } else {
        const icon = L.divIcon({
          html: '<div class="pin-loja"></div>',
          className: '',
          iconSize: [36, 36],
          iconAnchor: [18, 36],
          popupAnchor: [0, -36]
        });
        marker = L.marker([lat, lng], { icon: icon, draggable: true }).addTo(map);
        marker.bindPopup(DATA.titulo);
        marker.on('dragend', function() {
          var p = marker.getLatLng();
          enviarCoords(p.lat, p.lng);
        });
      }
      if (centralizar) {
        map.setView([lat, lng], Math.max(map.getZoom(), 16));
      }
      enviarCoords(lat, lng);
    }

    map.on('click', function(e) {
      colocarMarcador(e.latlng.lat, e.latlng.lng, false);
    });

    if (DATA.marker) {
      colocarMarcador(DATA.marker.lat, DATA.marker.lng, false);
    }
  </script>
</body>
</html>`;
}
