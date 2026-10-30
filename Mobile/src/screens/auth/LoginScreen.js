import { StatusBar } from 'expo-status-bar';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { useState, useRef } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { login as authLogin } from '../../services/authService';
import { mapApiError } from '../../utils/apiErrorUtils';
import { useAuth, podeUsarModoLoja } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { FormField, PrimaryButton } from '../../components/form';
import AuthSplitLayout from '../../components/ui/AuthSplitLayout';
import { fonts } from '../../theme';

export default function LoginScreen({ navigation }) {
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const senhaRef = useRef(null);
  const { salvarSessao, sincronizarGpsCliente } = useAuth();
  const { colors } = useTheme();

  async function handleLogin() {
    if (loading) return;
    if (!email || !senha) {
      setError({ title: 'Campos obrigatórios', message: 'Preencha todos os campos.' });
      return;
    }

    setError(null);
    setLoading(true);
    try {
      const { tipo, perfil } = await authLogin(email.trim().toLowerCase(), senha);
      const novaSessao = { tipo, perfil };
      const modo = podeUsarModoLoja(novaSessao) ? 'store' : 'user';
      await salvarSessao(novaSessao, modo);
      navigation.replace('Home');

      if (perfil?.id) {
        sincronizarGpsCliente(perfil.id, { force: true }).catch(() => {});
      }
    } catch (err) {
      setError(mapApiError(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthSplitLayout
      title="Bem-vindo de volta"
      subtitle="Após entrar, sua localização é obtida pelo GPS para mostrar as lojas próximas."
      error={error}
    >
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
        returnKeyType="next"
        onSubmitEditing={() => senhaRef.current?.focus()}
        accessibilityLabel="Campo de e-mail"
        icon={<Ionicons name="mail-outline" size={17} color={colors.textMuted} />}
      />

      <FormField
        ref={senhaRef}
        label="Senha"
        placeholder="••••••••"
        value={senha}
        onChangeText={(v) => {
          setSenha(v);
          if (error) setError(null);
        }}
        secureTextEntry
        returnKeyType="go"
        onSubmitEditing={handleLogin}
        accessibilityLabel="Campo de senha"
        icon={<Ionicons name="lock-closed-outline" size={17} color={colors.textMuted} />}
      />

      <View style={styles.forgotRow}>
        <Pressable
          onPress={() => navigation.navigate('EsqueciSenha')}
          accessibilityRole="button"
          accessibilityLabel="Esqueci a senha"
          hitSlop={8}
        >
          <Text style={[styles.forgotLink, { color: colors.primary }]}>
            Esqueci a senha
          </Text>
        </Pressable>
      </View>

      <PrimaryButton
        label="Entrar"
        onPress={handleLogin}
        loading={loading}
        style={styles.submitBtn}
      />

      <View style={styles.switchRow}>
        <Text style={[styles.switchMuted, { color: colors.textMuted }]}>
          Novo por aqui?{' '}
        </Text>
        <Pressable
          onPress={() => navigation.navigate('Cadastro')}
          accessibilityRole="button"
          accessibilityLabel="Criar nova conta"
        >
          <Text style={[styles.switchLink, { color: colors.primary }]}>
            Criar conta
          </Text>
        </Pressable>
      </View>

      <StatusBar style="auto" />
    </AuthSplitLayout>
  );
}

const styles = StyleSheet.create({
  forgotRow: {
    alignItems: 'flex-end',
    marginTop: -2,
    marginBottom: 14,
  },
  forgotLink: {
    fontFamily: fonts.sans,
    fontSize: 12,
    fontWeight: '700',
  },
  submitBtn: {
    marginTop: 4,
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
