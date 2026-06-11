import { Pressable, Text, ActivityIndicator } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';

export function PrimaryButton({ label, onPress, loading, disabled, style }) {
  const s = useFormStyles();

  return (
    <Pressable
      style={[s.primaryButton, (loading || disabled) && s.buttonDisabled, style]}
      onPress={onPress}
      disabled={loading || disabled}
    >
      {loading ? (
        <ActivityIndicator color="#fff" />
      ) : (
        <Text style={s.primaryButtonText}>{label}</Text>
      )}
    </Pressable>
  );
}

export function SecondaryButton({ label, onPress, disabled, style }) {
  const s = useFormStyles();
  const { colors } = useTheme();

  return (
    <Pressable
      style={[s.secondaryButton, disabled && s.buttonDisabled, style]}
      onPress={onPress}
      disabled={disabled}
    >
      <Text style={[s.secondaryButtonText, { color: colors.primaryDark }]}>{label}</Text>
    </Pressable>
  );
}
