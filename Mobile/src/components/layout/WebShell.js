import { View, StyleSheet } from 'react-native';
import { Platform } from 'react-native';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';

export default function WebShell({ children, variant = 'default' }) {
  const { isWeb, isPhoneWeb, isDesktopWeb, contentMaxWidth, authCardMaxWidth } = useLayoutProfile();
  const { colors } = useTheme();

  if (!isWeb) {
    return children;
  }

  const isAuth = variant === 'auth';

  return (
    <View style={[styles.root, { backgroundColor: isDesktopWeb ? '#E2E8F0' : colors.background }]}>
      <View
        style={[
          styles.inner,
          {
            maxWidth: isAuth ? authCardMaxWidth : contentMaxWidth,
            backgroundColor: isAuth && isDesktopWeb ? colors.surface : 'transparent',
            borderColor: colors.border,
          },
          isAuth && isDesktopWeb && styles.authCard,
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
  inner: {
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
