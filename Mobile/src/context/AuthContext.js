import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { atualizarLocalizacao } from '../services/clienteService';
import { obterLocalizacaoAtual } from '../services/locationService';
import { clearToken, getToken } from '../services/tokenStorage';
import { invalidarCache } from '../services/feedCache';
import {
  MODO_CLIENTE,
  MODO_LOJA,
  podeUsarModoLoja,
  modoPadraoParaSessao,
  resolverModoSalvo,
  emModoLoja as resolverEmModoLoja,
  emModoCliente as resolverEmModoCliente,
  sincronizarContextoOperacional,
  aplicarGpsNaSessao,
} from '../utils/modoUsuario';

const AuthContext = createContext(null);

const SESSION_KEY = '@session';
const MODE_KEY = '@userMode';
const GPS_THROTTLE_MS = 5 * 60 * 1000;

export { podeUsarModoLoja };

function aplicarModo(modo, session) {
  const modoFinal = modo === MODO_LOJA && podeUsarModoLoja(session) ? MODO_LOJA : MODO_CLIENTE;
  sincronizarContextoOperacional(modoFinal, session);
  invalidarCache('conversas');
  return modoFinal;
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(null);
  const [appMode, setAppModeState] = useState(MODO_CLIENTE);
  const [loading, setLoading] = useState(true);
  const sessionRef = useRef(null);
  const ultimoGpsSyncEmRef = useRef(0);

  function gravarSessaoEmMemoria(novaSessao) {
    sessionRef.current = novaSessao;
    setSession(novaSessao);
  }

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
        gravarSessaoEmMemoria(parsed);

        const modoValido = resolverModoSalvo(savedMode, parsed);
        setAppModeState(aplicarModo(modoValido, parsed));
        if (modoValido !== savedMode) {
          await AsyncStorage.setItem(MODE_KEY, modoValido);
        }
      } else if (!token) {
        await AsyncStorage.multiRemove([SESSION_KEY, MODE_KEY]);
        gravarSessaoEmMemoria(null);
        setAppModeState(aplicarModo(MODO_CLIENTE, null));
      }
    } finally {
      setLoading(false);
    }
  }

  async function setAppMode(modo) {
    const modoFinal = aplicarModo(modo, sessionRef.current);
    await AsyncStorage.setItem(MODE_KEY, modoFinal);
    setAppModeState(modoFinal);
  }

  async function salvarSessao(novaSessao, modo) {
    await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(novaSessao));
    gravarSessaoEmMemoria(novaSessao);

    if (modo !== undefined) {
      const modoFinal =
        modo === MODO_LOJA && podeUsarModoLoja(novaSessao)
          ? MODO_LOJA
          : modo === MODO_CLIENTE
            ? MODO_CLIENTE
            : modoPadraoParaSessao(novaSessao);
      setAppModeState(aplicarModo(modoFinal, novaSessao));
      await AsyncStorage.setItem(MODE_KEY, modoFinal);
    } else {
      sincronizarContextoOperacional(appMode, novaSessao);
    }
  }

  async function atualizarPerfilSessao(perfilAtualizado, modo = null) {
    const atual = sessionRef.current;
    if (!atual) return;

    const novaSessao = {
      ...atual,
      perfil: {
        ...atual.perfil,
        ...perfilAtualizado,
      },
    };

    await salvarSessao(novaSessao, modo === null ? undefined : modo);
  }

  async function logout() {
    await clearToken();
    await AsyncStorage.multiRemove([SESSION_KEY, MODE_KEY]);
    gravarSessaoEmMemoria(null);
    setAppModeState(aplicarModo(MODO_CLIENTE, null));
    ultimoGpsSyncEmRef.current = 0;
  }

  const sincronizarGpsCliente = useCallback(async (clienteIdOverride = null, { force = false } = {}) => {
    const id = clienteIdOverride ?? sessionRef.current?.perfil?.id;
    if (!id) return null;

    const agora = Date.now();
    if (!force && ultimoGpsSyncEmRef.current && agora - ultimoGpsSyncEmRef.current < GPS_THROTTLE_MS) {
      return null;
    }

    try {
      const { latitude, longitude } = await obterLocalizacaoAtual();
      await atualizarLocalizacao(id, latitude, longitude);
      ultimoGpsSyncEmRef.current = Date.now();

      const sessAtual = sessionRef.current;
      const novaSessao = aplicarGpsNaSessao(sessAtual, id, latitude, longitude);
      if (novaSessao && novaSessao !== sessAtual) {
        sessionRef.current = novaSessao;
        await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(novaSessao));
        setSession(novaSessao);
      }

      return { latitude, longitude };
    } catch (error) {
      console.warn('GPS:', error.message);
      return null;
    }
  }, []);

  const temModoLoja = podeUsarModoLoja(session);
  const emModoLoja = resolverEmModoLoja(appMode, session);
  const emModoCliente = resolverEmModoCliente(appMode, session);

  return (
    <AuthContext.Provider
      value={{
        session,
        loading,
        appMode,
        emModoLoja,
        emModoCliente,
        temModoLoja,
        setAppMode,
        salvarSessao,
        atualizarPerfilSessao,
        logout,
        sincronizarGpsCliente,
        isCliente: emModoCliente,
        isLojista: session?.tipo === 'lojista',
        isVendedor: session?.tipo === 'vendedor',
        isAdmin: String(session?.tipo ?? '').toLowerCase() === 'admin',
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
