import { Platform } from 'react-native';
import * as Location from 'expo-location';

function obterLocalizacaoWeb() {
  return new Promise((resolve, reject) => {
    if (typeof navigator === 'undefined' || !navigator.geolocation) {
      reject(new Error('GPS indisponível neste navegador.'));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        resolve({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        });
      },
      (error) => {
        const msg =
          error.code === 1
            ? 'Permissão de localização negada. Ative o GPS para usar o Preço Certo.'
            : error.message || 'Não foi possível obter a localização.';
        reject(new Error(msg));
      },
      { enableHighAccuracy: true, timeout: 20000, maximumAge: 60000 }
    );
  });
}

/**
 * Solicita permissão e obtém coordenadas atuais do GPS do dispositivo.
 * Usado no cadastro/login do cliente — sem entrada manual de lat/long.
 */
export async function obterLocalizacaoAtual() {
  if (Platform.OS === 'web') {
    return obterLocalizacaoWeb();
  }

  const { status } = await Location.requestForegroundPermissionsAsync();

  if (status !== 'granted') {
    throw new Error(
      'Permissão de localização negada. Ative o GPS para usar o Preço Certo.'
    );
  }

  const position = await Location.getCurrentPositionAsync({
    accuracy: Location.Accuracy.Balanced,
  });

  return {
    latitude: position.coords.latitude,
    longitude: position.coords.longitude,
  };
}
