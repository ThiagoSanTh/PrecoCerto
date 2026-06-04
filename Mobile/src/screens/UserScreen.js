import { View, Text, TextInput, Pressable, Alert } from 'react-native';
import { styles } from '../style';
import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';

export default function UserScreen({ navigation }) {
  const { session, logout, isLojista } = useAuth();
  const [email, setEmail] = useState('');
  const [nomeUsuario, setNomeUsuario] = useState('');

  useEffect(() => {
    if (session?.perfil) {
      setEmail(session.perfil.email || '');
      setNomeUsuario(session.perfil.nomeUsuario || '');
    }
  }, [session]);

  async function handleLogout() {
    await logout();
  }

  async function goToStore() {
    navigation.replace('Home');
  }

  return (
    <View style={styles.container}>
      <Text style={styles.formTitle}>Meu Perfil</Text>

      <TextInput
        style={styles.formInput}
        value={nomeUsuario}
        editable={false}
        placeholder="Nome"
      />

      <TextInput
        style={styles.formInput}
        value={email}
        editable={false}
        placeholder="Email"
      />

      {isLojista ? (
        <Pressable style={styles.formButton} onPress={goToStore}>
          <Text style={styles.textButton}>Ir para Loja</Text>
        </Pressable>
      ) : (
        <Pressable
          style={styles.formButton}
          onPress={() => navigation.navigate('CreateStore')}
        >
          <Text style={styles.textButton}>Criar Loja</Text>
        </Pressable>
      )}

      <Pressable style={styles.formButton} onPress={handleLogout}>
        <Text style={styles.textButton}>Sair</Text>
      </Pressable>
    </View>
  );
}
