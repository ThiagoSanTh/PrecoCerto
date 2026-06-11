import { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { lightColors, darkColors } from '../theme';

const ThemeContext = createContext(null);

const THEME_KEY = '@theme';

export function ThemeProvider({ children }) {
  const [tema, setTema] = useState('light');

  useEffect(() => {
    (async () => {
      try {
        const salvo = await AsyncStorage.getItem(THEME_KEY);
        if (salvo === 'dark' || salvo === 'light') setTema(salvo);
      } catch {
        /* mantém padrão */
      }
    })();
  }, []);

  async function alternarTema() {
    const novo = tema === 'light' ? 'dark' : 'light';
    setTema(novo);
    try {
      await AsyncStorage.setItem(THEME_KEY, novo);
    } catch {
      /* ignora falha de persistência */
    }
  }

  const colors = tema === 'dark' ? darkColors : lightColors;

  return (
    <ThemeContext.Provider value={{ tema, colors, isDark: tema === 'dark', alternarTema }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme deve ser usado dentro de ThemeProvider');
  return ctx;
}
