import { Platform, View, StyleSheet } from 'react-native';
import { WebView } from 'react-native-webview';
import { forwardRef, useEffect, useImperativeHandle, useRef } from 'react';

/**
 * Mapa Leaflet: WebView no nativo, iframe no Expo Web (PWA).
 * Expõe via ref `enviarMensagem(obj)` — canal app -> mapa para atualizar o
 * estado do mapa (ex.: destaques de pins) sem recriar o HTML.
 */
const LeafletMapFrame = forwardRef(function LeafletMapFrame(
  { html, mapKey, onMessage, style },
  ref
) {
  const iframeRef = useRef(null);
  const webViewRef = useRef(null);

  useImperativeHandle(ref, () => ({
    enviarMensagem(obj) {
      const json = JSON.stringify(obj);
      if (Platform.OS === 'web') {
        iframeRef.current?.contentWindow?.postMessage(json, '*');
      } else {
        webViewRef.current?.injectJavaScript(
          `window.__onAppMessage && window.__onAppMessage(${JSON.stringify(json)}); true;`
        );
      }
    },
  }));

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
      <View style={[styles.frame, styles.frameWeb, style]}>
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
      ref={webViewRef}
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
});

export default LeafletMapFrame;

const styles = StyleSheet.create({
  frame: {
    flex: 1,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: '#e2e8f0',
  },
  frameWeb: {
    minHeight: 280,
    height: '100%',
  },
  iframe: {
    border: 'none',
    width: '100%',
    height: '100%',
    minHeight: 280,
    display: 'block',
  },
});
