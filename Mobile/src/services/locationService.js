import * as Location from 'expo-location';

/**
 * Solicita permissão e obtém coordenadas atuais do GPS do dispositivo.
 * Usado no cadastro/login do cliente — sem entrada manual de lat/long.
 */
export async function obterLocalizacaoAtual() {
  const { status } = await Location.requestForegroundPermissionsAsync();

  if (status !== 'granted') {
    throw new Error(
      'Permissão de localização negada. Ative o GPS para usar o Preço Certo.'
    );
  }

  const lastKnown = await Location.getLastKnownPositionAsync();
  if (lastKnown?.coords) {
    return {
      latitude: lastKnown.coords.latitude,
      longitude: lastKnown.coords.longitude,
    };
  }

  const position = await Location.getCurrentPositionAsync({
    accuracy: Location.Accuracy.Balanced,
  });

  return {
    latitude: position.coords.latitude,
    longitude: position.coords.longitude,
  };
}
