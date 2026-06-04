import { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
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

  async function logout() {
    await AsyncStorage.multiRemove([SESSION_KEY, MODE_KEY]);
    await removerToken();
    setSession(null);
  }

  return (
    <AuthContext.Provider
      value={{
        session,
        loading,
        salvarSessao,
        logout,
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
