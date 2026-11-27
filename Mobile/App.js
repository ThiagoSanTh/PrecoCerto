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
  'https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&family=Sora:wght@500;600;700&display=swap';
const VIEWPORT_STYLE_ID = 'preco-certo-viewport-safe-area';

function ThemedStatusBar() {
  const { isDark } = useTheme();
  return <StatusBar style={isDark ? 'light' : 'dark'} />;
}

function ThemedApp() {
  const { colors } = useTheme();
  return (
    <View style={[styles.root, { backgroundColor: colors.background }]}>
      <ThemedStatusBar />
      <AuthProvider>
        <ChatBadgeProvider>
          <Routes />
        </ChatBadgeProvider>
      </AuthProvider>
    </View>
  );
}

export default function App() {
  useEffect(() => {
    if (Platform.OS !== 'web' || typeof document === 'undefined') return;

    document.title = 'Preço Certo — Encontre perto de você';

    if (!document.getElementById(GOOGLE_FONTS_ID)) {
      const link = document.createElement('link');
      link.id = GOOGLE_FONTS_ID;
      link.rel = 'stylesheet';
      link.href = GOOGLE_FONTS_HREF;
      document.head.appendChild(link);
    }

    const applyVisualViewport = () => {
      const vv = window.visualViewport;
      const height = vv?.height || window.innerHeight;
      const offset = vv?.offsetTop || 0;
      document.documentElement.style.setProperty('--app-height', `${Math.round(height)}px`);
      const root = document.getElementById('root');
      if (root) root.style.top = `${Math.round(offset)}px`;
    };

    if (!document.getElementById(VIEWPORT_STYLE_ID)) {
      const style = document.createElement('style');
      style.id = VIEWPORT_STYLE_ID;
      style.textContent = `
        html, body, #root {
          height: 100%;
          height: 100dvh;
          min-height: 100dvh;
          margin: 0;
          padding: 0;
          overflow: hidden;
          overscroll-behavior: none;
          -webkit-font-smoothing: antialiased;
          -moz-osx-font-smoothing: grayscale;
        }
      `;
      document.head.appendChild(style);
    }

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
      <ThemeProvider>
        <ThemedApp />
      </ThemeProvider>
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
