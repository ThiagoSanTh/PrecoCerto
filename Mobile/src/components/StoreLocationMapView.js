import { useEffect, useRef } from 'react';
import { StyleSheet } from 'react-native';
import MapView, { Marker } from 'react-native-maps';
import {
  MAX_ZOOM_SAQUAREMA,
  MIN_ZOOM_SAQUAREMA,
  regiaoParaPontos,
  travarCoordenadaEmSaquarema,
} from '../utils/saquaremaLock';
import { useMapaSaquarema } from '../hooks/useMapaSaquarema';

/**
 * Mapa para marcar a loja. Abre em Saquarema e não deixa o pin sair da cidade.
 */
export default function StoreLocationMapView({
  latitude,
  longitude,
  titulo,
  onCoordsChange,
  style,
}) {
  const { mapRef, aplicarLimites, mostrarRegiao, onRegionChangeComplete, regiaoInicial } =
    useMapaSaquarema();
  const onChangeRef = useRef(onCoordsChange);
  const semPinNoInicio = useRef(latitude == null || longitude == null);
  onChangeRef.current = onCoordsChange;

  const pin =
    latitude != null && longitude != null
      ? travarCoordenadaEmSaquarema({ latitude, longitude })
      : null;

  useEffect(() => {
    if (latitude == null || longitude == null) return;
    const travada = travarCoordenadaEmSaquarema({ latitude, longitude });
    const lat = Number(latitude);
    const lng = Number(longitude);
    if (Math.abs(travada.latitude - lat) > 0.00001 || Math.abs(travada.longitude - lng) > 0.00001) {
      onChangeRef.current?.(travada);
    }
  }, [latitude, longitude]);

  useEffect(() => {
    if (latitude == null || longitude == null || !semPinNoInicio.current) return;
    semPinNoInicio.current = false;
    mostrarRegiao(
      regiaoParaPontos([travarCoordenadaEmSaquarema({ latitude, longitude })])
    );
  }, [latitude, longitude, mostrarRegiao]);

  function publicar(coordenada) {
    onChangeRef.current?.(travarCoordenadaEmSaquarema(coordenada));
  }

  return (
    <MapView
      ref={mapRef}
      style={[styles.map, style]}
      initialRegion={pin ? regiaoParaPontos([pin]) : regiaoInicial}
      minZoomLevel={MIN_ZOOM_SAQUAREMA}
      maxZoomLevel={MAX_ZOOM_SAQUAREMA}
      onMapReady={aplicarLimites}
      onRegionChangeComplete={onRegionChangeComplete}
      onPress={(evento) => publicar(evento.nativeEvent.coordinate)}
      rotateEnabled={false}
      pitchEnabled={false}
      toolbarEnabled={false}
      moveOnMarkerPress={false}
    >
      {pin ? (
        <Marker
          coordinate={pin}
          title={titulo || 'Local da loja'}
          description="Saquarema, RJ"
          pinColor="#14B8A6"
          draggable
          anchor={{ x: 0.5, y: 1 }}
          onDragEnd={(evento) => publicar(evento.nativeEvent.coordinate)}
        />
      ) : null}
    </MapView>
  );
}

const styles = StyleSheet.create({
  map: {
    height: 260,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
});
