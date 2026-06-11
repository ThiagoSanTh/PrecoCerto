import { Platform, useWindowDimensions } from 'react-native';

const PHONE_MAX = 768;
const DESKTOP_MIN = 1024;
const CONTENT_WIDTH_PERCENT = 0.8;

const TYPOGRAPHY_SCALE = {
  nativePhone: 1.0,
  nativeTablet: 1.1,
  webPhone: 1.0,
  webTablet: 1.1,
  webDesktop: 1.15,
};

function resolveLayoutProfile({ isWeb, isNative, width }) {
  if (isWeb) {
    if (width >= DESKTOP_MIN) return 'webDesktop';
    if (width >= PHONE_MAX) return 'webTablet';
    return 'webPhone';
  }
  if (width >= PHONE_MAX) return 'nativeTablet';
  return 'nativePhone';
}

export function useLayoutProfile() {
  const { width } = useWindowDimensions();
  const isWeb = Platform.OS === 'web';
  const isNative = !isWeb;
  const isPhoneWeb = isWeb && width < PHONE_MAX;
  const isDesktopWeb = isWeb && width >= DESKTOP_MIN;
  const isTabletWeb = isWeb && width >= PHONE_MAX && width < DESKTOP_MIN;
  const isPhone = width < PHONE_MAX;
  const layoutProfile = resolveLayoutProfile({ isWeb, isNative, width });
  const typographyScale = TYPOGRAPHY_SCALE[layoutProfile] ?? 1.0;
  const contentWidthPercent = CONTENT_WIDTH_PERCENT;
  const contentWidth = Math.round(width * CONTENT_WIDTH_PERCENT);
  const useTopNav = isDesktopWeb || isTabletWeb;

  return {
    width,
    isWeb,
    isNative,
    isPhoneWeb,
    isTabletWeb,
    isDesktopWeb,
    isPhone,
    layoutProfile,
    typographyScale,
    contentWidthPercent,
    contentWidth,
    contentMaxWidth: isDesktopWeb ? 960 : isPhone ? Math.round(width * CONTENT_WIDTH_PERCENT) : 720,
    authCardMaxWidth: 440,
    sidebarWidth: 220,
    gridColumns: width >= DESKTOP_MIN ? 4 : width >= PHONE_MAX ? 3 : 2,
    useTopNav,
  };
}
