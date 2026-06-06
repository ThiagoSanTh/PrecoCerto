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
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" crossorigin=""></script>
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
