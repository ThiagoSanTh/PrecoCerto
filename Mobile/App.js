import { Platform, View, StyleSheet } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import Routes from './src/navigation';
import { AuthProvider } from './src/context/AuthContext';
import { ThemeProvider } from './src/context/ThemeContext';
import { ChatBadgeProvider } from './src/context/ChatBadgeContext';

export default function App() {
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
    ...(Platform.OS === 'web' ? { minHeight: '100vh', width: '100%' } : null),
  },
});
