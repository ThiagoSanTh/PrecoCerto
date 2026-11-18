import { View, Text, Pressable, StyleSheet } from 'react-native';
import { useState, useRef } from 'react';
import { Ionicons } from '@expo/vector-icons';
import { registrarCliente } from '../../services/clienteService';
import { login as authLogin } from '../../services/authService';
import { saveToken } from '../../services/tokenStorage';
import { mapApiError } from '../../utils/apiErrorUtils';
import { obterLocalizacaoAtual } from '../../services/locationService';
import { isEmailValido, isTelefoneValido } from '../../utils/validacaoUtils';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { FormField, PrimaryButton } from '../../components/form';
import AuthSplitLayout from '../../components/ui/AuthSplitLayout';
import { fonts } from '../../theme';
import { touchTarget } from '../../components/form/formStyles';

const LOGIN_RETRY_DELAYS_MS = [300, 600, 1200];

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function loginComRetry(email, senha) {
  let lastError;
  for (let i = 0; i <= LOGIN_RETRY_DELAYS_MS.length; i++) {
    try {
      return await authLogin(email, senha);
    } catch (error) {
      lastError = error;
      if (i < LOGIN_RETRY_DELAYS_MS.length) {
        await sleep(LOGIN_RETRY_DELAYS_MS[i]);
      }
    }
  }
  throw lastError;
}

