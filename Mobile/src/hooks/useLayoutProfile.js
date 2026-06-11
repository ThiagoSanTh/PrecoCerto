import { Platform, useWindowDimensions } from 'react-native';

const PHONE_WEB_MAX = 768;
const DESKTOP_WEB_MIN = 1024;

export function useLayoutProfile() {
  const { width } = useWindowDimensions();
  const isWeb = Platform.OS === 'web';
  const isNative = !isWeb;
  const isPhoneWeb = isWeb && width < PHONE_WEB_MAX;
  const isDesktopWeb = isWeb && width >= DESKTOP_WEB_MIN;
  const isTabletWeb = isWeb && width >= PHONE_WEB_MAX && width < DESKTOP_WEB_MIN;

  return {
    width,
    isWeb,
    isNative,
    isPhoneWeb,
    isTabletWeb,
    isDesktopWeb,
    contentMaxWidth: isDesktopWeb ? 1200 : isPhoneWeb ? 480 : 720,
    authCardMaxWidth: 440,
    sidebarWidth: 220,
    gridColumns: isDesktopWeb ? 4 : isPhoneWeb ? 2 : 3,
  };
}
