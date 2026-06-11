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

  const position = await Location.getCurrentPositionAsync({
    accuracy: Location.Accuracy.Balanced,
  });

  return {
    latitude: position.coords.latitude,
    longitude: position.coords.longitude,
  };
}
