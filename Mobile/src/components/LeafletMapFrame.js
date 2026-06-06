import { Platform, View, StyleSheet } from 'react-native';
import { WebView } from 'react-native-webview';
import { useEffect, useRef } from 'react';

/**
 * Mapa Leaflet: WebView no nativo, iframe no Expo Web (PWA).
 */
export default function LeafletMapFrame({ html, mapKey, onMessage, style }) {
  const iframeRef = useRef(null);

  useEffect(() => {
    if (Platform.OS !== 'web') return undefined;

    function handleWindowMessage(event) {
      if (event.source !== iframeRef.current?.contentWindow) return;
      onMessage?.({ nativeEvent: { data: event.data } });
    }

    window.addEventListener('message', handleWindowMessage);
    return () => window.removeEventListener('message', handleWindowMessage);
  }, [onMessage]);

  if (Platform.OS === 'web') {
    return (
      <View style={[styles.frame, style]}>
        <iframe
          ref={iframeRef}
          key={mapKey}
          title="Mapa"
          srcDoc={html}
          style={styles.iframe}
          sandbox="allow-scripts allow-same-origin"
        />
      </View>
    );
  }

  return (
    <WebView
      key={mapKey}
      style={[styles.frame, style]}
      originWhitelist={['*']}
      source={{ html }}
      javaScriptEnabled
      domStorageEnabled
      scrollEnabled={false}
      setSupportMultipleWindows={false}
      onMessage={onMessage}
    />
  );
}

const styles = StyleSheet.create({
  frame: {
    flex: 1,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
  iframe: {
    border: 'none',
    width: '100%',
    height: '100%',
    minHeight: 260,
  },
});
