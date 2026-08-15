import { Text } from 'react-native';

import { useState } from 'react';

import { registrarCliente } from '../../services/clienteService';

import { login as authLogin } from '../../services/authService';

import { saveToken } from '../../services/tokenStorage';

import { mapApiError } from '../../utils/apiErrorUtils';

import { obterLocalizacaoAtual } from '../../services/locationService';

import { isEmailValido, isTelefoneValido } from '../../utils/validacaoUtils';

import { useAuth } from '../../context/AuthContext';

import { useFormStyles } from '../../hooks/useFormStyles';

import {

  FormScreen,

  FormField,

  PrimaryButton,

} from '../../components/form';



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

  const { salvarSessao, sincronizarGpsCliente } = useAuth();

  const formStyles = useFormStyles();



  async function concluirSessao(tipo, perfil) {

    await salvarSessao({ tipo: tipo || 'cliente', perfil }, 'user');

    navigation.replace('Home');



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

    <FormScreen

      title="Cadastro"

      subtitle="Crie sua conta no Preço Certo"

      webVariant="auth"

      onBack={() => navigation.goBack()}

      error={error}

      onErrorAction={() => navigation.navigate('Login')}

      footer={

        <PrimaryButton label="Cadastrar" onPress={handleRegister} loading={loading} />

      }

    >

      <Text style={formStyles.sectionHint}>

        Crie sua conta de usuário. Para se tornar lojista, abra uma loja com um CNPJ válido depois.

      </Text>



      <FormField label="Nome de usuário *" value={nomeUsuario} onChangeText={setNomeUsuario} />

      <FormField

        label="E-mail *"

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

        placeholder="opcional"

      />



      <FormField label="Senha *" value={senha} onChangeText={setSenha} secureTextEntry />

      <FormField

        label="Confirmar senha *"

        value={confirmarSenha}

        onChangeText={setConfirmarSenha}

        secureTextEntry

      />

    </FormScreen>

  );

}

