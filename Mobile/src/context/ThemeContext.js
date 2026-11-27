import { createContext, useContext, useEffect, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { lightColors, darkColors } from '../theme';
import { applyDocumentChrome, readSavedTheme, THEME_STORAGE_KEY } from '../theme/webChrome';

const ThemeContext = createContext(null);

export function ThemeProvider({ children }) {
  const [tema, setTema] = useState(readSavedTheme);

  useEffect(() => {
    (async () => {
      try {
        const salvo = await AsyncStorage.getItem(THEME_STORAGE_KEY);
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
      await AsyncStorage.setItem(THEME_STORAGE_KEY, novo);
    } catch {
      /* ignora falha de persistência */
    }
  }

  const colors = tema === 'dark' ? darkColors : lightColors;

  useEffect(() => {
    applyDocumentChrome(tema);
  }, [tema]);

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
