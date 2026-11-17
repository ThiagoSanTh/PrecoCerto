import { useEffect } from 'react';
import { Platform, View, StyleSheet } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import Routes from './src/navigation';
import { AuthProvider } from './src/context/AuthContext';
import { ThemeProvider, useTheme } from './src/context/ThemeContext';
import { ChatBadgeProvider } from './src/context/ChatBadgeContext';
import { fonts } from './src/theme';

const GOOGLE_FONTS_ID = 'preco-certo-google-fonts';
const GOOGLE_FONTS_HREF =
  'https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&family=Sora:wght@600;700&display=swap';
const VIEWPORT_STYLE_ID = 'preco-certo-viewport-safe-area';

function ThemedStatusBar() {
  const { isDark } = useTheme();
  return <StatusBar style={isDark ? 'light' : 'dark'} />;
}

export default function App() {
  useEffect(() => {
    if (Platform.OS !== 'web' || typeof document === 'undefined') return;

    if (!document.getElementById(GOOGLE_FONTS_ID)) {
      const link = document.createElement('link');
      link.id = GOOGLE_FONTS_ID;
      link.rel = 'stylesheet';
      link.href = GOOGLE_FONTS_HREF;
      document.head.appendChild(link);
    }

    const viewportMeta = document.querySelector('meta[name="viewport"]');
    if (viewportMeta) {
      const content = viewportMeta.getAttribute('content') || '';
      if (!content.includes('viewport-fit=cover')) {
        viewportMeta.setAttribute(
          'content',
          `${content}, viewport-fit=cover`.replace(/^,\s*/, '')
        );
      }
    }

    if (!document.getElementById(VIEWPORT_STYLE_ID)) {
      const style = document.createElement('style');
      style.id = VIEWPORT_STYLE_ID;
      style.textContent = `
        html, body, #root {
          margin: 0;
          padding: 0;
          overflow: hidden;
          overscroll-behavior: none;
        }
        html, body { height: 100%; }
        #root {
          position: fixed;
          top: 0;
          left: 0;
          width: 100%;
          height: var(--app-height, 100dvh);
          overflow: hidden;
        }
        :focus-visible {
          outline: 2px solid var(--focus-ring, #0A5E4D);
          outline-offset: 2px;
        }
      `;
      document.head.appendChild(style);
    }

    const applyVisualViewport = () => {
      const vv = window.visualViewport;
      const height = vv?.height || window.innerHeight;
      const offset = vv?.offsetTop || 0;
      document.documentElement.style.setProperty('--app-height', `${Math.round(height)}px`);
      const root = document.getElementById('root');
      if (root) root.style.top = `${Math.round(offset)}px`;
    };

    applyVisualViewport();
    window.visualViewport?.addEventListener('resize', applyVisualViewport);
    window.visualViewport?.addEventListener('scroll', applyVisualViewport);
    window.addEventListener('resize', applyVisualViewport);
    return () => {
      window.visualViewport?.removeEventListener('resize', applyVisualViewport);
      window.visualViewport?.removeEventListener('scroll', applyVisualViewport);
      window.removeEventListener('resize', applyVisualViewport);
    };
  }, []);

  return (
    <SafeAreaProvider>
      <View style={styles.root}>
        <ThemeProvider>
          <ThemedStatusBar />
          <AuthProvider>
            <ChatBadgeProvider>
              <Routes />
            </ChatBadgeProvider>
          </AuthProvider>
        </ThemeProvider>
      </View>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    ...(Platform.OS === 'web'
      ? {
          height: '100%',
          minHeight: 0,
          width: '100%',
          overflow: 'hidden',
          fontFamily: fonts.sans,
        }
      : null),
  },
});
