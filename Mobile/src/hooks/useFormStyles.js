import { useMemo } from 'react';
import { createFormStyles, formStyles as baseFormStyles } from '../components/form/formStyles';
import { useLayoutProfile } from './useLayoutProfile';

export function useFormStyles() {
  const { typographyScale } = useLayoutProfile();
  return useMemo(() => createFormStyles(typographyScale), [typographyScale]);
}

/** Estilos estáticos legados (sem escala) — preferir useFormStyles() em componentes. */
export { baseFormStyles as formStyles };
