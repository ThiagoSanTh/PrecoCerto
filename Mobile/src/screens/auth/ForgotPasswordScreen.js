import { View, Text, Pressable, StyleSheet } from 'react-native';
import { useState, useRef } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { solicitarRecuperacaoSenha, redefinirSenha } from '../../services/authService';
import { mapApiError } from '../../utils/apiErrorUtils';
import { isEmailValido } from '../../utils/validacaoUtils';
import { useTheme } from '../../context/ThemeContext';
import { FormField, PrimaryButton, SecondaryButton } from '../../components/form';
import AuthSplitLayout from '../../components/ui/AuthSplitLayout';
import { fonts } from '../../theme';

export default function ForgotPasswordScreen({ navigation }) {
  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [modoToken, setModoToken] = useState(false);
  const [loading, setLoading] = useState(false);
  const novaSenhaRef = useRef(null);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const { colors } = useTheme();

  async function handleSolicitar() {
    if (!isEmailValido(email)) {
      setError({ title: 'E-mail inválido', message: 'Informe um e-mail válido.' });
      return;
    }

    setError(null);
    setSuccess(null);
    setLoading(true);
    try {
      await solicitarRecuperacaoSenha(email);
      setSuccess({
        title: 'Verifique seu e-mail',
        message: 'Se o e-mail estiver cadastrado, enviamos um token para redefinir a senha.',
        variant: 'success',
      });
      setModoToken(true);
    } catch (err) {
      setError(mapApiError(err));
    } finally {
      setLoading(false);
    }
  }

  async function handleRedefinir() {
    if (!token || novaSenha.length < 6) {
      setError({
        title: 'Dados incompletos',
        message: 'Informe o token e uma senha com pelo menos 6 caracteres.',
      });
      return;
    }

    setError(null);
    setLoading(true);
    try {
      await redefinirSenha(token.trim(), novaSenha);
      setSuccess({ title: 'Sucesso', message: 'Senha redefinida. Faça login.', variant: 'success' });
      setTimeout(() => navigation.replace('Login'), 1500);
    } catch (err) {
      setError(mapApiError(err));
    } finally {
      setLoading(false);
    }
  }

  const banner = error || success;

  return (
    <AuthSplitLayout
      title="Recuperar senha"
      subtitle={
        modoToken
          ? 'Informe o token recebido por e-mail e defina uma nova senha.'
          : 'Informe o e-mail da sua conta para receber o token de recuperação.'
      }
      error={banner}
    >
      {!modoToken ? (
        <>
          <FormField
            label="E-mail"
            placeholder="voce@email.com"
            value={email}
            onChangeText={(v) => {
              setEmail(v);
              if (error) setError(null);
            }}
            autoCapitalize="none"
            keyboardType="email-address"
            returnKeyType="go"
            onSubmitEditing={handleSolicitar}
            icon={<Ionicons name="mail-outline" size={17} color={colors.textMuted} />}
          />

          <PrimaryButton
            label="Solicitar recuperação"
            onPress={handleSolicitar}
            loading={loading}
            style={styles.submitBtn}
          />
          <SecondaryButton
            label="Já tenho o token"
            onPress={() => setModoToken(true)}
            style={styles.secondaryBtn}
          />
        </>
      ) : (
        <>
          <FormField
            label="Token"
            placeholder="Código recebido por e-mail"
            value={token}
            onChangeText={(v) => {
              setToken(v);
              if (error) setError(null);
            }}
            autoCapitalize="none"
            returnKeyType="next"
            onSubmitEditing={() => novaSenhaRef.current?.focus()}
            icon={<Ionicons name="key-outline" size={17} color={colors.textMuted} />}
          />

          <FormField
            ref={novaSenhaRef}
            label="Nova senha"
            placeholder="••••••••"
            value={novaSenha}
            onChangeText={(v) => {
              setNovaSenha(v);
              if (error) setError(null);
            }}
            secureTextEntry
            returnKeyType="go"
            onSubmitEditing={handleRedefinir}
            icon={<Ionicons name="lock-closed-outline" size={17} color={colors.textMuted} />}
          />

          <PrimaryButton
            label="Redefinir senha"
            onPress={handleRedefinir}
            loading={loading}
            style={styles.submitBtn}
          />
        </>
      )}

      <View style={styles.switchRow}>
        <Text style={[styles.switchMuted, { color: colors.textMuted }]}>
          Lembrou sua senha?{' '}
        </Text>
        <Pressable
          onPress={() => navigation.navigate('Login')}
          accessibilityRole="button"
          accessibilityLabel="Voltar para login"
        >
          <Text style={[styles.switchLink, { color: colors.primary }]}>
            Entrar
          </Text>
        </Pressable>
      </View>
    </AuthSplitLayout>
  );
}

const styles = StyleSheet.create({
  submitBtn: {
    marginTop: 4,
  },
  secondaryBtn: {
    marginTop: 10,
  },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 22,
  },
  switchMuted: {
    fontFamily: fonts.sans,
    fontSize: 14,
  },
  switchLink: {
    fontFamily: fonts.sans,
    fontSize: 14,
    fontWeight: '700',
  },
});
