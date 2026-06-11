import { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { atualizarLocalizacao } from '../services/clienteService';
import { obterLocalizacaoAtual } from '../services/locationService';
import { clearToken, getToken } from '../services/tokenStorage';

const AuthContext = createContext(null);

const SESSION_KEY = '@session';
const MODE_KEY = '@userMode';

export function podeUsarModoLoja(session) {
  if (!session) return false;
  return (
    session.tipo === 'lojista' ||
    session.tipo === 'vendedor' ||
    !!session.perfil?.lojaId
  );
}

function modoPadraoParaSessao(session) {
  if (!session) return 'user';
  return session.tipo === 'lojista' || session.tipo === 'vendedor' ? 'store' : 'user';
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(null);
  const [appMode, setAppModeState] = useState('user');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    carregarSessao();
  }, []);

  async function carregarSessao() {
    try {
      const token = await getToken();
      const raw = await AsyncStorage.getItem(SESSION_KEY);
      const savedMode = await AsyncStorage.getItem(MODE_KEY);

      if (token && raw) {
        const parsed = JSON.parse(raw);
        setSession(parsed);

        const modoValido =
          savedMode === 'store' && podeUsarModoLoja(parsed)
            ? 'store'
            : savedMode === 'user'
              ? 'user'
              : modoPadraoParaSessao(parsed);

        setAppModeState(modoValido);
        if (modoValido !== savedMode) {
          await AsyncStorage.setItem(MODE_KEY, modoValido);
        }
      } else if (!token) {
        await AsyncStorage.multiRemove([SESSION_KEY, MODE_KEY]);
        setAppModeState('user');
      }
    } finally {
      setLoading(false);
    }
  }

  async function setAppMode(modo) {
    const modoFinal = modo === 'store' && podeUsarModoLoja(session) ? 'store' : 'user';
    await AsyncStorage.setItem(MODE_KEY, modoFinal);
    setAppModeState(modoFinal);
  }

  async function salvarSessao(novaSessao, modo) {
    await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(novaSessao));
    setSession(novaSessao);

    if (modo !== undefined) {
      const modoFinal =
        modo === 'store' && podeUsarModoLoja(novaSessao)
          ? 'store'
          : modo === 'user'
            ? 'user'
            : modoPadraoParaSessao(novaSessao);
      await AsyncStorage.setItem(MODE_KEY, modoFinal);
      setAppModeState(modoFinal);
    }
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

    await salvarSessao(novaSessao, modo === null ? undefined : modo);
  }

  async function logout() {
    await clearToken();
    await AsyncStorage.multiRemove([SESSION_KEY, MODE_KEY]);
    setSession(null);
    setAppModeState('user');
  }

  async function sincronizarGpsCliente(clienteIdOverride = null) {
    const id = clienteIdOverride ?? session?.perfil?.id;
    if (!id || (session?.tipo !== 'cliente' && !clienteIdOverride)) return null;

    try {
      const { latitude, longitude } = await obterLocalizacaoAtual();
      await atualizarLocalizacao(id, latitude, longitude);

      if (session?.tipo === 'cliente') {
        const perfilAtualizado = {
          ...session.perfil,
          latitudeAtual: latitude,
          longitudeAtual: longitude,
        };
        const novaSessao = { ...session, perfil: perfilAtualizado };
        await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(novaSessao));
        setSession(novaSessao);
      }

      return { latitude, longitude };
    } catch (error) {
      console.warn('GPS:', error.message);
      return null;
    }
  }

  const temModoLoja = podeUsarModoLoja(session);
  const emModoLoja = appMode === 'store' && temModoLoja;

  return (
    <AuthContext.Provider
      value={{
        session,
        loading,
        appMode,
        emModoLoja,
        temModoLoja,
        setAppMode,
        salvarSessao,
        atualizarPerfilSessao,
        logout,
        sincronizarGpsCliente,
        isCliente: session?.tipo === 'cliente',
        isLojista: session?.tipo === 'lojista',
        isVendedor: session?.tipo === 'vendedor',
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
