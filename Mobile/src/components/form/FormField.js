import { forwardRef, useId, useRef, useState } from 'react';
import { View, Text, TextInput, Platform, StyleSheet } from 'react-native';
import { useFormStyles } from '../../hooks/useFormStyles';
import { useTheme } from '../../context/ThemeContext';
import { radii } from '../../theme';

function FieldLabel({ label, htmlFor, nativeID, style, onPress }) {
  if (Platform.OS === 'web' && htmlFor) {
    const flat = StyleSheet.flatten(style) || {};
    return (
      <label
        htmlFor={htmlFor}
        id={nativeID}
        style={{ ...flat, display: 'block', cursor: 'pointer' }}
      >
        {label}
      </label>
    );
  }

  return (
    <Text nativeID={nativeID} onPress={onPress} style={style}>
      {label}
    </Text>
  );
}

const FormField = forwardRef(function FormField(
  {
    label,
    value,
    onChangeText,
    placeholder,
    keyboardType,
    autoCapitalize,
    autoComplete,
    autoCorrect,
    spellCheck,
    name,
    id,
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
  const reactId = useId();
  const inputId = id || `pc-${String(reactId).replace(/:/g, '')}`;
  const labelId = `${inputId}-label`;
  const innerRef = useRef(null);
  const isEmail = keyboardType === 'email-address';

  const setInputRef = (node) => {
    innerRef.current = node;
    if (Platform.OS === 'web' && node?.setAttribute && name) {
      node.setAttribute('name', name);
    }
    if (typeof ref === 'function') ref(node);
    else if (ref) ref.current = node;
  };

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

  const scrollFocusedField = (e) => {
    if (Platform.OS !== 'web') return;
    const node = e?.target || innerRef.current;
    const scroll = () => node?.scrollIntoView?.({ block: 'center', inline: 'nearest' });
    requestAnimationFrame(scroll);
    window.visualViewport?.addEventListener('resize', scroll, { once: true });
  };

  const handleFocus = (e) => {
    setFocused(true);
    onFocus?.(e);
    scrollFocusedField(e);
  };

  const handleBlur = (e) => {
    setFocused(false);
    onBlur?.(e);
  };

  const labelNode = label ? (
    <FieldLabel
      label={label}
      htmlFor={inputId}
      nativeID={labelId}
      style={[s.fieldLabel, { color: colors.label }]}
      onPress={() => innerRef.current?.focus?.()}
    />
  ) : null;

  const inputProps = {
    ref: setInputRef,
    ...(Platform.OS === 'web' ? { id: inputId } : null),
    value,
    onChangeText,
    placeholder: placeholder || label,
    placeholderTextColor: colors.textMuted,
    keyboardType,
    autoCapitalize: autoCapitalize ?? (isEmail ? 'none' : 'sentences'),
    ...(isEmail || autoCorrect != null
      ? { autoCorrect: isEmail ? (autoCorrect ?? false) : autoCorrect }
      : null),
    ...(Platform.OS === 'web'
      ? {
          autoComplete: autoComplete ?? (isEmail ? 'email' : undefined),
          spellCheck: spellCheck ?? (isEmail ? false : undefined),
        }
      : null),
    maxLength,
    autoFocus,
    secureTextEntry,
    multiline,
    editable,
    onSubmitEditing,
    returnKeyType,
    onFocus: handleFocus,
    onBlur: handleBlur,
    onKeyPress: handleKeyPress,
    accessibilityLabel: accessibilityLabel || label || placeholder,
    ...rest,
  };

  if (icon || rightAccessory) {
    return (
      <View style={[s.field, compact && s.fieldCompact, style]}>
        {labelNode}
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
            {...inputProps}
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
          />
          {rightAccessory ? <View style={styles.rightSlot}>{rightAccessory}</View> : null}
        </View>
      </View>
    );
  }

  return (
    <View style={[s.field, compact && s.fieldCompact, style]}>
      {labelNode}
      <TextInput
        {...inputProps}
        style={[
          s.input,
          {
            color: colors.text,
            backgroundColor: colors.inputBackground,
            borderColor: focused ? colors.primary : colors.inputBorder,
          },
          inputStyle,
        ]}
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
