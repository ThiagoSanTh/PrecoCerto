import { useState } from 'react';

import { solicitarRecuperacaoSenha, redefinirSenha } from '../../services/authService';

import { mapApiError } from '../../utils/apiErrorUtils';

import { isEmailValido } from '../../utils/validacaoUtils';

import { FormScreen, FormField, PrimaryButton, SecondaryButton } from '../../components/form';



export default function ForgotPasswordScreen({ navigation }) {

  const [email, setEmail] = useState('');

  const [token, setToken] = useState('');

  const [novaSenha, setNovaSenha] = useState('');

  const [modoToken, setModoToken] = useState(false);

  const [loading, setLoading] = useState(false);

  const [error, setError] = useState(null);

  const [success, setSuccess] = useState(null);



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

    <FormScreen

      title="Recuperar senha"

      subtitle={modoToken ? 'Informe o token recebido por e-mail' : 'Informe o e-mail da conta'}

      onBack={() => navigation.goBack()}

      webVariant="auth"

      error={banner}

      footer={

        modoToken ? (

          <PrimaryButton label="Redefinir senha" onPress={handleRedefinir} loading={loading} />

        ) : (

          <>

            <PrimaryButton label="Solicitar recuperação" onPress={handleSolicitar} loading={loading} />

            <SecondaryButton label="Já tenho o token" onPress={() => setModoToken(true)} />

          </>

        )

      }

    >

      {!modoToken ? (

        <FormField

          label="E-mail"

          value={email}

          onChangeText={setEmail}

          autoCapitalize="none"

          keyboardType="email-address"

        />

      ) : (

        <>

          <FormField label="Token" value={token} onChangeText={setToken} autoCapitalize="none" />

          <FormField label="Nova senha" value={novaSenha} onChangeText={setNovaSenha} secureTextEntry />

        </>

      )}

    </FormScreen>

  );

}

