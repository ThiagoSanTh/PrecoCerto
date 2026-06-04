import { StatusBar } from 'expo-status-bar';
import { Pressable, Text, View, TextInput, Alert } from 'react-native';
import { styles } from '../style';
import { useState } from 'react';
import { loginCliente } from '../services/clienteService';
import { loginLojista } from '../services/lojistaService';
import { useAuth } from '../context/AuthContext';

export default function LoginScreen({ navigation }) {
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [tipo, setTipo] = useState('cliente');
  const [loading, setLoading] = useState(false);
  const { salvarSessao } = useAuth();

  async function handleLogin() {
    if (!email || !senha) {
      Alert.alert('Erro', 'Preencha todos os campos');
      return;
    }

    setLoading(true);
    try {
      if (tipo === 'cliente') {
        const { token, perfil } = await loginCliente(email.trim(), senha);
        await salvarSessao({ tipo: 'cliente', perfil }, 'user', token);
      } else {
        const { token, perfil } = await loginLojista(email.trim(), senha);
        await salvarSessao({ tipo: 'lojista', perfil }, 'store', token);
      }
    } catch (error) {
      const msg =
        error.response?.data ||
        error.message ||
        'Não foi possível entrar. Verifique email e senha.';
      Alert.alert('Erro', String(msg));
    } finally {
      setLoading(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.formTitle}>Login</Text>

      <View style={{ flexDirection: 'row', marginBottom: 16, gap: 8 }}>
        <Pressable
          onPress={() => setTipo('cliente')}
          style={[
            styles.formButton,
            { flex: 1, backgroundColor: tipo === 'cliente' ? '#22C55E' : '#374151' },
          ]}
        >
          <Text style={styles.textButton}>Cliente</Text>
        </Pressable>
        <Pressable
          onPress={() => setTipo('lojista')}
          style={[
            styles.formButton,
            { flex: 1, backgroundColor: tipo === 'lojista' ? '#22C55E' : '#374151' },
          ]}
        >
          <Text style={styles.textButton}>Lojista</Text>
        </Pressable>
      </View>

      <TextInput
        style={styles.formInput}
        placeholder="Informe o email"
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
      />

      <TextInput
        style={styles.formInput}
        placeholder="Informe a senha"
        secureTextEntry
        value={senha}
        onChangeText={setSenha}
      />

      <Pressable
        style={[styles.formButton, loading && { opacity: 0.6 }]}
        onPress={handleLogin}
        disabled={loading}
      >
        <Text style={styles.textButton}>{loading ? 'Entrando...' : 'Logar'}</Text>
      </Pressable>

      <View style={styles.subContainer}>
        <Pressable
          onPress={() => navigation.navigate('EsqueciSenha')}
          style={styles.subButton}
        >
          <Text style={styles.subTextButton}>Esqueci a senha</Text>
        </Pressable>

        <Pressable
          onPress={() => navigation.navigate('Cadastro', { tipoInicial: tipo })}
          style={styles.subButton}
        >
          <Text style={styles.subTextButton}>Novo usuário</Text>
        </Pressable>
      </View>

      <StatusBar style="auto" />
    </View>
  );
}
