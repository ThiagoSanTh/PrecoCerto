import { View, StyleSheet } from 'react-native';
import { Platform } from 'react-native';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';

/**
 * Wrapper responsivo apenas para telas de auth no web.
 * Telas do app (mapa, tabs) usam largura total — wrapper quebra flex/altura.
 */
export default function WebShell({ children, variant = 'default' }) {
  const { isWeb, isPhoneWeb, isDesktopWeb, authCardMaxWidth } = useLayoutProfile();
  const { colors } = useTheme();

  if (!isWeb || variant !== 'auth') {
    return children;
  }

  return (
    <View style={[styles.root, { backgroundColor: isDesktopWeb ? '#E2E8F0' : colors.background }]}>
      <View
        style={[
          styles.authInner,
          {
            maxWidth: authCardMaxWidth,
            backgroundColor: isDesktopWeb ? colors.surface : 'transparent',
            borderColor: colors.border,
          },
          isDesktopWeb && styles.authCard,
          isPhoneWeb && styles.phonePadding,
        ]}
      >
        {children}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    alignItems: 'center',
    width: '100%',
  },
  authInner: {
    flex: 1,
    width: '100%',
  },
  phonePadding: {
    paddingHorizontal: 4,
  },
  authCard: {
    marginVertical: 32,
    borderRadius: 16,
    borderWidth: 1,
    overflow: 'hidden',
    maxHeight: '92%',
    ...(Platform.OS === 'web'
      ? {
          boxShadow: '0 8px 32px rgba(15, 23, 42, 0.12)',
        }
      : {}),
  },
});
