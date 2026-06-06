import { useState } from 'react';
import {
  View,
  Image,
  ScrollView,
  Text,
  StyleSheet,
  useWindowDimensions,
} from 'react-native';

export default function ProductImageGallery({ imagens = [] }) {
  const { width: screenWidth } = useWindowDimensions();
  const [pagina, setPagina] = useState(0);
  const urls = imagens.filter(Boolean);
  const altura = screenWidth * 0.85;

  function onScroll(e) {
    const x = e.nativeEvent.contentOffset.x;
    const index = Math.round(x / screenWidth);
    setPagina(index);
  }

  if (urls.length === 0) {
    return (
      <View style={[styles.placeholder, { width: screenWidth, height: altura }]}>
        <Text style={styles.placeholderText}>Sem foto</Text>
      </View>
    );
  }

  if (urls.length === 1) {
    return (
      <View style={{ width: screenWidth, height: altura }}>
        <Image
          source={{ uri: urls[0] }}
          style={{ width: screenWidth, height: altura, backgroundColor: '#F1F5F9' }}
          resizeMode="cover"
        />
      </View>
    );
  }

  return (
    <View style={{ width: screenWidth, height: altura }}>
      <ScrollView
        horizontal
        pagingEnabled
        nestedScrollEnabled
        showsHorizontalScrollIndicator={false}
        onScroll={onScroll}
        scrollEventThrottle={16}
        style={{ flex: 1 }}
      >
        {urls.map((url, index) => (
          <Image
            key={`${url}-${index}`}
            source={{ uri: url }}
            style={{ width: screenWidth, height: altura, backgroundColor: '#F1F5F9' }}
            resizeMode="cover"
          />
        ))}
      </ScrollView>
      <View style={styles.dots}>
        {urls.map((_, index) => (
          <View
            key={index}
            style={[styles.dot, index === pagina && styles.dotActive]}
          />
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  placeholder: {
    backgroundColor: '#F1F5F9',
    alignItems: 'center',
    justifyContent: 'center',
  },
  placeholderText: {
    color: '#94A3B8',
    fontSize: 16,
    fontWeight: '600',
  },
  dots: {
    position: 'absolute',
    bottom: 10,
    left: 0,
    right: 0,
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 6,
  },
  dot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: '#CBD5E1',
  },
  dotActive: {
    backgroundColor: '#14B8A6',
  },
});
