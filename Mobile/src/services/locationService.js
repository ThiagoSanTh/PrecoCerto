import * as Location from 'expo-location';

const GPS_TIMEOUT_MS = 8000;
const CACHE_MAX_AGE_MS = 5 * 60 * 1000;

/**
 * Solicita permissão e obtém coordenadas do dispositivo.
 * Por padrão usa a última posição conhecida (rápido) e só espera o GPS
 * se não houver cache. Cadastro de loja deve passar { allowCached: false }.
 */
export async function obterLocalizacaoAtual({ allowCached = true } = {}) {
  const { status } = await Location.requestForegroundPermissionsAsync();

  if (status !== 'granted') {
    throw new Error(
      'Permissão de localização negada. Ative o GPS para usar o Preço Certo.'
    );
  }

  if (allowCached) {
    try {
      const last = await Location.getLastKnownPositionAsync({
        maxAge: CACHE_MAX_AGE_MS,
      });
      if (last?.coords) {
        return {
          latitude: last.coords.latitude,
          longitude: last.coords.longitude,
        };
      }
    } catch {
      /* segue para GPS atual */
    }
  }

  const position = await Promise.race([
    Location.getCurrentPositionAsync({
      accuracy: Location.Accuracy.Balanced,
    }),
    new Promise((_, reject) => {
      setTimeout(() => reject(new Error('GPS timeout')), GPS_TIMEOUT_MS);
    }),
  ]);

  return {
    latitude: position.coords.latitude,
    longitude: position.coords.longitude,
  };
}
