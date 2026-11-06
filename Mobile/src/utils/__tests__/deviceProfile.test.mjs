import test from 'node:test';
import assert from 'node:assert/strict';
import { computeDeviceLayoutProfile } from '../deviceProfile.js';

test('detecta Compact Phone Android nativo (< 380px) com alvo de toque 48px e Bottom Pill Bar', () => {
  const p = computeDeviceLayoutProfile({
    platformOS: 'android',
    width: 360,
    height: 740,
  });

  assert.equal(p.os, 'android');
  assert.equal(p.isAndroid, true);
  assert.equal(p.isNative, true);
  assert.equal(p.isPhone, true);
  assert.equal(p.isCompactPhone, true);
  assert.equal(p.deviceCategory, 'compactPhone');
  assert.equal(p.useSidebarNav, false);
  assert.equal(p.useSplitView, false);
  assert.equal(p.useTableView, false);
  assert.equal(p.minTouchTarget, 48);
  assert.equal(p.bodyHorizontalPadding, 12);
});

test('detecta iPhone iOS nativo (390x844) com alvo 44px, keyboardBehavior padding e Bottom Pill Bar', () => {
  const p = computeDeviceLayoutProfile({
    platformOS: 'ios',
    width: 390,
    height: 844,
  });

  assert.equal(p.os, 'ios');
  assert.equal(p.isIOS, true);
  assert.equal(p.isPhone, true);
  assert.equal(p.isCompactPhone, false);
  assert.equal(p.deviceCategory, 'phone');
  assert.equal(p.useSidebarNav, false);
  assert.equal(p.minTouchTarget, 44);
  assert.equal(p.keyboardBehavior, 'padding');
  assert.equal(p.bodyHorizontalPadding, 16);
});

test('detecta Mobile Web no Safari iOS via userAgent e adapta como touch iOS', () => {
  const p = computeDeviceLayoutProfile({
    platformOS: 'web',
    width: 393,
    height: 852,
    userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1',
    hasCoarsePointer: true,
    maxTouchPoints: 5,
  });

  assert.equal(p.isWeb, true);
  assert.equal(p.isMobileWeb, true);
  assert.equal(p.os, 'ios');
  assert.equal(p.isTouch, true);
  assert.equal(p.minTouchTarget, 44);
  assert.equal(p.useSidebarNav, false);
});

test('adapta Tablet em Retrato (768x1024): ativa sidebar compacta (208px) mas DESATIVA split-view e tabela de 6 colunas para nao esmagar conteudo', () => {
  const p = computeDeviceLayoutProfile({
    platformOS: 'web',
    width: 768,
    height: 1024,
    userAgent: 'Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X)',
    hasCoarsePointer: true,
    maxTouchPoints: 5,
  });

  assert.equal(p.deviceCategory, 'tabletPortrait');
  assert.equal(p.isTablet, true);
  assert.equal(p.isTabletPortrait, true);
  assert.equal(p.useSidebarNav, true);
  assert.equal(p.isCompactSidebar, true);
  assert.equal(p.sidebarWidth, 208);
  assert.equal(p.availableContentWidth, 560);
  assert.equal(p.useSplitView, false);
  assert.equal(p.useTableView, false);
});

test('adapta Tablet Nativo em Paisagem (1080x810): ativa sidebar 256px, split-view e table-view com alvos touch', () => {
  const p = computeDeviceLayoutProfile({
    platformOS: 'ios',
    width: 1080,
    height: 810,
  });

  assert.equal(p.deviceCategory, 'tabletLandscape');
  assert.equal(p.isTablet, true);
  assert.equal(p.isLandscape, true);
  assert.equal(p.useSidebarNav, true);
  assert.equal(p.sidebarWidth, 256);
  assert.equal(p.availableContentWidth, 824);
  assert.equal(p.useSplitView, true);
  assert.equal(p.useTableView, true);
  assert.equal(p.minTouchTarget, 44);
});

test('detecta Desktop Web (1440x900) com mouse/hover, split-view e table-view ativos', () => {
  const p = computeDeviceLayoutProfile({
    platformOS: 'web',
    width: 1440,
    height: 900,
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/125.0.0.0',
    hasFinePointer: true,
    hasHoverCapability: true,
    maxTouchPoints: 0,
  });

  assert.equal(p.os, 'windows');
  assert.equal(p.deviceCategory, 'wideDesktop');
  assert.equal(p.isDesktopWeb, true);
  assert.equal(p.hasHover, true);
  assert.equal(p.isTouch, false);
  assert.equal(p.minTouchTarget, 36);
  assert.equal(p.useSidebarNav, true);
  assert.equal(p.useSplitView, true);
  assert.equal(p.useTableView, true);
});
