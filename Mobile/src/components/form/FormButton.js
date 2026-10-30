import { Pressable, Text, ActivityIndicator } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';

export function PrimaryButton({
  label,
  onPress,
  loading,
  disabled,
  icon,
  style,
  textStyle,
  accessibilityLabel,
}) {
  const s = useFormStyles();
  const { colors } = useTheme();

  return (
    <Pressable
      style={[
        s.primaryButton,
        { backgroundColor: colors.primary },
        (loading || disabled) && s.buttonDisabled,
        style,
      ]}
      onPress={onPress}
      disabled={loading || disabled}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel || label}
      accessibilityState={{ disabled: !!(disabled || loading), busy: !!loading }}
    >
      {loading ? (
        <ActivityIndicator color={colors.primaryForeground || '#fff'} />
      ) : (
        <>
          {icon}
          <Text style={[s.primaryButtonText, { color: colors.primaryForeground || '#fff' }, textStyle]}>
            {label}
          </Text>
        </>
      )}
    </Pressable>
  );
}

export function SecondaryButton({
  label,
  onPress,
  disabled,
  icon,
  style,
  textStyle,
  accessibilityLabel,
}) {
  const s = useFormStyles();
  const { colors } = useTheme();

  return (
    <Pressable
      style={[
        s.secondaryButton,
        { backgroundColor: colors.surface, borderColor: colors.border },
        disabled && s.buttonDisabled,
        style,
      ]}
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel || label}
      accessibilityState={{ disabled: !!disabled }}
    >
      {icon}
      <Text style={[s.secondaryButtonText, { color: colors.text }, textStyle]}>{label}</Text>
    </Pressable>
  );
}
