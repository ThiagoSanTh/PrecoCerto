import { Platform, useWindowDimensions } from 'react-native';
import { computeDeviceLayoutProfile } from '../utils/deviceProfile';

function readWebEnvironment() {
  if (Platform.OS !== 'web' || typeof window === 'undefined') {
    return {
      userAgent: '',
      hasCoarsePointer: false,
      hasFinePointer: false,
      hasHoverCapability: false,
      maxTouchPoints: 0,
      isStandalone: false,
    };
  }

  const nav = typeof navigator !== 'undefined' ? navigator : {};
  const match = (q) => Boolean(window.matchMedia?.(q)?.matches);

  return {
    userAgent: nav.userAgent || '',
    hasCoarsePointer: match('(pointer: coarse)'),
    hasFinePointer: match('(pointer: fine)'),
    hasHoverCapability: match('(hover: hover)'),
    maxTouchPoints: Number(nav.maxTouchPoints) || 0,
    isStandalone: match('(display-mode: standalone)') || Boolean(nav.standalone),
  };
}

export function useLayoutProfile() {
  const { width, height } = useWindowDimensions();
  const webEnv = readWebEnvironment();

  return computeDeviceLayoutProfile({
    platformOS: Platform.OS,
    width,
    height,
    ...webEnv,
  });
}

