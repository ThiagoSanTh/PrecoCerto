const base = require('./app.json');

const googleMapsApiKey = process.env.EXPO_PUBLIC_GOOGLE_MAPS_API_KEY;

module.exports = {
  expo: {
    ...base.expo,
    ios: {
      ...base.expo.ios,
      infoPlist: {
        ...base.expo.ios?.infoPlist,
        NSPhotoLibraryUsageDescription:
          'O Preço Certo precisa acessar suas fotos para cadastrar imagens dos produtos.',
        NSCameraUsageDescription:
          'O Preço Certo usa a câmera para fotografar produtos da sua loja.',
      },
    },
    android: {
      ...base.expo.android,
      permissions: [
        ...(base.expo.android?.permissions || []),
        'READ_MEDIA_IMAGES',
        'CAMERA',
      ],
      ...(googleMapsApiKey
        ? {
            config: {
              ...(base.expo.android?.config || {}),
              googleMaps: { apiKey: googleMapsApiKey },
            },
          }
        : {}),
    },
    plugins: [
      ...(base.expo.plugins || []),
      [
        'expo-image-picker',
        {
          photosPermission:
            'O Preço Certo precisa acessar suas fotos para cadastrar imagens dos produtos.',
          cameraPermission:
            'O Preço Certo usa a câmera para fotografar produtos da sua loja.',
        },
      ],
    ],
  },
};
