import { useEffect } from 'react';
import { Platform, View, StyleSheet } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import Routes from './src/navigation';
import { AuthProvider } from './src/context/AuthContext';
import { ThemeProvider } from './src/context/ThemeContext';
import { ChatBadgeProvider } from './src/context/ChatBadgeContext';
import { fonts } from './src/theme';

const GOOGLE_FONTS_ID = 'preco-certo-google-fonts';
const GOOGLE_FONTS_HREF =
  'https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&family=Sora:wght@600;700&display=swap';

export default function App() {
  useEffect(() => {
    if (Platform.OS !== 'web' || typeof document === 'undefined') return;
    if (document.getElementById(GOOGLE_FONTS_ID)) return;

    const link = document.createElement('link');
    link.id = GOOGLE_FONTS_ID;
    link.rel = 'stylesheet';
    link.href = GOOGLE_FONTS_HREF;
    document.head.appendChild(link);
  }, []);

  return (
    <SafeAreaProvider>
      <View style={styles.root}>
        <ThemeProvider>
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
      ? { minHeight: '100vh', width: '100%', fontFamily: fonts.sans }
      : null),
  },
});
