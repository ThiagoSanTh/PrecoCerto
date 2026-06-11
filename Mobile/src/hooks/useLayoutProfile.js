import { Platform, useWindowDimensions } from 'react-native';

const PHONE_MAX = 768;
const DESKTOP_MIN = 1024;

export function useLayoutProfile() {
  const { width } = useWindowDimensions();
  const isWeb = Platform.OS === 'web';
  const isNative = !isWeb;
  const isPhoneWeb = isWeb && width < PHONE_MAX;
  const isDesktopWeb = isWeb && width >= DESKTOP_MIN;
  const isTabletWeb = isWeb && width >= PHONE_MAX && width < DESKTOP_MIN;
  const isPhone = width < PHONE_MAX;

  return {
    width,
    isWeb,
    isNative,
    isPhoneWeb,
    isTabletWeb,
    isDesktopWeb,
    isPhone,
    contentMaxWidth: isDesktopWeb ? 1200 : isPhone ? 480 : 720,
    authCardMaxWidth: 440,
    sidebarWidth: 220,
    gridColumns: width >= DESKTOP_MIN ? 4 : width >= PHONE_MAX ? 3 : 2,
  };
}
