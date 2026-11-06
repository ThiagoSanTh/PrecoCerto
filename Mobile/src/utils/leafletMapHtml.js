import {
  formatarPrecoBrl,
  inicialPlaceholder,
  COORDENADAS_SAQUAREMA,
  SAQUAREMA_CENTER,
  SAQUAREMA_ZOOM,
} from './mapaUtils.js';
import { nomeProduto } from './produtoUtils.js';

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

  let view = { center: SAQUAREMA_CENTER, zoom: SAQUAREMA_ZOOM, bounds: null };

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
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link rel="preconnect" href="https://fonts.googleapis.com" />
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@500;600;700;800&family=Sora:wght@600;700&display=swap" />
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" crossorigin="" />
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" crossorigin=""></script>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    html, body, #map { width: 100%; height: 100%; background: #EFF3E8; font-family: 'Manrope', system-ui, sans-serif; }
    .pin {
      width: 42px;
      height: 42px;
      border-radius: 50%;
      border: 2.5px solid #B6F035;
      background: #0E7C66;
      color: #F8FFFC;
      font-family: 'Sora', system-ui, sans-serif;
      font-weight: 700;
      font-size: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 4px 12px rgba(15, 29, 34, 0.28);
    }
    .pin-img {
      background-color: #FFFFFF;
      overflow: hidden;
      border-radius: 50%;
    }
    .pin-img img {
      display: block;
      width: 38px;
      height: 38px;
      object-fit: cover;
      border-radius: 50%;
    }
    .pin-cliente {
      width: 18px;
      height: 18px;
      border-radius: 50%;
      background: #0E7C66;
      border: 3px solid #FFFFFF;
      box-shadow: 0 0 0 4px rgba(14, 124, 102, 0.28);
    }
    .leaflet-popup-content-wrapper {
      border-radius: 16px;
      border: 1px solid #DDE5E1;
      box-shadow: 0 10px 28px rgba(15, 29, 34, 0.16);
    }
    .leaflet-popup-content { margin: 12px 14px; font-family: 'Manrope', system-ui, sans-serif; font-size: 14px; line-height: 1.4; }
    .popup-title { font-family: 'Sora', system-ui, sans-serif; font-weight: 700; font-size: 15px; color: #0F1D22; }
    .popup-price { color: #0E7C66; font-weight: 800; margin-top: 4px; font-variant-numeric: tabular-nums; }
    .popup-meta { color: #5B6E74; font-size: 12px; margin-top: 4px; }
    .popup-btn {
      display: block;
      width: 100%;
      margin-top: 10px;
      padding: 9px 12px;
      background: #0E7C66;
      color: #F8FFFC;
      font-weight: 700;
      font-size: 13px;
      border: none;
      border-radius: 10px;
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

    const map = L.map('map', {
      zoomControl: false,
      dragging: true,
      touchZoom: true,
      scrollWheelZoom: true,
      doubleClickZoom: true,
      boxZoom: true,
      keyboard: true,
    });
    L.control.zoom({ position: 'bottomright' }).addTo(map);

    var tileLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
      maxZoom: 19,
      subdomains: 'abcd',
      attribution: '&copy; OpenStreetMap &copy; CARTO'
    }).addTo(map);

    tileLayer.on('tileerror', function() {
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
      }).addTo(map);
    });

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
        return '<div class="pin pin-img"><img src="' + src + '" alt="" width="38" height="38" /></div>';
      }
      return '<div class="pin">' + m.inicial + '</div>';
    }

    DATA.marcadores.forEach(function(m) {
      const icon = L.divIcon({
        html: pinHtml(m),
        className: '',
        iconSize: [42, 42],
        iconAnchor: [21, 42],
        popupAnchor: [0, -42]
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
        iconSize: [18, 18],
        iconAnchor: [9, 9]
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

  const view = { center: SAQUAREMA_CENTER, zoom: SAQUAREMA_ZOOM, bounds: null };

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
 * Mapa do feed com identidade visual Preço Certo (#0E7C66, #0F1D22, #B6F035)
 * e motor de rotas integrado (OSRM + Polyline em camadas + resumo de trajeto).
 */
export function buildLojasMapHtml(dadosMapa) {
  const payload = JSON.stringify(dadosMapa);

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link rel="preconnect" href="https://fonts.googleapis.com" />
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@500;600;700;800&family=Sora:wght@600;700&display=swap" />
  ${LEAFLET_ASSETS}
  <style>
    ${LEAFLET_BASE_CSS}

    /* Customização da camada de tiles para harmonizar com #EFF3E8 e #0E7C66 */
    .leaflet-container {
      background: #EFF3E8;
      font-family: 'Manrope', system-ui, -apple-system, sans-serif;
    }
    .leaflet-tile-pane {
      filter: saturate(0.95) contrast(1.02);
    }

    /* Botões de Zoom no estilo SurfaceCard do Preço Certo */
    .leaflet-bar {
      border: none !important;
      box-shadow: 0 6px 20px rgba(15, 29, 34, 0.12) !important;
      border-radius: 12px !important;
      overflow: hidden;
    }
    .leaflet-bar a {
      background-color: #FFFFFF !important;
      color: #0F1D22 !important;
      border-bottom: 1px solid #DDE5E1 !important;
      width: 36px !important;
      height: 36px !important;
      line-height: 36px !important;
      font-family: 'Sora', sans-serif !important;
      font-weight: 700 !important;
      transition: background-color 0.15s ease;
    }
    .leaflet-bar a:hover {
      background-color: #E2F5EE !important;
      color: #0E7C66 !important;
    }

    /* Pins das lojas na paleta oficial (#0F1D22 ink, #0E7C66 primary, #B6F035 highlight) */
    .pin-wrap {
      position: relative;
      display: flex;
       flex-direction: column;
      align-items: center;
    }
    .pin-price-tag {
      background: #0F1D22;
      color: #B6F035;
      font-family: 'Sora', sans-serif;
      font-size: 11px;
      font-weight: 700;
      padding: 2px 7px;
      border-radius: 999px;
      border: 1.5px solid #B6F035;
      white-space: nowrap;
      margin-bottom: 3px;
      box-shadow: 0 3px 10px rgba(15, 29, 34, 0.25);
      font-variant-numeric: tabular-nums;
    }
    .pin-loja-feed {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      border: 2.5px solid #FFFFFF;
      background: #0F1D22;
      color: #F2FAF6;
      font-family: 'Sora', system-ui, sans-serif;
      font-weight: 700;
      font-size: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 4px 12px rgba(15, 29, 34, 0.26);
      transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.2s ease;
    }
    .pin-loja-feed.destaque {
      width: 46px;
      height: 46px;
      font-size: 18px;
      background: #0E7C66;
      color: #F8FFFC;
      border: 2.5px solid #B6F035;
      box-shadow: 0 0 0 4px rgba(182, 240, 53, 0.32), 0 6px 16px rgba(15, 29, 34, 0.32);
    }
    .pin-loja-feed.selecionada {
      width: 52px;
      height: 52px;
      font-size: 20px;
      background: #B6F035;
      color: #132A1C;
      border: 3px solid #0F1D22;
      transform: scale(1.06);
      box-shadow: 0 0 0 6px rgba(14, 124, 102, 0.28), 0 8px 22px rgba(15, 29, 34, 0.38);
    }
    .pin-loja-feed.pin-loja-feed-img {
      background: #FFFFFF;
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

    /* Pin do cliente com pulso radar em #0E7C66 */
    @keyframes pulseCliente {
      0% { box-shadow: 0 0 0 0 rgba(14, 124, 102, 0.55); }
      70% { box-shadow: 0 0 0 14px rgba(14, 124, 102, 0); }
      100% { box-shadow: 0 0 0 0 rgba(14, 124, 102, 0); }
    }
    .pin-cliente {
      width: 18px;
      height: 18px;
      border-radius: 50%;
      background: #0E7C66;
      border: 3px solid #FFFFFF;
      animation: pulseCliente 2s infinite;
    }

    /* Banner Flutuante da Rota Ativa (Identidade #0F1D22 + #B6F035) */
    .route-pill-banner {
      position: absolute;
      bottom: 18px;
      left: 14px;
      z-index: 900;
      display: none;
      align-items: center;
      gap: 10px;
      background: #0F1D22;
      color: #F2FAF6;
      border: 1px solid #243A42;
      border-radius: 14px;
      padding: 8px 12px;
      box-shadow: 0 8px 24px rgba(15, 29, 34, 0.25);
      max-width: calc(100% - 80px);
    }
    .route-pill-banner.ativo {
      display: inline-flex;
    }
    .route-pill-badge {
      background: #B6F035;
      color: #132A1C;
      font-family: 'Sora', sans-serif;
      font-size: 11px;
      font-weight: 800;
      padding: 3px 8px;
      border-radius: 8px;
      white-space: nowrap;
      font-variant-numeric: tabular-nums;
    }
    .route-pill-text {
      font-size: 12px;
      font-weight: 600;
      color: #C9D8DB;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .route-pill-close {
      background: #1A2D33;
      color: #C9D8DB;
      border: none;
      width: 24px;
      height: 24px;
      border-radius: 7px;
      font-size: 12px;
      font-weight: 700;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }
    .route-pill-close:hover {
      background: #DC3B32;
      color: #FFFFFF;
    }

    /* Popup Card com identidade Preço Certo */
    .leaflet-popup-content-wrapper {
      border-radius: 16px;
      border: 1px solid #DDE5E1;
      box-shadow: 0 10px 28px rgba(15, 29, 34, 0.18);
      padding: 0;
      background: #FFFFFF;
    }
    .leaflet-popup-tip {
      background: #FFFFFF;
    }
    .leaflet-popup-content {
      margin: 0;
      font-family: 'Manrope', system-ui, sans-serif;
      font-size: 14px;
      line-height: 1.4;
      min-width: 230px;
      max-width: 270px;
    }
    .map-card { padding: 13px 15px 15px; }
    .map-card-eyebrow {
      font-size: 10px;
      font-weight: 800;
      text-transform: uppercase;
      letter-spacing: 1px;
      color: #0E7C66;
      margin-bottom: 2px;
    }
    .map-card-title {
      font-family: 'Sora', system-ui, sans-serif;
      font-weight: 700;
      font-size: 15px;
      color: #0F1D22;
    }
    .map-card-produto { margin-top: 6px; font-size: 13px; font-weight: 600; color: #132A31; }
    .map-card-preco {
      margin-top: 2px;
      color: #0E7C66;
      font-family: 'Sora', system-ui, sans-serif;
      font-weight: 700;
      font-size: 17px;
      font-variant-numeric: tabular-nums;
    }
    .map-card-meta { color: #5B6E74; font-size: 12px; margin-top: 4px; font-weight: 500; }
    .map-card-empty { margin-top: 8px; font-size: 13px; color: #5B6E74; line-height: 1.35; }
    .map-card-img {
      display: block;
      width: 100%;
      height: 88px;
      object-fit: cover;
      border-radius: 10px;
      margin-top: 8px;
      background: #EEF3F0;
    }
    .popup-actions {
      display: flex;
      gap: 6px;
      margin-top: 10px;
    }
    .popup-btn {
      flex: 1;
      padding: 8px 10px;
      background: #0E7C66;
      color: #F8FFFC;
      font-family: 'Manrope', sans-serif;
      font-weight: 700;
      font-size: 12px;
      border: none;
      border-radius: 10px;
      cursor: pointer;
      text-align: center;
    }
    .popup-btn-route {
      background: #0F1D22;
      color: #B6F035;
    }
  </style>
</head>
<body>
  <div id="map"></div>
  <div id="routeBanner" class="route-pill-banner">
    <span id="routeBadge" class="route-pill-badge">Rota</span>
    <span id="routeText" class="route-pill-text">Calculando trajeto...</span>
    <button id="routeCloseBtn" type="button" class="route-pill-close" title="Limpar rota">✕</button>
  </div>
  <script>
    const DATA = ${payload};
    const SAQUAREMA_FALLBACK = [-22.92, -42.51];

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

    const map = L.map('map', {
      zoomControl: false,
      dragging: true,
      touchZoom: true,
      scrollWheelZoom: true,
      doubleClickZoom: true,
      boxZoom: true,
      keyboard: true,
    });
    L.control.zoom({ position: 'bottomright' }).addTo(map);

    var mainTiles = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
      maxZoom: 19,
      subdomains: 'abcd',
      attribution: '&copy; OpenStreetMap &copy; CARTO'
    }).addTo(map);

    var fallbackAdded = false;
    mainTiles.on('tileerror', function() {
      if (!fallbackAdded) {
        fallbackAdded = true;
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
          maxZoom: 19,
          attribution: '&copy; OpenStreetMap'
        }).addTo(map);
      }
    });

    var destaques = { lojaIds: null, produtosPorLoja: {}, imagemPinPorLoja: {}, buscaAtiva: false };
    var markers = {};
    var lojaSelecionadaId = null;
    var routeLayerGroup = L.layerGroup().addTo(map);
    var routeReqCounter = 0;

    function obterOrigemRota() {
      if (DATA.cliente && DATA.cliente.lat != null && DATA.cliente.lng != null) {
        return { lat: Number(DATA.cliente.lat), lng: Number(DATA.cliente.lng), real: true };
      }
      return { lat: SAQUAREMA_FALLBACK[0], lng: SAQUAREMA_FALLBACK[1], real: false };
    }

    function haversineKm(lat1, lng1, lat2, lng2) {
      var R = 6371;
      var dLat = (lat2 - lat1) * Math.PI / 180;
      var dLng = (lng2 - lng1) * Math.PI / 180;
      var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLng / 2) * Math.sin(dLng / 2);
      return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function formatarDistancia(m) {
      var c = DATA.cliente;
      if (!c || c.lat == null || c.lng == null) return '';
      var km = haversineKm(c.lat, c.lng, m.lat, m.lng);
      if (!isFinite(km)) return '';
      if (km < 1) return Math.round(km * 1000) + ' m';
      return km.toFixed(1).replace('.', ',') + ' km';
    }

    function primeiroPrecoLoja(lojaId) {
      var prods = (destaques.produtosPorLoja && destaques.produtosPorLoja[lojaId]) || [];
      return prods[0] ? prods[0].preco : null;
    }

    function pinHtml(m, comDestaque, imagemUrl, isSelecionada) {
      var classe = 'pin-loja-feed' +
        (comDestaque ? ' destaque' : '') +
        (isSelecionada ? ' selecionada' : '');
      var precoTag = '';
      var preco = primeiroPrecoLoja(m.id);
      if ((comDestaque || isSelecionada) && preco) {
        precoTag = '<div class="pin-price-tag">' + esc(preco) + '</div>';
      }
      var circleHtml = '';
      if (imagemUrl) {
        var src = encodeURI(imagemUrl);
        circleHtml = '<div class="' + classe + ' pin-loja-feed-img"><img src="' + src + '" alt="" width="44" height="44" /></div>';
      } else {
        circleHtml = '<div class="' + classe + '">' + m.inicial + '</div>';
      }
      return '<div class="pin-wrap">' + precoTag + circleHtml + '</div>';
    }

    function criarIcon(m, comDestaque, imagemUrl, isSelecionada) {
      var size = isSelecionada ? 52 : comDestaque ? 46 : 40;
      var hasTag = Boolean((comDestaque || isSelecionada) && primeiroPrecoLoja(m.id));
      var totalH = hasTag ? size + 22 : size;
      return L.divIcon({
        html: pinHtml(m, comDestaque, imagemUrl, isSelecionada),
        className: '',
        iconSize: [Math.max(size, 76), totalH],
        iconAnchor: [Math.max(size, 76) / 2, totalH],
        popupAnchor: [0, -totalH]
      });
    }

    function imagemPinParaLoja(lojaId, comDestaque) {
      if (!comDestaque || !destaques.imagemPinPorLoja) return null;
      return destaques.imagemPinPorLoja[lojaId] || null;
    }

    function atualizarIconesMarcadores() {
      var ids = destaques.lojaIds === null ? null : {};
      if (ids !== null) {
        destaques.lojaIds.forEach(function(id) { ids[String(id)] = true; });
      }
      Object.keys(markers).forEach(function(id) {
        var entry = markers[id];
        var deveDestacar = ids !== null && ids[String(id)] === true;
        var isSel = lojaSelecionadaId != null && String(id) === String(lojaSelecionadaId);
        var imagemUrl = imagemPinParaLoja(id, deveDestacar);
        entry.marker.setIcon(criarIcon(entry.dados, deveDestacar, imagemUrl, isSel));
        entry.marker.setZIndexOffset(isSel ? 1000 : deveDestacar ? 500 : 0);
        entry.destaque = deveDestacar;
      });
    }

    function desenharPolilinhaIdentidade(coordsLatLng, isFallback) {
      routeLayerGroup.clearLayers();
      // Camada 1: Casing externo #0F1D22 (ink)
      L.polyline(coordsLatLng, {
        color: '#0F1D22',
        weight: 9,
        opacity: 0.26,
        lineCap: 'round',
        lineJoin: 'round'
      }).addTo(routeLayerGroup);

      // Camada 2: Linha principal #0E7C66 (primary)
      L.polyline(coordsLatLng, {
        color: '#0E7C66',
        weight: 5,
        opacity: 0.96,
        dashArray: isFallback ? '10, 10' : null,
        lineCap: 'round',
        lineJoin: 'round'
      }).addTo(routeLayerGroup);

      // Camada 3: Trilha interna #B6F035 (highlight)
      L.polyline(coordsLatLng, {
        color: '#B6F035',
        weight: 2,
        opacity: 0.9,
        dashArray: '6, 12',
        lineCap: 'round',
        lineJoin: 'round'
      }).addTo(routeLayerGroup);
    }

    function atualizarBannerRota(visivel, badgeTexto, descricaoTexto) {
      var banner = document.getElementById('routeBanner');
      var badge = document.getElementById('routeBadge');
      var text = document.getElementById('routeText');
      if (!banner) return;
      if (!visivel) {
        banner.classList.remove('ativo');
        return;
      }
      badge.textContent = badgeTexto || 'Rota';
      text.textContent = descricaoTexto || '';
      banner.classList.add('ativo');
    }

    function limparRota() {
      routeReqCounter++;
      lojaSelecionadaId = null;
      routeLayerGroup.clearLayers();
      atualizarBannerRota(false);
      atualizarIconesMarcadores();
    }

    document.getElementById('routeCloseBtn').onclick = function(e) {
      e.stopPropagation();
      limparRota();
    };

    function aplicarRotaParaLoja(lojaId, opcoes) {
      var opts = opcoes || {};
      if (!lojaId || !markers[lojaId]) {
        if (opts.limparSeVazio) limparRota();
        return;
      }

      lojaSelecionadaId = String(lojaId);
      atualizarIconesMarcadores();

      var destino = markers[lojaId].dados;
      var origem = obterOrigemRota();
      var reqId = ++routeReqCounter;

      var linhaReta = [
        [origem.lat, origem.lng],
        [destino.lat, destino.lng]
      ];
      desenharPolilinhaIdentidade(linhaReta, true);

      var distRetaKm = haversineKm(origem.lat, origem.lng, destino.lat, destino.lng);
      var modoWalking = distRetaKm <= 1.8;
      var minEstimado = Math.max(1, Math.round(modoWalking ? (distRetaKm / 4.8) * 60 : (distRetaKm / 30) * 60));
      var distLabel = distRetaKm < 1
        ? Math.round(distRetaKm * 1000) + ' m'
        : distRetaKm.toFixed(1).replace('.', ',') + ' km';

      atualizarBannerRota(
        true,
        (modoWalking ? '🚶 ' : '🚗 ') + minEstimado + ' min · ' + distLabel,
        'Rota até ' + destino.nome
      );

      if (opts.ajustarBounds !== false) {
        map.fitBounds(L.latLngBounds(linhaReta), {
          paddingTopLeft: [48, 68],
          paddingBottomRight: [48, 120],
          maxZoom: 16
        });
      }

      var osrmProfile = modoWalking ? 'foot' : 'driving';
      var url =
        'https://router.project-osrm.org/route/v1/' +
        osrmProfile +
        '/' +
        origem.lng + ',' + origem.lat + ';' +
        destino.lng + ',' + destino.lat +
        '?overview=full&geometries=geojson';

      fetch(url)
        .then(function(res) { return res.json(); })
        .then(function(json) {
          if (reqId !== routeReqCounter) return;
          if (!json || !json.routes || !json.routes[0] || !json.routes[0].geometry) return;
          var route = json.routes[0];
          var coords = route.geometry.coordinates.map(function(c) {
            return [c[1], c[0]];
          });
          if (coords.length > 1) {
            desenharPolilinhaIdentidade(coords, false);
            var kmReal = route.distance / 1000;
            var dTexto = kmReal < 1
              ? Math.round(route.distance) + ' m'
              : kmReal.toFixed(1).replace('.', ',') + ' km';
            var minReal = Math.max(1, Math.round(modoWalking ? (kmReal / 4.8) * 60 : route.duration / 60));
            atualizarBannerRota(
              true,
              (modoWalking ? '🚶 ' : '🚗 ') + minReal + ' min · ' + dTexto,
              'Rota até ' + destino.nome
            );
            postToApp({
              type: 'routeCalculated',
              lojaId: String(destino.id),
              distanceKm: kmReal,
              durationMin: minReal
            });
          }
        })
        .catch(function() {
          /* Mantém polilinha tracejada de fallback já desenhada */
        });
    }

    function popupHtml(m) {
      var produtos = (destaques.produtosPorLoja && destaques.produtosPorLoja[m.id]) || [];
      var produto = produtos[0];
      var dist = formatarDistancia(m);
      var parts = [
        '<div class="map-card">',
        '<div class="map-card-eyebrow">Saquarema · RJ</div>',
        '<div class="map-card-title">' + m.nome + '</div>'
      ];

      if (produto) {
        if (produto.imagemUrl) {
          parts.push('<img class="map-card-img" src="' + encodeURI(produto.imagemUrl) + '" alt="" width="230" height="88" />');
        }
        parts.push('<div class="map-card-produto">' + esc(produto.nome) + '</div>');
        parts.push('<div class="map-card-preco">' + esc(produto.preco) + '</div>');
        if (dist) parts.push('<div class="map-card-meta">📍 ' + dist + ' de você</div>');
        parts.push(
          '<div class="popup-actions">' +
            '<button type="button" class="popup-btn popup-btn-route" data-action="route" data-loja-id="' + esc(String(m.id)) + '">Traçar rota</button>' +
            '<button type="button" class="popup-btn" data-action="product" data-id="' + esc(produto.id) + '">Ver oferta</button>' +
          '</div>'
        );
      } else if (destaques.buscaAtiva) {
        parts.push('<div class="map-card-empty">Esta loja não possui resultado para sua busca atual.</div>');
        if (dist) parts.push('<div class="map-card-meta">📍 ' + dist + '</div>');
        parts.push(
          '<div class="popup-actions">' +
            '<button type="button" class="popup-btn popup-btn-route" data-action="route" data-loja-id="' + esc(String(m.id)) + '">Traçar rota</button>' +
            '<button type="button" class="popup-btn" data-action="store" data-id="' + esc(String(m.id)) + '">Catálogo</button>' +
          '</div>'
        );
      } else {
        if (m.endereco) parts.push('<div class="map-card-meta">' + m.endereco + '</div>');
        if (dist) parts.push('<div class="map-card-meta">📍 ' + dist + '</div>');
        parts.push(
          '<div class="popup-actions">' +
            '<button type="button" class="popup-btn popup-btn-route" data-action="route" data-loja-id="' + esc(String(m.id)) + '">Traçar rota</button>' +
            '<button type="button" class="popup-btn" data-action="store" data-id="' + esc(String(m.id)) + '">Ver loja</button>' +
          '</div>'
        );
      }

      parts.push('</div>');
      return parts.join('');
    }

    function ligarBotoesPopup() {
      var botoes = document.querySelectorAll('.popup-btn');
      botoes.forEach(function(btn) {
        btn.onclick = function(e) {
          e.stopPropagation();
          var action = btn.getAttribute('data-action');
          var id = btn.getAttribute('data-id');
          var lojaId = btn.getAttribute('data-loja-id');
          if (action === 'route' && lojaId) {
            aplicarRotaParaLoja(lojaId, { ajustarBounds: true });
            postToApp({ type: 'storeSelected', lojaId: lojaId });
          } else if (action === 'store') {
            postToApp({ type: 'store', lojaId: id });
          } else if (id) {
            postToApp({ type: 'product', productId: id });
          }
        };
      });
    }

    var usarCluster = DATA.marcadores.length > 100;
    var clusterGroup = usarCluster ? L.markerClusterGroup({ maxClusterRadius: 50 }) : null;

    DATA.marcadores.forEach(function(m) {
      var marker = L.marker([m.lat, m.lng], { icon: criarIcon(m, false, null, false) })
        .bindPopup(function() { return popupHtml(m); });
      marker.on('click', function() {
        aplicarRotaParaLoja(m.id, { ajustarBounds: false });
        postToApp({ type: 'storeSelected', lojaId: String(m.id) });
      });
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
        imagemPinPorLoja: (payload && payload.imagemPinPorLoja) || {},
        buscaAtiva: Boolean(payload && payload.buscaAtiva)
      };

      atualizarIconesMarcadores();

      Object.keys(markers).forEach(function(id) {
        var entry = markers[id];
        if (entry.marker.isPopupOpen()) {
          entry.marker.setPopupContent(popupHtml(entry.dados));
          ligarBotoesPopup();
        }
      });
    }

    var clienteMarker = null;
    function aplicarCliente(c) {
      if (!c || c.lat == null || c.lng == null) return;
      DATA.cliente = c;
      if (clienteMarker) {
        clienteMarker.setLatLng([c.lat, c.lng]);
      } else {
        var clienteIcon = L.divIcon({
          html: '<div class="pin-cliente"></div>',
          className: '',
          iconSize: [18, 18],
          iconAnchor: [9, 9]
        });
        clienteMarker = L.marker([c.lat, c.lng], { icon: clienteIcon })
          .addTo(map)
          .bindPopup('Sua localização atual');
      }
      if (lojaSelecionadaId) {
        aplicarRotaParaLoja(lojaSelecionadaId, { ajustarBounds: false });
      }
    }

    function tratarSelecionarLoja(payload) {
      if (!payload || !payload.lojaId) {
        limparRota();
        return;
      }
      if (payload.trazarRota !== false) {
        aplicarRotaParaLoja(String(payload.lojaId), {
          ajustarBounds: payload.ajustarBounds !== false
        });
      } else {
        lojaSelecionadaId = String(payload.lojaId);
        atualizarIconesMarcadores();
      }
    }

    window.__onAppMessage = function(json) {
      try {
        var data = typeof json === 'string' ? JSON.parse(json) : json;
        if (!data) return;
        if (data.type === 'destaques') aplicarDestaques(data.payload);
        if (data.type === 'cliente') aplicarCliente(data.payload);
        if (data.type === 'selecionarLoja') tratarSelecionarLoja(data.payload);
        if (data.type === 'limparRota') limparRota();
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
  html, body, #map {
    width: 100%;
    height: 100%;
    overflow: hidden;
    touch-action: pan-x pan-y pinch-zoom;
  }
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
        : SAQUAREMA_CENTER,
      zoom: hasPin ? 16 : SAQUAREMA_ZOOM,
    },
    titulo: escapeHtml(titulo || 'Local da loja'),
  };
  const payload = JSON.stringify(data);

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  ${LEAFLET_ASSETS}
  <style>
    ${LEAFLET_BASE_CSS}
    .leaflet-container { background: #EFF3E8; }
    .pin-loja {
      width: 38px;
      height: 38px;
      border-radius: 50% 50% 50% 0;
      transform: rotate(-45deg);
      background: #0E7C66;
      border: 2.5px solid #B6F035;
      box-shadow: 0 4px 12px rgba(15, 29, 34, 0.32);
    }
    .leaflet-popup-content { margin: 8px 10px; font-family: system-ui, sans-serif; font-size: 13px; font-weight: 600; color: #0F1D22; }
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

    const map = L.map('map', {
      zoomControl: false,
      dragging: true,
      touchZoom: true,
      scrollWheelZoom: true,
      doubleClickZoom: true,
      boxZoom: true,
      keyboard: true,
    });
    L.control.zoom({ position: 'bottomright' }).addTo(map);

    var pickerTiles = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
      maxZoom: 19,
      subdomains: 'abcd',
      attribution: '&copy; OpenStreetMap &copy; CARTO'
    }).addTo(map);

    pickerTiles.on('tileerror', function() {
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
      }).addTo(map);
    });

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
          iconSize: [38, 38],
          iconAnchor: [19, 38],
          popupAnchor: [0, -38]
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
