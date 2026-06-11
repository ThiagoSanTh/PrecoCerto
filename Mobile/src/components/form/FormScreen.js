import {
  View,
  Text,
  Pressable,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { formStyles as s } from './formStyles';
import { useTheme } from '../../context/ThemeContext';
import WebShell from '../layout/WebShell';

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
  webVariant = 'default',
}) {
  const { colors } = useTheme();

  const content = scrollable ? (
    <ScrollView
      style={s.flex}
      contentContainerStyle={s.scrollContent}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      {children}
    </ScrollView>
  ) : (
    <View style={[s.body, s.bodyFill]}>{children}</View>
  );

  return (
    <WebShell variant={webVariant}>
      <SafeAreaView
        style={[s.safe, { backgroundColor: colors.background }]}
        edges={['top', 'bottom', 'left', 'right']}
      >
      <KeyboardAvoidingView
        style={s.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <View style={[s.header, { borderBottomColor: colors.border }]}>
          {onBack ? (
            <Pressable onPress={onBack} hitSlop={12}>
              <Text style={s.backLink}>← {backLabel}</Text>
            </Pressable>
          ) : null}
          <Text style={[s.title, { color: colors.text }]}>{title}</Text>
          {subtitle ? <Text style={[s.subtitle, { color: colors.textMuted }]}>{subtitle}</Text> : null}
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

        {content}

        {footer ? (
          <View style={[s.footer, { backgroundColor: colors.surface, borderTopColor: colors.border }]}>
            {footer}
          </View>
        ) : null}
      </KeyboardAvoidingView>
      </SafeAreaView>
    </WebShell>
  );
}
