import { forwardRef, useContext, useEffect, useImperativeHandle, useRef, useState, createContext, Children, isValidElement } from 'react';
import { View, StyleSheet } from 'react-native';
import { zoomDeRegiao } from '../utils/saquaremaLock';

/**
 * Web: o pacote react-native-maps 1.20 não desenha MapView/Marker no browser
 * (MapView.web é UnimplementedView e Marker puxa código nativo).
 * Este módulo expõe a mesma API (MapView, Marker, Polyline, Callout)
 * para o Metro resolver `react-native-maps` na plataforma web.
 */

const MapContext = createContext(null);

let leafletPromise = null;

function ensureLeaflet() {
  if (typeof window === 'undefined') return Promise.reject(new Error('sem window'));
  if (window.L?.map) return Promise.resolve(window.L);
  if (leafletPromise) return leafletPromise;

  leafletPromise = new Promise((resolve, reject) => {
    const cssId = 'leaflet-css-rn-maps';
    if (!document.getElementById(cssId)) {
      const link = document.createElement('link');
      link.id = cssId;
      link.rel = 'stylesheet';
      link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
      document.head.appendChild(link);
    }
    if (!document.getElementById('pc-pin-style')) {
      const style = document.createElement('style');
      style.id = 'pc-pin-style';
      style.textContent = '.leaflet-marker-icon.pc-pin{background:transparent;border:none;}';
      document.head.appendChild(style);
    }
    const script = document.createElement('script');
    script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
    script.async = true;
    script.onload = () => resolve(window.L);
    script.onerror = () => {
      leafletPromise = null;
      reject(new Error('Falha ao carregar o mapa'));
    };
    document.head.appendChild(script);
  });

  return leafletPromise;
}

