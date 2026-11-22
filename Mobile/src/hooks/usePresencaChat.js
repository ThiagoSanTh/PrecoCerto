import { useCallback } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { presencaBlur, presencaFoco } from '../services/chatPresencaRuntime';

export function usePresencaChat() {
  useFocusEffect(
    useCallback(() => {
      presencaFoco();
      return () => {
        presencaBlur();
      };
    }, [])
  );
}
