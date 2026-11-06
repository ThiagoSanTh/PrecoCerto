import { useMemo } from 'react';
import { createFormStyles, formStyles as baseFormStyles } from '../components/form/formStyles';
import { useLayoutProfile } from './useLayoutProfile';
import { useTheme } from '../context/ThemeContext';

export function useFormStyles() {
  const { typographyScale } = useLayoutProfile();
  const { colors } = useTheme();
  return useMemo(
    () => createFormStyles(typographyScale, colors),
    [typographyScale, colors]
  );
}

/** Estilos estáticos legados (sem escala) — preferir useFormStyles() em componentes. */
export { baseFormStyles as formStyles };
