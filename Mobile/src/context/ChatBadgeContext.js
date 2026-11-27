import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { AppState, Platform } from 'react-native';
import { useAuth } from './AuthContext';
import {
  calcularTotalNaoLidas,
  encerrarHub,
  garantirHubBadge,
  listarConversas,
  verificarBadgeFallback,
} from '../services/chatService';
import {
  desdeAtual,
  presencaAoEntrar,
  presencaBackground,
  presencaEncerrar,
  presencaForeground,
  presencaOcultar,
  presencaPageHide,
  presencaRegistrarCheck,
  presencaUsuario,
} from '../services/chatPresencaRuntime';

const BADGE_FALLBACK_INTERVAL_MS = 60_000;
const SYNC_THROTTLE_MS = 5_000;

const ChatBadgeContext = createContext(null);

export function ChatBadgeProvider({ children }) {
  const { session, appMode } = useAuth();
  const sessionId = session?.perfil?.id ?? null;
  const modoOperacional = `${sessionId ?? ''}:${appMode}`;
  const [badgeCount, setBadgeCount] = useState(0);
  const [hubConectado, setHubConectado] = useState(false);

  const sessionRef = useRef(session);
  sessionRef.current = session;

  const flagsRef = useRef({
    mensagensTabAtiva: false,
    chatAtivo: false,
    conversaCodigoAtiva: null,
    appForeground: true,
  });

  const hubAtivoRef = useRef(false);
  const syncEmAndamentoRef = useRef(false);
  const ultimoSyncEmRef = useRef(0);

  const setMensagensTabAtiva = useCallback((ativo) => {
    flagsRef.current.mensagensTabAtiva = ativo;
  }, []);

  const setChatAtivo = useCallback((ativo, conversaCodigo = null) => {
    flagsRef.current.chatAtivo = ativo;
    flagsRef.current.conversaCodigoAtiva = ativo ? conversaCodigo : null;
  }, []);

  const atualizarFromConversas = useCallback((conversas) => {
    setBadgeCount(calcularTotalNaoLidas(conversas));
  }, []);

  const zerarBadge = useCallback(() => {
    setBadgeCount(0);
  }, []);

  const podePoll = useCallback(() => {
    const f = flagsRef.current;
    return f.appForeground && !f.mensagensTabAtiva && !f.chatAtivo;
  }, []);

  const syncFallback = useCallback(async ({ force = false } = {}) => {
    if (!sessionRef.current) return;

    const agora = Date.now();
    if (!force && agora - ultimoSyncEmRef.current < SYNC_THROTTLE_MS) return;
    if (syncEmAndamentoRef.current) return;

    syncEmAndamentoRef.current = true;
    try {
      const resultado = await verificarBadgeFallback(desdeAtual());
      setBadgeCount(resultado.total);
      ultimoSyncEmRef.current = Date.now();
      if (resultado.consultouRemoto) presencaRegistrarCheck();
    } catch {
      // mantém valor atual
    } finally {
      syncEmAndamentoRef.current = false;
    }
  }, []);

  useEffect(() => {
    const sub = AppState.addEventListener('change', (next) => {
      flagsRef.current.appForeground = next === 'active';
      if (Platform.OS !== 'web') {
        if (next === 'background') presencaBackground();
        else if (next === 'active') presencaForeground();
      }
      if (next === 'active' && sessionRef.current) {
        syncFallback();
      }
    });
    return () => sub.remove();
  }, [syncFallback]);

  useEffect(() => {
    if (Platform.OS !== 'web' || typeof document === 'undefined') return undefined;
    const aoMudarVisibilidade = () => {
      if (document.visibilityState === 'hidden') presencaOcultar();
      else presencaForeground();
    };
    document.addEventListener('visibilitychange', aoMudarVisibilidade);
    window.addEventListener('pagehide', presencaPageHide);
    return () => {
      document.removeEventListener('visibilitychange', aoMudarVisibilidade);
      window.removeEventListener('pagehide', presencaPageHide);
    };
  }, []);

  useEffect(() => {
    if (!sessionId) {
      presencaEncerrar();
      setBadgeCount(0);
      setHubConectado(false);
      return undefined;
    }

    let vivo = true;
    (async () => {
      if (presencaUsuario() !== String(sessionId)) {
        await presencaAoEntrar(sessionId);
      }
      if (!vivo) return;
      await syncFallback({ force: true });
    })();

    return () => {
      vivo = false;
    };
  }, [modoOperacional, syncFallback]);

  useEffect(() => {
    if (!sessionId) return undefined;

    let ativo = true;
    const tick = () => {
      if (!ativo || !podePoll() || hubConectado) return;
      syncFallback();
    };

    const interval = setInterval(tick, BADGE_FALLBACK_INTERVAL_MS);
    return () => {
      ativo = false;
      clearInterval(interval);
    };
  }, [modoOperacional, hubConectado, podePoll, syncFallback]);

  useEffect(() => {
    if (!sessionId) {
      hubAtivoRef.current = false;
      encerrarHub();
      setHubConectado(false);
      return undefined;
    }

    let cancelado = false;

    async function conectar() {
      await garantirHubBadge({
        onNovaMensagem: async (payload) => {
          const f = flagsRef.current;
          if (f.chatAtivo && f.conversaCodigoAtiva === payload.conversaCodigo) return;

          if (f.mensagensTabAtiva) {
            try {
              const data = await listarConversas({ force: true });
              setBadgeCount(calcularTotalNaoLidas(data));
            } catch {
              // ignora
            }
            return;
          }

          setBadgeCount((prev) => prev + (payload.incremento ?? 1));
        },
        onConnected: ({ isReconnect = false } = {}) => {
          if (cancelado) return;
          hubAtivoRef.current = true;
          setHubConectado(true);
          if (isReconnect) {
            syncFallback({ force: true });
          }
        },
        onDisconnected: () => {
          if (!cancelado) {
            hubAtivoRef.current = false;
            setHubConectado(false);
          }
        },
      });
    }

    conectar();

    return () => {
      cancelado = true;
      hubAtivoRef.current = false;
      encerrarHub();
      setHubConectado(false);
    };
  }, [modoOperacional, syncFallback]);

  return (
    <ChatBadgeContext.Provider
      value={{
        badgeCount,
        hubConectado,
        setMensagensTabAtiva,
        setChatAtivo,
        atualizarFromConversas,
        zerarBadge,
        syncBadge: syncFallback,
      }}
    >
      {children}
    </ChatBadgeContext.Provider>
  );
}

export function useChatBadge() {
  const ctx = useContext(ChatBadgeContext);
  if (!ctx) throw new Error('useChatBadge deve ser usado dentro de ChatBadgeProvider');
  return ctx;
}
