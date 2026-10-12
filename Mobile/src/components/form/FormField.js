import { forwardRef } from 'react';
import { View, Text, TextInput, Platform } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';

const FormField = forwardRef(function FormField(
  {
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
    onKeyPress,
    accessibilityLabel,
    style,
    inputStyle,
    ...rest
  },
  ref
) {
  const s = useFormStyles();
  const { colors } = useTheme();

  const handleKeyPress = (e) => {
    onKeyPress?.(e);
    if (
      Platform.OS === 'web' &&
      !multiline &&
      e.nativeEvent?.key === 'Enter' &&
      !e.nativeEvent?.shiftKey
    ) {
      if (typeof onSubmitEditing === 'function') {
        e.preventDefault?.();
        onSubmitEditing();
      }
    }
  };

  return (
    <View style={[s.field, compact && s.fieldCompact, style]}>
      {label ? <Text style={[s.fieldLabel, { color: colors.label }]}>{label}</Text> : null}
      <TextInput
        ref={ref}
        style={[
          s.input,
          {
            color: colors.text,
            backgroundColor: colors.inputBackground,
            borderColor: colors.inputBorder,
          },
          inputStyle,
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
        onKeyPress={handleKeyPress}
        accessibilityLabel={accessibilityLabel || label || placeholder}
        {...rest}
      />
    </View>
  );
});

export default FormField;
