import { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { atualizarLocalizacao } from '../services/clienteService';
import { obterLocalizacaoAtual } from '../services/locationService';
import { salvarToken, removerToken, obterToken } from '../services/tokenStorage';

const AuthContext = createContext(null);

const SESSION_KEY = '@session';
const MODE_KEY = '@userMode';

export function AuthProvider({ children }) {
  const [session, setSession] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    carregarSessao();
  }, []);

  async function carregarSessao() {
    try {
      const [raw, token] = await Promise.all([
        AsyncStorage.getItem(SESSION_KEY),
        obterToken(),
      ]);

      if (raw && token) {
        setSession(JSON.parse(raw));
      } else {
        await AsyncStorage.multiRemove([SESSION_KEY, MODE_KEY]);
        await removerToken();
      }
    } finally {
      setLoading(false);
    }
  }

  async function salvarSessao(novaSessao, modo = 'user', token) {
    if (!token) throw new Error('Token JWT é obrigatório para salvar sessão.');

    await salvarToken(token);
    await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(novaSessao));
    await AsyncStorage.setItem(MODE_KEY, modo);
    setSession(novaSessao);
  }

  async function atualizarPerfilSessao(perfilAtualizado, modo = null) {
    if (!session) return;

    const novaSessao = {
      ...session,
      perfil: {
        ...session.perfil,
        ...perfilAtualizado,
      },
    };

    await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(novaSessao));
    if (modo) await AsyncStorage.setItem(MODE_KEY, modo);
    setSession(novaSessao);
  }

  async function logout() {
    await AsyncStorage.multiRemove([SESSION_KEY, MODE_KEY]);
    await removerToken();
    setSession(null);
  }

  async function sincronizarGpsCliente() {
    if (!session || session.tipo !== 'cliente' || !session.perfil?.id) return null;

    try {
      const { latitude, longitude } = await obterLocalizacaoAtual();
      await atualizarLocalizacao(session.perfil.id, latitude, longitude);

      const perfilAtualizado = {
        ...session.perfil,
        latitudeAtual: latitude,
        longitudeAtual: longitude,
      };
      const novaSessao = { ...session, perfil: perfilAtualizado };
      await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(novaSessao));
      setSession(novaSessao);
      return { latitude, longitude };
    } catch (error) {
      console.warn('GPS:', error.message);
      return null;
    }
  }

  return (
    <AuthContext.Provider
      value={{
        session,
        loading,
        salvarSessao,
        atualizarPerfilSessao,
        logout,
        sincronizarGpsCliente,
        isCliente: session?.tipo === 'cliente',
        isLojista: session?.tipo === 'lojista',
        isAuthenticated: !!session,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider');
  return ctx;
}