function escapeHtml(text) {
  return String(text ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function pinIcon(L, color) {
  const safe = /^#[0-9A-Fa-f]{3,8}$/.test(color) ? color : '#14B8A6';
  const html = `<div style="width:22px;height:22px;margin:3px;border-radius:50%;background:${safe};border:3px solid #fff;box-shadow:0 0 0 2px ${safe}"></div>`;
  return L.divIcon({
    className: 'pc-pin',
    html,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
    popupAnchor: [0, -16],
  });
}

function popupHtml({ title, description, comAcao }) {
  const partes = ['<div style="font-family:system-ui,sans-serif;min-width:180px;max-width:240px">'];
  if (title) {
    partes.push(`<strong style="color:#0f172a">${escapeHtml(title)}</strong>`);
  }
  if (description) {
    partes.push(
      `<div style="margin-top:4px;color:#334155;white-space:pre-line;font-size:13px">${escapeHtml(description)}</div>`
    );
  }
  if (comAcao) {
    partes.push(
      '<button type="button" data-map-action="1" style="margin-top:8px;width:100%;padding:8px 10px;background:#14B8A6;color:#fff;border:none;border-radius:8px;font-weight:600;cursor:pointer">Abrir</button>'
    );
  }
  partes.push('</div>');
  return partes.join('');
}

function aplicarLimites(map, ne, sw) {
  const L = window.L;
  if (!map || !L || !ne || !sw) return;
  const bounds = L.latLngBounds([sw.latitude, sw.longitude], [ne.latitude, ne.longitude]);
  map.setMaxBounds(bounds);
  map.options.maxBoundsViscosity = 1;
  const fitZoom = map.getBoundsZoom(bounds, false);
  if (Number.isFinite(fitZoom)) {
    map.setMinZoom(Math.min(map.getMaxZoom(), fitZoom));
  }
}

export const MapView = forwardRef(function MapView(
  {
    style,
    initialRegion,
    region,
    onRegionChangeComplete,
    onPress,
    onMapReady,
    minZoomLevel,
    maxZoomLevel,
    scrollEnabled = true,
    zoomEnabled = true,
    children,
  },
  ref
) {
  const containerRef = useRef(null);
  const mapRef = useRef(null);
  const pendentesRef = useRef([]);
  const onPressRef = useRef(onPress);
  const onRegionRef = useRef(onRegionChangeComplete);
  const onReadyRef = useRef(onMapReady);
  const [mapInstance, setMapInstance] = useState(null);

  onPressRef.current = onPress;
  onRegionRef.current = onRegionChangeComplete;
  onReadyRef.current = onMapReady;

  useImperativeHandle(ref, () => ({
    setMapBoundaries(ne, sw) {
      const map = mapRef.current;
      if (!map) {
        pendentesRef.current.push(() => aplicarLimites(mapRef.current, ne, sw));
        return;
      }
      aplicarLimites(map, ne, sw);
    },
    animateToRegion(proxima, duration = 250) {
      const map = mapRef.current;
      const ir = () => {
        const atual = mapRef.current;
        if (!atual || !proxima) return;
        atual.flyTo([proxima.latitude, proxima.longitude], zoomDeRegiao(proxima), {
          duration: Math.max(duration, 0) / 1000,
        });
      };
      if (!map) {
        pendentesRef.current.push(ir);
        return;
      }
      ir();
    },
    fitToCoordinates(coords) {
      const map = mapRef.current;
      if (!map || !window.L || !coords?.length) return;
      const bounds = window.L.latLngBounds(coords.map((c) => [c.latitude, c.longitude]));
      map.fitBounds(bounds, { padding: [48, 48], maxZoom: maxZoomLevel || 18 });
    },
  }));

  useEffect(() => {
    let cancelado = false;
    let mapa = null;

    ensureLeaflet()
      .then((L) => {
        if (cancelado || !containerRef.current || mapRef.current) return;
        const inicio = region || initialRegion || {
          latitude: -22.92,
          longitude: -42.53,
          latitudeDelta: 0.16,
          longitudeDelta: 0.23,
        };
        mapa = L.map(containerRef.current, {
          zoomControl: true,
          minZoom: minZoomLevel ?? 10,
          maxZoom: maxZoomLevel ?? 18,
          scrollWheelZoom: zoomEnabled !== false,
          dragging: scrollEnabled !== false,
        }).setView([inicio.latitude, inicio.longitude], zoomDeRegiao(inicio));

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
          maxZoom: 19,
          attribution: '&copy; OpenStreetMap',
        }).addTo(mapa);

        mapa.on('click', (evento) => {
          onPressRef.current?.({
            nativeEvent: {
              coordinate: { latitude: evento.latlng.lat, longitude: evento.latlng.lng },
            },
          });
        });
        mapa.on('moveend', () => {
          if (!mapa.getSize()?.x || !mapa.getSize()?.y) return;
          const centro = mapa.getCenter();
          const limites = mapa.getBounds();
          onRegionRef.current?.({
            latitude: centro.lat,
            longitude: centro.lng,
            latitudeDelta: limites.getNorth() - limites.getSouth(),
            longitudeDelta: limites.getEast() - limites.getWest(),
          });
        });

        mapRef.current = mapa;
        const fila = pendentesRef.current.splice(0);
        fila.forEach((fn) => fn());
        setMapInstance(mapa);
        setTimeout(() => mapa.invalidateSize(), 50);
        onReadyRef.current?.();
      })
      .catch(() => {});

    const aoRedimensionar = () => mapRef.current?.invalidateSize();
    window.addEventListener('resize', aoRedimensionar);

    return () => {
      cancelado = true;
      window.removeEventListener('resize', aoRedimensionar);
      mapa?.remove();
      mapRef.current = null;
      setMapInstance(null);
    };
    // O mapa é criado uma vez; região seguinte entra por animateToRegion.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <MapContext.Provider value={mapInstance}>
      <View style={[styles.frame, style]}>
        <div ref={containerRef} style={canvasStyle} />
        {children}
      </View>
    </MapContext.Provider>
  );
});

MapView.displayName = 'MapView';

export function Callout() {
  return null;
}
Callout.displayName = 'Callout';

function acharCallout(children) {
  let achado = null;
  Children.forEach(children, (child) => {
    if (!isValidElement(child)) return;
    if (child.type === Callout || child.type?.displayName === 'Callout') achado = child;
  });
  return achado;
}

export function Marker({
  coordinate,
  title,
  description,
  pinColor = '#14B8A6',
  onPress,
  draggable = false,
  onDragEnd,
  children,
}) {
  const map = useContext(MapContext);
  const markerRef = useRef(null);
  const onPressRef = useRef(onPress);
  const onDragEndRef = useRef(onDragEnd);
  const calloutRef = useRef(null);
  onPressRef.current = onPress;
  onDragEndRef.current = onDragEnd;
  const callout = acharCallout(children);
  calloutRef.current = callout;
  const temAcao = Boolean(callout?.props?.onPress);
  const propsRef = useRef({});
  propsRef.current = { coordinate, title, description, pinColor, draggable, temAcao };

  function aplicarMarcador(marker) {
    const atual = propsRef.current;
    if (!marker || !window.L || !atual.coordinate) return;
    marker.setLatLng([atual.coordinate.latitude, atual.coordinate.longitude]);
    marker.setIcon(pinIcon(window.L, atual.pinColor || '#14B8A6'));
    if (atual.draggable) marker.dragging?.enable();
    else marker.dragging?.disable();
    const estavaAberto = marker.isPopupOpen();
    marker.bindPopup(
      popupHtml({
        title: atual.title,
        description: atual.description,
        comAcao: atual.temAcao,
      })
    );
    if (estavaAberto) marker.openPopup();
  }

  useEffect(() => {
    if (!map || !window.L || !coordinate) return undefined;
    const L = window.L;
    const marker = L.marker([coordinate.latitude, coordinate.longitude], {
      icon: pinIcon(L, pinColor),
      draggable: Boolean(draggable),
      bubblingMouseEvents: false,
      autoPanOnFocus: false,
    }).addTo(map);
    markerRef.current = marker;
    aplicarMarcador(marker);

    marker.on('click', (evento) => {
      if (evento?.originalEvent) L.DomEvent.stopPropagation(evento.originalEvent);
      marker.openPopup();
      const ponto = marker.getLatLng();
      onPressRef.current?.({
        nativeEvent: { coordinate: { latitude: ponto.lat, longitude: ponto.lng } },
      });
    });
    marker.on('dragend', () => {
      const ponto = marker.getLatLng();
      onDragEndRef.current?.({
        nativeEvent: { coordinate: { latitude: ponto.lat, longitude: ponto.lng } },
      });
    });
    marker.on('popupopen', () => {
      const botao = marker.getPopup()?.getElement()?.querySelector('[data-map-action]');
      if (!botao) return;
      botao.onclick = (evento) => {
        evento.preventDefault();
        evento.stopPropagation();
        calloutRef.current?.props?.onPress?.();
      };
    });

    return () => {
      marker.remove();
      if (markerRef.current === marker) markerRef.current = null;
    };
    // Recria só quando o mapa nasce. Posição e texto atualizam no efeito seguinte.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [map]);

  useEffect(() => {
    aplicarMarcador(markerRef.current);
  }, [coordinate?.latitude, coordinate?.longitude, pinColor, title, description, draggable, temAcao]);

  return null;
}

export function Polyline({ coordinates = [], strokeColor = '#0F766E', strokeWidth = 4 }) {
  const map = useContext(MapContext);
  const linhaRef = useRef(null);

  useEffect(() => {
    if (!map || !window.L) return undefined;
    const latlngs = (coordinates || [])
      .filter((c) => Number.isFinite(c?.latitude) && Number.isFinite(c?.longitude))
      .map((c) => [c.latitude, c.longitude]);
    if (latlngs.length < 2) {
      linhaRef.current?.remove();
      linhaRef.current = null;
      return undefined;
    }
    if (!linhaRef.current) {
      linhaRef.current = window.L.polyline(latlngs, {
        color: strokeColor,
        weight: strokeWidth,
      }).addTo(map);
    } else {
      linhaRef.current.setLatLngs(latlngs);
      linhaRef.current.setStyle({ color: strokeColor, weight: strokeWidth });
    }
    return undefined;
  }, [map, coordinates, strokeColor, strokeWidth]);

  useEffect(() => () => {
    linhaRef.current?.remove();
    linhaRef.current = null;
  }, [map]);

  return null;
}

export const PROVIDER_GOOGLE = 'google';
export const PROVIDER_DEFAULT = null;

const styles = StyleSheet.create({
  frame: {
    flex: 1,
    minHeight: 240,
    position: 'relative',
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
});

const canvasStyle = {
  position: 'absolute',
  top: 0,
  right: 0,
  bottom: 0,
  left: 0,
};

export default MapView;
