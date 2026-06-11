import { View, Text, TextInput } from 'react-native';
import { formStyles as s } from './formStyles';
import { useTheme } from '../../context/ThemeContext';

export default function FormField({
  label,
  value,
  onChangeText,
  placeholder,
  keyboardType,
  autoCapitalize,
  maxLength,
  autoFocus,
  compact,
  secureTextEntry,
  multiline,
  editable = true,
  onSubmitEditing,
  returnKeyType,
  onBlur,
}) {
  const { colors } = useTheme();

  return (
    <View style={[s.field, compact && s.fieldCompact]}>
      {label ? <Text style={[s.fieldLabel, { color: colors.label }]}>{label}</Text> : null}
      <TextInput
        style={[
          s.input,
          {
            color: colors.text,
            backgroundColor: colors.inputBackground,
            borderColor: colors.inputBorder,
          },
        ]}
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder || label}
        placeholderTextColor={colors.textMuted}
        keyboardType={keyboardType}
        autoCapitalize={autoCapitalize ?? 'sentences'}
        maxLength={maxLength}
        autoFocus={autoFocus}
        secureTextEntry={secureTextEntry}
        multiline={multiline}
        editable={editable}
        onSubmitEditing={onSubmitEditing}
        returnKeyType={returnKeyType}
        onBlur={onBlur}
      />
    </View>
  );
}
