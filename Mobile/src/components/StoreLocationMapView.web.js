import { useMemo } from 'react';
import { StyleSheet } from 'react-native';
import { buildLeafletPickerMapHtml } from '../utils/leafletMapHtml';
import LeafletMapFrame from './LeafletMapFrame';

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

  const mapKey = `${latitude ?? 'x'}-${longitude ?? 'y'}`;

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
    <LeafletMapFrame
      mapKey={mapKey}
      style={[styles.map, style]}
      html={mapHtml}
      onMessage={handleMessage}
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
