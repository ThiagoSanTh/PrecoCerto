import { useEffect, useRef } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import MapView, { Marker, UrlTile, PROVIDER_DEFAULT } from 'react-native-maps';
import { SAQUAREMA_CENTER } from '../utils/mapaUtils';

/**
 * Mapa para escolher a localização da loja (Android/iOS): toque no mapa ou
 * arraste o pin. A versão web continua em StoreLocationMapView.web.js (Leaflet).
 */
export default function StoreLocationMapView({
  latitude,
  longitude,
  titulo,
  onCoordsChange,
  style,
}) {
  const mapRef = useRef(null);
  const hasPin = latitude != null && longitude != null;
  const coord = hasPin
    ? { latitude: Number(latitude), longitude: Number(longitude) }
    : null;

  // Recentraliza quando as coordenadas mudam por fora (ex.: geocodificação do endereço).
  useEffect(() => {
    if (!coord) return;
    mapRef.current?.animateToRegion(
      { ...coord, latitudeDelta: 0.01, longitudeDelta: 0.01 },
      300
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [latitude, longitude]);

  function emitir(c) {
    onCoordsChange?.({ latitude: c.latitude, longitude: c.longitude });
  }

  return (
    <View style={[styles.map, style]}>
      <MapView
        ref={mapRef}
        style={StyleSheet.absoluteFill}
        provider={PROVIDER_DEFAULT}
        mapType="none"
        initialRegion={{
          latitude: coord?.latitude ?? SAQUAREMA_CENTER[0],
          longitude: coord?.longitude ?? SAQUAREMA_CENTER[1],
          latitudeDelta: hasPin ? 0.01 : 0.08,
          longitudeDelta: hasPin ? 0.01 : 0.08,
        }}
        toolbarEnabled={false}
        onPress={(e) => emitir(e.nativeEvent.coordinate)}
      >
        <UrlTile
          urlTemplate="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
          maximumZ={19}
          flipY={false}
          tileSize={256}
        />
        {coord ? (
          <Marker
            coordinate={coord}
            title={titulo || 'Local da loja'}
            pinColor="#14B8A6"
            draggable
            onDragEnd={(e) => emitir(e.nativeEvent.coordinate)}
          />
        ) : null}
      </MapView>
      <Text style={styles.atribuicao} pointerEvents="none">© OpenStreetMap</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  map: {
    height: 260,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
  atribuicao: {
    position: 'absolute',
    right: 4,
    bottom: 2,
    fontSize: 10,
    color: '#334155',
    backgroundColor: 'rgba(255,255,255,0.7)',
    paddingHorizontal: 4,
  },
});

