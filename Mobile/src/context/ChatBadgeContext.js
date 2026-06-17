import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { AppState } from 'react-native';
import { useAuth } from './AuthContext';
import {
  calcularTotalNaoLidas,
  encerrarHub,
  garantirHubBadge,
  listarConversas,
  verificarBadgeFallback,
} from '../services/chatService';

const BADGE_FALLBACK_INTERVAL_MS = 60_000;
const SYNC_THROTTLE_MS = 5_000;

const ChatBadgeContext = createContext(null);

export function ChatBadgeProvider({ children }) {
  const { session } = useAuth();
  const [badgeCount, setBadgeCount] = useState(0);
  const [hubConectado, setHubConectado] = useState(false);

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

  const syncFallback = useCallback(
    async ({ force = false } = {}) => {
      if (!session) return;

      const agora = Date.now();
      if (!force && agora - ultimoSyncEmRef.current < SYNC_THROTTLE_MS) return;
      if (syncEmAndamentoRef.current) return;

      syncEmAndamentoRef.current = true;
      try {
        const total = await verificarBadgeFallback();
        setBadgeCount(total);
        ultimoSyncEmRef.current = Date.now();
      } catch {
        // mantém valor atual
      } finally {
        syncEmAndamentoRef.current = false;
      }
    },
    [session]
  );

  useEffect(() => {
    const sub = AppState.addEventListener('change', (next) => {
      flagsRef.current.appForeground = next === 'active';
      if (next === 'active' && session) {
        syncFallback();
      }
    });
    return () => sub.remove();
  }, [session, syncFallback]);

  useEffect(() => {
    if (!session) {
      setBadgeCount(0);
      setHubConectado(false);
      return;
    }
    syncFallback({ force: true });
  }, [session, syncFallback]);

  useEffect(() => {
    if (!session) return undefined;

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
  }, [session, hubConectado, podePoll, syncFallback]);

  useEffect(() => {
    if (!session) {
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
  }, [session, syncFallback]);

  return (
    <ChatBadgeContext.Provider
      value={{
        badgeCount,
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
