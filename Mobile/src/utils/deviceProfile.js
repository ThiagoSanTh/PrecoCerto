const PHONE_MAX = 768;
const DESKTOP_MIN = 1024;
const WIDE_DESKTOP_MIN = 1440;
const SPLIT_CONTENT_MIN_WIDTH = 760;
const CONTENT_WIDTH_PERCENT = 1.0;

const TYPOGRAPHY_SCALE = {
  nativePhone: 1.0,
  nativeTablet: 1.08,
  webPhone: 1.0,
  webTablet: 1.08,
  webDesktop: 1.15,
};

export function detectRuntimeOS({ platformOS, userAgent = '', maxTouchPoints = 0 }) {
  if (platformOS === 'android') return 'android';
  if (platformOS === 'ios') return 'ios';

  const ua = String(userAgent || '').toLowerCase();
  if (ua.includes('android')) return 'android';
  if (ua.includes('iphone') || ua.includes('ipad') || ua.includes('ipod')) return 'ios';
  if (ua.includes('macintosh') && maxTouchPoints > 1) return 'ios';
  if (ua.includes('win')) return 'windows';
  if (ua.includes('mac')) return 'macos';
  if (ua.includes('linux')) return 'linux';
  return platformOS || 'unknown';
}

function resolveLegacyLayoutProfile({ isWeb, width }) {
  if (isWeb) {
    if (width >= DESKTOP_MIN) return 'webDesktop';
    if (width >= PHONE_MAX) return 'webTablet';
    return 'webPhone';
  }
  if (width >= PHONE_MAX) return 'nativeTablet';
  return 'nativePhone';
}

export function computeDeviceLayoutProfile({
  platformOS = 'web',
  width = 390,
  height = 844,
  userAgent = '',
  hasCoarsePointer = false,
  hasFinePointer = false,
  hasHoverCapability = false,
  maxTouchPoints = 0,
  isStandalone = false,
} = {}) {
  const safeWidth = Math.max(0, Number(width) || 0);
  const safeHeight = Math.max(0, Number(height) || 0);
  const isWeb = platformOS === 'web';
  const isNative = !isWeb;
  const os = detectRuntimeOS({ platformOS, userAgent, maxTouchPoints });
  const isAndroid = os === 'android';
  const isIOS = os === 'ios';
  const isMobileWeb = isWeb && (isAndroid || isIOS);
  const isPWA = Boolean(isStandalone);

  const isTouch =
    isNative ||
    isMobileWeb ||
    Boolean(hasCoarsePointer) ||
    (Number(maxTouchPoints) > 0 && !hasFinePointer);
  const hasHover = isWeb && Boolean(hasHoverCapability) && Boolean(hasFinePointer) && !isMobileWeb;

  const isLandscape = safeWidth > safeHeight && safeHeight > 0;
  const isPortrait = !isLandscape;

  const isCompactPhone = safeWidth < 380;
  const isPhone = safeWidth < PHONE_MAX;
  const isPhoneWeb = isWeb && isPhone;
  const isTabletPortrait = safeWidth >= PHONE_MAX && safeWidth < DESKTOP_MIN;
  const isTabletLandscape =
    safeWidth >= DESKTOP_MIN && (isNative || isMobileWeb || (isTouch && safeWidth < 1366));
  const isTablet = isTabletPortrait || isTabletLandscape;
  const isTabletWeb = isWeb && isTabletPortrait;
  const isDesktopWeb = isWeb && safeWidth >= DESKTOP_MIN;

  let deviceCategory = 'phone';
  if (isCompactPhone) {
    deviceCategory = 'compactPhone';
  } else if (isPhone) {
    deviceCategory = 'phone';
  } else if (isTabletPortrait) {
    deviceCategory = 'tabletPortrait';
  } else if (isTabletLandscape) {
    deviceCategory = 'tabletLandscape';
  } else if (safeWidth >= WIDE_DESKTOP_MIN) {
    deviceCategory = 'wideDesktop';
  } else {
    deviceCategory = 'desktop';
  }

  const useSidebarNav = safeWidth >= PHONE_MAX;
  const isCompactSidebar = safeWidth >= PHONE_MAX && safeWidth < DESKTOP_MIN;
  const sidebarWidth = useSidebarNav ? (isCompactSidebar ? 208 : 256) : 0;
  const availableContentWidth = Math.max(320, safeWidth - sidebarWidth);

  const useSplitView = availableContentWidth >= SPLIT_CONTENT_MIN_WIDTH;
  const useTableView = availableContentWidth >= SPLIT_CONTENT_MIN_WIDTH;

  const minTouchTarget = isAndroid ? 48 : isTouch ? 44 : 36;
  const keyboardBehavior = isIOS ? 'padding' : isAndroid && isNative ? 'height' : undefined;
  const bodyHorizontalPadding = isCompactPhone ? 12 : isPhone ? 16 : isTabletPortrait ? 20 : 24;

  const layoutProfile = resolveLegacyLayoutProfile({ isWeb, width: safeWidth });
  const typographyScale = TYPOGRAPHY_SCALE[layoutProfile] ?? 1.0;
  const contentWidthPercent = CONTENT_WIDTH_PERCENT;
  const contentWidth = Math.round(safeWidth * CONTENT_WIDTH_PERCENT);
  const contentFullWidth = useSidebarNav || isPhone;

  const gridColumns =
    availableContentWidth >= 1100
      ? 4
      : availableContentWidth >= 720
        ? 3
        : safeWidth >= 540
          ? 2
          : isCompactPhone
            ? 1
            : 2;

  return {
    width: safeWidth,
    height: safeHeight,
    os,
    isWeb,
    isNative,
    isAndroid,
    isIOS,
    isMobileWeb,
    isPWA,
    isTouch,
    hasHover,
    isLandscape,
    isPortrait,
    isPhoneWeb,
    isTabletWeb,
    isDesktopWeb,
    isPhone,
    isCompactPhone,
    isTablet,
    isTabletPortrait,
    isTabletLandscape,
    isShortScreen: safeHeight > 0 && safeHeight < 720,
    deviceCategory,
    layoutProfile,
    typographyScale,
    minTouchTarget,
    keyboardBehavior,
    bodyHorizontalPadding,
    contentWidthPercent,
    contentWidth,
    contentMaxWidth: isDesktopWeb ? 960 : isPhone ? safeWidth : 720,
    authCardMaxWidth: 440,
    sidebarWidth,
    isCompactSidebar,
    availableContentWidth,
    gridColumns,
    useSidebarNav,
    useSplitView,
    useTableView,
    contentFullWidth,
  };
}
