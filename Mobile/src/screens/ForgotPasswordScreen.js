import { View, Text, TextInput, Pressable, Alert } from 'react-native';
import { useState } from 'react';
import { styles } from '../style';

export default function ForgotPasswordScreen({ navigation }) {
  const [email, setEmail] = useState('');

  function handleRecover() {
    if (!email.trim()) {
      Alert.alert('Erro', 'Informe o e-mail da conta');
      return;
    }

    Alert.alert(
      'Recuperação de senha',
      'Por segurança, a senha não pode ser exibida no app. Entre em contato com o suporte ou, se já estiver logado, altere a senha nas configurações da conta.',
      [{ text: 'OK', onPress: () => navigation.goBack() }]
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.formTitle}>Recuperar Senha</Text>

      <TextInput
        style={styles.formInput}
        placeholder="Digite seu email"
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
      />

      <Pressable style={styles.formButton} onPress={handleRecover}>
        <Text style={styles.textButton}>Continuar</Text>
      </Pressable>

      <Pressable style={styles.formButton} onPress={() => navigation.goBack()}>
        <Text style={styles.textButton}>Voltar</Text>
      </Pressable>
    </View>
  );
}
