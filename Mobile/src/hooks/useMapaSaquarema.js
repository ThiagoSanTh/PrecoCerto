import { useCallback, useEffect, useRef } from 'react';
import {
  LIMITES_NORDESTE_SAQUAREMA,
  LIMITES_SUDOESTE_SAQUAREMA,
  regiaoEscapou,
  regiaoInicialSaquarema,
  travarRegiaoEmSaquarema,
} from '../utils/saquaremaLock';

/**
 * Mantém o MapView dentro de Saquarema: limites nativos e correção da câmera.
 */
export function useMapaSaquarema() {
  const mapRef = useRef(null);
  const travandoRef = useRef(false);

  const aplicarLimites = useCallback(() => {
    mapRef.current?.setMapBoundaries?.(
      LIMITES_NORDESTE_SAQUAREMA,
      LIMITES_SUDOESTE_SAQUAREMA
    );
  }, []);

  useEffect(() => {
    aplicarLimites();
  }, [aplicarLimites]);

  const mostrarRegiao = useCallback((region) => {
    const travada = travarRegiaoEmSaquarema(region);
    mapRef.current?.animateToRegion?.(travada, 350);
  }, []);

  const onRegionChangeComplete = useCallback((region) => {
    const travada = travarRegiaoEmSaquarema(region);
    if (!regiaoEscapou(region, travada) || travandoRef.current) return;
    travandoRef.current = true;
    mapRef.current?.animateToRegion?.(travada, 180);
    setTimeout(() => {
      travandoRef.current = false;
    }, 420);
  }, []);

  return {
    mapRef,
    aplicarLimites,
    mostrarRegiao,
    onRegionChangeComplete,
    regiaoInicial: regiaoInicialSaquarema(),
  };
}
