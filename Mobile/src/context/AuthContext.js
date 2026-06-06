import { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { atualizarLocalizacao } from '../services/clienteService';
import { obterLocalizacaoAtual } from '../services/locationService';

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
      const raw = await AsyncStorage.getItem(SESSION_KEY);
      if (raw) setSession(JSON.parse(raw));
    } finally {
      setLoading(false);
    }
  }

  async function salvarSessao(novaSessao, modo = 'user') {
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
    setSession(null);
  }

  /** Sincroniza GPS com a API quando o perfil logado é cliente */
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
