import { View, StyleSheet, Platform } from 'react-native';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';

/**
 * Wrapper centralizado (80% da largura) para telas mobile/narrow web.
 * Com sidebar web: largura total da área de conteúdo.
 * variant: default | auth | fullBleed
 */
export default function ScreenShell({ children, variant = 'default', fullBleed = false }) {
  const {
    isWeb,
    isDesktopWeb,
    contentWidthPercent,
    contentMaxWidth,
    authCardMaxWidth,
    contentFullWidth,
  } = useLayoutProfile();
  const { colors } = useTheme();

  const shellVariant = fullBleed ? 'fullBleed' : variant;
  const isAuth = shellVariant === 'auth';
  const isFullBleed = shellVariant === 'fullBleed';
  const showAuthCard = isAuth && isWeb;
  const useFullWidth = contentFullWidth || isFullBleed;

  const columnWidth = showAuthCard
    ? '100%'
    : useFullWidth
      ? '100%'
      : `${contentWidthPercent * 100}%`;
  const columnMaxWidth = showAuthCard
    ? authCardMaxWidth
    : useFullWidth
      ? undefined
      : contentMaxWidth;

  const inner = (
    <View
      style={[
        showAuthCard ? styles.authCard : styles.contentColumn,
        useFullWidth && !showAuthCard && styles.contentFullWidth,
        {
          width: columnWidth,
          maxWidth: columnMaxWidth,
          backgroundColor: showAuthCard ? colors.surface : undefined,
          borderColor: showAuthCard ? colors.border : undefined,
        },
      ]}
    >
      {children}
    </View>
  );

  if (isAuth && isWeb) {
    return (
      <View
        style={[
          styles.authRoot,
          styles.authRootWeb,
          { backgroundColor: isDesktopWeb ? '#E2E8F0' : colors.background },
        ]}
      >
        {inner}
      </View>
    );
  }

  return (
    <View
      style={[
        styles.root,
        useFullWidth && styles.rootFullWidth,
        { backgroundColor: colors.background },
      ]}
    >
      {inner}
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    alignItems: 'center',
    width: '100%',
  },
  rootFullWidth: {
    alignItems: 'stretch',
  },
  authRoot: {
    flex: 1,
    alignItems: 'center',
    width: '100%',
  },
  authRootWeb: {
    justifyContent: 'center',
    paddingVertical: 24,
    paddingHorizontal: 16,
  },
  contentColumn: {
    flex: 1,
    alignSelf: 'center',
    overflow: 'hidden',
  },
  contentFullWidth: {
    alignSelf: 'stretch',
    width: '100%',
  },
  authCard: {
    alignSelf: 'center',
    overflow: 'hidden',
    borderRadius: 16,
    borderWidth: 1,
    maxHeight: '92%',
    ...(Platform.OS === 'web'
      ? {
          boxShadow: '0 8px 32px rgba(15, 23, 42, 0.12)',
        }
      : {}),
  },
});
