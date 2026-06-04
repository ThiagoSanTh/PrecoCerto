import { useMemo } from 'react';
import { StyleSheet } from 'react-native';
import { WebView } from 'react-native-webview';
import { buildLeafletPickerMapHtml } from '../utils/leafletMapHtml';

export default function StoreLocationMapView({
  latitude,
  longitude,
  titulo,
  onCoordsChange,
  style,
}) {
  const mapHtml = useMemo(
    () =>
      buildLeafletPickerMapHtml({
        latitude,
        longitude,
        titulo,
      }),
    [latitude, longitude, titulo]
  );

  function handleMessage(event) {
    try {
      const msg = JSON.parse(event.nativeEvent.data);
      if (msg.type === 'coords' && onCoordsChange) {
        onCoordsChange({
          latitude: msg.latitude,
          longitude: msg.longitude,
        });
      }
    } catch {
      /* ignore */
    }
  }

  return (
    <WebView
      style={[styles.map, style]}
      originWhitelist={['*']}
      source={{ html: mapHtml }}
      javaScriptEnabled
      domStorageEnabled
      onMessage={handleMessage}
      scrollEnabled={false}
      setSupportMultipleWindows={false}
    />
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
