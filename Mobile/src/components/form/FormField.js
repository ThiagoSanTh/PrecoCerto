import { forwardRef, useState } from 'react';
import { View, Text, TextInput, Platform, StyleSheet } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';
import { radii } from '../../theme';

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
    onFocus,
    onKeyPress,
    accessibilityLabel,
    icon,
    rightAccessory,
    style,
    inputStyle,
    ...rest
  },
  ref
) {
  const s = useFormStyles();
  const { colors } = useTheme();
  const [focused, setFocused] = useState(false);

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

  if (icon || rightAccessory) {
    return (
      <View style={[s.field, compact && s.fieldCompact, style]}>
        {label ? <Text style={[s.fieldLabel, { color: colors.label }]}>{label}</Text> : null}
        <View
          style={[
            styles.iconWrapper,
            {
              backgroundColor: colors.inputBackground,
              borderColor: focused ? colors.primary : colors.inputBorder,
            },
            focused && styles.iconWrapperFocused,
          ]}
        >
          {icon ? <View style={styles.iconSlot}>{icon}</View> : null}
          <TextInput
            ref={ref}
            style={[
              s.input,
              styles.innerInput,
              {
                color: colors.text,
                backgroundColor: 'transparent',
                borderWidth: 0,
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
            onFocus={(e) => {
              setFocused(true);
              onFocus?.(e);
            }}
            onBlur={(e) => {
              setFocused(false);
              onBlur?.(e);
            }}
            onKeyPress={handleKeyPress}
            accessibilityLabel={accessibilityLabel || label || placeholder}
            {...rest}
          />
          {rightAccessory ? <View style={styles.rightSlot}>{rightAccessory}</View> : null}
        </View>
      </View>
    );
  }

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
            borderColor: focused ? colors.primary : colors.inputBorder,
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
        onFocus={(e) => {
          setFocused(true);
          onFocus?.(e);
        }}
        onBlur={(e) => {
          setFocused(false);
          onBlur?.(e);
        }}
        onKeyPress={handleKeyPress}
        accessibilityLabel={accessibilityLabel || label || placeholder}
        {...rest}
      />
    </View>
  );
});

const styles = StyleSheet.create({
  iconWrapper: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderRadius: radii.lg,
    paddingHorizontal: 12,
    minHeight: 46,
    gap: 8,
  },
  iconWrapperFocused: {
    ...(Platform.OS === 'web'
      ? { boxShadow: '0 0 0 2px rgba(14, 124, 102, 0.22)' }
      : null),
  },
  iconSlot: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  rightSlot: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  innerInput: {
    flex: 1,
    paddingHorizontal: 0,
    minHeight: 44,
  },
});

export default FormField;
