import {
  View,
  Text,
  Pressable,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';
import { useTheme } from '../../context/ThemeContext';
import ScreenShell from '../layout/ScreenShell';
import FormErrorBanner from './FormErrorBanner';

export default function FormScreen({
  title,
  subtitle,
  onBack,
  backLabel = 'Voltar',
  children,
  footer,
  scrollable = true,
  steps,
  currentStep = 0,
  webVariant,
  fullBleed = false,
  hideHeader = false,
  noBodyPadding = false,
  error,
  onErrorAction,
}) {
  const s = useFormStyles();
  const { colors } = useTheme();
  const { contentFullWidth } = useLayoutProfile();
  const shellVariant = webVariant === 'auth' ? 'auth' : 'default';
  const isAuthLayout = shellVariant === 'auth';
  const showHeader = !hideHeader;
  const bodyPaddingStyle =
    noBodyPadding
      ? styles.noBodyPadding
      : contentFullWidth
        ? styles.fullWidthBodyPadding
        : null;

  const content = scrollable ? (
    <ScrollView
      style={s.flex}
      contentContainerStyle={[
        s.scrollContent,
        bodyPaddingStyle,
        isAuthLayout && styles.authScrollContent,
      ]}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      <FormErrorBanner error={error} onAction={onErrorAction} />
      {children}
    </ScrollView>
  ) : (
    <View style={[s.body, bodyPaddingStyle]}>
      <FormErrorBanner error={error} onAction={onErrorAction} />
      {children}
    </View>
  );

  return (
    <SafeAreaView
      style={[s.safe, { backgroundColor: colors.background }]}
      edges={['top', 'bottom', 'left', 'right']}
    >
      <ScreenShell variant={shellVariant} fullBleed={fullBleed}>
        <KeyboardAvoidingView
          style={isAuthLayout ? styles.authBody : s.flex}
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        >
          {showHeader ? (
            <View
              style={[
                s.header,
                contentFullWidth && styles.fullWidthHeader,
                { borderBottomColor: colors.border },
              ]}
            >
              {onBack ? (
                <Pressable onPress={onBack} hitSlop={12}>
                  <Text style={s.backLink}>← {backLabel}</Text>
                </Pressable>
              ) : null}
              <Text style={[s.title, { color: colors.text }]}>{title}</Text>
              {subtitle ? (
                <Text style={[s.subtitle, { color: colors.textMuted }]}>{subtitle}</Text>
              ) : null}
              {steps?.length > 0 ? (
                <>
                  <Text style={[s.subtitle, { marginTop: 4, color: colors.textMuted }]}>
                    Passo {currentStep + 1} de {steps.length} · {steps[currentStep]?.label}
                  </Text>
                  <View style={s.progressRow}>
                    {steps.map((item, index) => (
                      <View
                        key={item.key}
                        style={[
                          s.progressDot,
                          { backgroundColor: colors.border },
                          index <= currentStep && { backgroundColor: colors.primary },
                        ]}
                      />
                    ))}
                  </View>
                </>
              ) : null}
            </View>
          ) : null}

          {content}

          {footer ? (
            <View
              style={[
                s.footer,
                { backgroundColor: colors.surface, borderTopColor: colors.border },
              ]}
            >
              {footer}
            </View>
          ) : null}
        </KeyboardAvoidingView>
      </ScreenShell>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  authBody: {
    width: '100%',
  },
  authScrollContent: {
    flexGrow: 1,
  },
  noBodyPadding: {
    paddingHorizontal: 0,
    paddingTop: 0,
    paddingBottom: 0,
  },
  fullWidthBodyPadding: {
    paddingHorizontal: 20,
  },
  fullWidthHeader: {
    paddingHorizontal: 20,
  },
});