export default function RegisterScreen({ navigation }) {
  const [nomeUsuario, setNomeUsuario] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [senha, setSenha] = useState('');
  const [confirmarSenha, setConfirmarSenha] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const emailRef = useRef(null);
  const telefoneRef = useRef(null);
  const senhaRef = useRef(null);
  const confirmarSenhaRef = useRef(null);

  const { salvarSessao, sincronizarGpsCliente } = useAuth();
  const { colors } = useTheme();

  function change(setter) {
    return (value) => {
      setter(value);
      if (error) setError(null);
    };
  }

  async function concluirSessao(tipo, perfil) {
    await salvarSessao({ tipo: tipo || 'cliente', perfil }, 'user');
    navigation.reset({ index: 0, routes: [{ name: 'Home' }] });

    if (perfil?.id) {
      sincronizarGpsCliente(perfil.id, { force: true }).catch(() => {});
    }
  }

  async function handleRegister() {
    if (!nomeUsuario || !email || !senha || !confirmarSenha) {
      setError({ title: 'Campos obrigatórios', message: 'Preencha todos os campos obrigatórios.' });
      return;
    }

    if (!isEmailValido(email)) {
      setError({ title: 'E-mail inválido', message: 'Informe um e-mail válido.' });
      return;
    }

    if (senha.length < 6) {
      setError({ title: 'Senha curta', message: 'A senha deve ter pelo menos 6 caracteres.' });
      return;
    }

    if (telefone.trim() && !isTelefoneValido(telefone)) {
      setError({
        title: 'Telefone inválido',
        message:
          'Telefone inválido. Informe 8 dígitos (fixo) ou 9 dígitos (celular), com DDD opcional.',
      });
      return;
    }

    if (senha !== confirmarSenha) {
      setError({ title: 'Senhas diferentes', message: 'As senhas não coincidem.' });
      return;
    }

    setError(null);
    setLoading(true);
    const emailNormalizado = email.trim().toLowerCase();
    let registroOk = false;

    try {
      let latitudeAtual = null;
      let longitudeAtual = null;

      try {
        const coords = await obterLocalizacaoAtual();
        latitudeAtual = coords.latitude;
        longitudeAtual = coords.longitude;
      } catch {
        // GPS opcional no cadastro
      }

      const resposta = await registrarCliente({
        nomeUsuario: nomeUsuario.trim(),
        email: emailNormalizado,
        senha,
        telefone: telefone.trim() || null,
        latitudeAtual,
        longitudeAtual,
      });

      registroOk = true;

      let token = resposta?.token;
      let tipo = resposta?.tipo;
      let perfil = resposta?.perfil;

      if (!token) {
        const loginData = await loginComRetry(emailNormalizado, senha);
        token = loginData.token;
        tipo = loginData.tipo;
        perfil = loginData.perfil;
      } else {
        await saveToken(token);
      }

      await concluirSessao(tipo, perfil);
    } catch (err) {
      const mapped = mapApiError(err);

      if (err.response?.status === 409 || mapped.code === 'EMAIL_ALREADY_REGISTERED') {
        setError({
          ...mapped,
          actionLabel: 'Ir para login',
        });
        return;
      }

      if (registroOk) {
        setError({
          title: 'Conta criada',
          message: 'Sua conta foi criada. Entre com seu e-mail e senha.',
          variant: 'success',
          actionLabel: 'Ir para login',
        });
        return;
      }

      setError(mapped);
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthSplitLayout
      title="Crie sua conta"
      subtitle="Após entrar, sua localização é obtida pelo GPS para mostrar as lojas próximas."
      error={error}
      onErrorAction={() => navigation.navigate('Login')}
    >
      <FormField
        id="cadastro-nome"
        name="name"
        autoComplete="name"
        label="Nome"
        placeholder="Seu nome"
        value={nomeUsuario}
        onChangeText={change(setNomeUsuario)}
        returnKeyType="next"
        onSubmitEditing={() => emailRef.current?.focus()}
        icon={<Ionicons name="person-outline" size={17} color={colors.textMuted} />}
      />

      <FormField
        ref={emailRef}
        id="cadastro-email"
        name="email"
        autoComplete="email"
        label="E-mail"
        placeholder="voce@email.com"
        value={email}
        onChangeText={change(setEmail)}
        autoCapitalize="none"
        keyboardType="email-address"
        returnKeyType="next"
        onSubmitEditing={() => telefoneRef.current?.focus()}
        icon={<Ionicons name="mail-outline" size={17} color={colors.textMuted} />}
      />

      <FormField
        ref={telefoneRef}
        id="cadastro-telefone"
        name="tel"
        autoComplete="tel"
        label="Telefone (opcional)"
        placeholder="(22) 99999-9999"
        value={telefone}
        onChangeText={change(setTelefone)}
        keyboardType="phone-pad"
        returnKeyType="next"
        onSubmitEditing={() => senhaRef.current?.focus()}
        icon={<Ionicons name="call-outline" size={17} color={colors.textMuted} />}
      />

      <FormField
        ref={senhaRef}
        id="cadastro-senha"
        name="new-password"
        autoComplete="new-password"
        label="Senha"
        placeholder="Crie uma senha"
        value={senha}
        onChangeText={change(setSenha)}
        secureTextEntry
        returnKeyType="next"
        onSubmitEditing={() => confirmarSenhaRef.current?.focus()}
        icon={<Ionicons name="lock-closed-outline" size={17} color={colors.textMuted} />}
      />

      <FormField
        ref={confirmarSenhaRef}
        id="cadastro-confirmar-senha"
        name="confirm-password"
        autoComplete="new-password"
        label="Confirmar senha"
        placeholder="Repita a senha"
        value={confirmarSenha}
        onChangeText={change(setConfirmarSenha)}
        secureTextEntry
        returnKeyType="go"
        onSubmitEditing={handleRegister}
        icon={<Ionicons name="shield-checkmark-outline" size={17} color={colors.textMuted} />}
      />

      <PrimaryButton
        label="Criar conta"
        onPress={handleRegister}
        loading={loading}
        style={styles.submitBtn}
      />

      <View style={styles.switchRow}>
        <Text style={[styles.switchMuted, { color: colors.textMuted }]}>
          Já tem conta?{' '}
        </Text>
        <Pressable
          onPress={() => navigation.navigate('Login')}
          accessibilityRole="button"
          accessibilityLabel="Ir para login"
          style={touchTarget}
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
    marginTop: 8,
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
