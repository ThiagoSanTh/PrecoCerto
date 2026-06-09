import { View, Text, Pressable, Alert, ActivityIndicator, Switch } from 'react-native';
import { useState, useCallback } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';
import { atualizarCliente } from '../../services/clienteService';
import { atualizarLojista } from '../../services/lojistaService';
import { isEmailValido } from '../../utils/validacaoUtils';
import {
  FormScreen,
  FormField,
  PrimaryButton,
  SecondaryButton,
  formStyles,
} from '../../components/form';
import { colors } from '../../theme';

export default function UserScreen({ navigation }) {
  const { session, logout, sincronizarGpsCliente, isCliente, isLojista, salvarSessao } =
    useAuth();
  const { isDark, alternarTema, colors: tema } = useTheme();

  const [nomeUsuario, setNomeUsuario] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [cargo, setCargo] = useState('');
  const [gpsStatus, setGpsStatus] = useState('');
  const [loadingGps, setLoadingGps] = useState(false);
  const [salvando, setSalvando] = useState(false);
  const [temLoja, setTemLoja] = useState(false);

  useFocusEffect(
    useCallback(() => {
      if (session?.perfil) {
        setNomeUsuario(session.perfil.nomeUsuario || '');
        setEmail(session.perfil.email || '');
        setTelefone(session.perfil.telefone || '');
        setCargo(session.perfil.cargo || '');
        if (session.perfil.lojaId) setTemLoja(true);
      }
    }, [session])
  );

  function validarFormulario() {
    if (!nomeUsuario.trim()) {
      Alert.alert('Erro', 'Informe o nome de usuário.');
      return false;
    }
    if (!isEmailValido(email)) {
      Alert.alert('Erro', 'Informe um e-mail válido.');
      return false;
    }
    return true;
  }

  async function handleAtualizarPerfilCliente() {
    if (!session?.perfil?.id || !validarFormulario()) return;

    setSalvando(true);
    try {
      const atualizado = await atualizarCliente(session.perfil.id, {
        nomeUsuario: nomeUsuario.trim(),
        email: email.trim(),
        telefone: telefone.trim() || null,
        senha: '',
      });
      await salvarSessao({ tipo: 'cliente', perfil: atualizado }, 'user');
      Alert.alert('Sucesso', 'Perfil atualizado');
    } catch (error) {
      Alert.alert('Erro', String(error.response?.data || error.message));
    } finally {
      setSalvando(false);
    }
  }

  async function handleAtualizarPerfilLojista() {
    if (!session?.perfil?.id || !validarFormulario()) return;

    setSalvando(true);
    try {
      const atualizado = await atualizarLojista(session.perfil.id, {
        nomeUsuario: nomeUsuario.trim(),
        email: email.trim(),
        telefone: telefone.trim() || null,
        cargo: cargo.trim() || null,
      });
      await salvarSessao(
        { tipo: 'lojista', perfil: { ...session.perfil, ...atualizado } },
        'store'
      );
      Alert.alert('Sucesso', 'Perfil atualizado');
    } catch (error) {
      Alert.alert('Erro', String(error.response?.data || error.message));
    } finally {
      setSalvando(false);
    }
  }

  async function handleSincronizarGps() {
    setLoadingGps(true);
    setGpsStatus('');
    try {
      const coords = await sincronizarGpsCliente();
      if (coords) {
        setGpsStatus(
          `GPS: ${coords.latitude.toFixed(5)}, ${coords.longitude.toFixed(5)}`
        );
      } else {
        setGpsStatus('Não foi possível obter o GPS. Verifique permissões.');
      }
    } finally {
      setLoadingGps(false);
    }
  }

  async function handleLogout() {
    await logout();
    navigation.replace('Login');
  }

  async function goToUserMode() {
    await salvarSessao(session, 'user');
    navigation.replace('Home');
  }

  const temaToggle = (
    <View
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        paddingVertical: 8,
      }}
    >
      <Text style={{ fontSize: 14, color: tema.text }}>Modo escuro</Text>
      <Switch
        value={isDark}
        onValueChange={alternarTema}
        trackColor={{ true: colors.primary }}
      />
    </View>
  );

  if (isLojista) {
    return (
      <FormScreen
        title="Perfil lojista"
        subtitle="Gerencie os dados da sua conta"
        scrollable
        footer={
          <>
            <PrimaryButton label="Modo cliente" onPress={goToUserMode} />
            <SecondaryButton label="Sair" onPress={handleLogout} />
          </>
        }
      >
        <FormField label="Nome de usuário" value={nomeUsuario} onChangeText={setNomeUsuario} />
        <FormField
          label="E-mail"
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          keyboardType="email-address"
        />
        <FormField
          label="Telefone"
          value={telefone}
          onChangeText={setTelefone}
          keyboardType="phone-pad"
        />
        <FormField label="Cargo na loja" value={cargo} onChangeText={setCargo} />

        <Text style={formStyles.sectionHint}>Loja ID: {session.perfil.lojaId || '—'}</Text>

        <PrimaryButton
          label="Salvar perfil"
          onPress={handleAtualizarPerfilLojista}
          loading={salvando}
        />
        <SecondaryButton
          label="Alterar senha"
          onPress={() => navigation.navigate('ChangePassword')}
        />

        <Text style={[formStyles.summaryTitle, { marginTop: 16, marginBottom: 4, color: tema.text }]}>
          Aparência
        </Text>
        {temaToggle}
      </FormScreen>
    );
  }

  return (
    <FormScreen
      title="Meu perfil"
      subtitle="Gerencie seus dados e localização"
      scrollable
      footer={<SecondaryButton label="Sair" onPress={handleLogout} />}
    >
      <Text style={formStyles.sectionHint}>
        Localização obtida automaticamente pelo GPS — sem digitar coordenadas.
      </Text>

      <Pressable
        style={[formStyles.primaryButton, loadingGps && formStyles.buttonDisabled]}
        onPress={handleSincronizarGps}
        disabled={loadingGps}
      >
        {loadingGps ? (
          <ActivityIndicator color="#fff" />
        ) : (
          <Text style={formStyles.primaryButtonText}>Atualizar localização (GPS)</Text>
        )}
      </Pressable>

      {gpsStatus ? (
        <Text style={[formStyles.sectionHint, { color: colors.primaryDark }]}>{gpsStatus}</Text>
      ) : null}

      <Text style={[formStyles.summaryTitle, { marginTop: 16, marginBottom: 8 }]}>
        Dados pessoais
      </Text>
      <FormField label="Nome de usuário" value={nomeUsuario} onChangeText={setNomeUsuario} />
      <FormField
        label="E-mail"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        keyboardType="email-address"
      />
      <FormField
        label="Telefone"
        value={telefone}
        onChangeText={setTelefone}
        keyboardType="phone-pad"
      />

      <PrimaryButton
        label="Salvar perfil"
        onPress={handleAtualizarPerfilCliente}
        loading={salvando}
      />

      <Text style={[formStyles.summaryTitle, { marginTop: 16, marginBottom: 8 }]}>
        Segurança
      </Text>
      <SecondaryButton
        label="Alterar senha"
        onPress={() => navigation.navigate('ChangePassword')}
      />

      <Text style={[formStyles.summaryTitle, { marginTop: 16, marginBottom: 4, color: tema.text }]}>
        Aparência
      </Text>
      {temaToggle}

      {!temLoja ? (
        <PrimaryButton
          label="Criar loja"
          onPress={() => navigation.navigate('CreateStore')}
          style={{ marginTop: 8 }}
        />
      ) : null}
    </FormScreen>
  );
}
