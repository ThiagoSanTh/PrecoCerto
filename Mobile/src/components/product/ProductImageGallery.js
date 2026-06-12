import { useState } from 'react';
import {
  View,
  Image,
  ScrollView,
  Text,
  StyleSheet,
  useWindowDimensions,
} from 'react-native';
import { useLayoutProfile } from '../../hooks/useLayoutProfile';

const WEB_HORIZONTAL_PADDING = 40;
const WEB_MAX_GALLERY_WIDTH = 480;
const WEB_MAX_GALLERY_HEIGHT = 360;

function useGalleryDimensions() {
  const { width: screenWidth } = useWindowDimensions();
  const { isWeb, width, sidebarWidth, contentFullWidth } = useLayoutProfile();

  if (!isWeb) {
    const altura = screenWidth * 0.85;
    return {
      galleryWidth: screenWidth,
      galleryHeight: altura,
      resizeMode: 'cover',
      containerStyle: null,
    };
  }

  const contentAreaWidth =
    width - (contentFullWidth ? sidebarWidth : 0) - WEB_HORIZONTAL_PADDING;
  const galleryWidth = Math.min(Math.max(contentAreaWidth, 0), WEB_MAX_GALLERY_WIDTH);
  const galleryHeight = Math.min(galleryWidth * 0.75, WEB_MAX_GALLERY_HEIGHT);

  return {
    galleryWidth,
    galleryHeight,
    resizeMode: 'contain',
    containerStyle: styles.webContainer,
  };
}

export default function ProductImageGallery({ imagens = [] }) {
  const { galleryWidth, galleryHeight, resizeMode, containerStyle } = useGalleryDimensions();
  const [pagina, setPagina] = useState(0);
  const urls = imagens.filter(Boolean);

  function onScroll(e) {
    const x = e.nativeEvent.contentOffset.x;
    const index = Math.round(x / galleryWidth);
    setPagina(index);
  }

  const frameStyle = { width: galleryWidth, height: galleryHeight };
  const imageStyle = {
    width: galleryWidth,
    height: galleryHeight,
    backgroundColor: '#F1F5F9',
  };

  if (urls.length === 0) {
    return (
      <View style={[styles.placeholder, frameStyle, containerStyle]}>
        <Text style={styles.placeholderText}>Sem foto</Text>
      </View>
    );
  }

  if (urls.length === 1) {
    return (
      <View style={[frameStyle, containerStyle]}>
        <Image
          source={{ uri: urls[0] }}
          style={imageStyle}
          resizeMode={resizeMode}
        />
      </View>
    );
  }

  return (
    <View style={[frameStyle, containerStyle]}>
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
            style={imageStyle}
            resizeMode={resizeMode}
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
  webContainer: {
    alignSelf: 'center',
    borderRadius: 12,
    overflow: 'hidden',
  },
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
