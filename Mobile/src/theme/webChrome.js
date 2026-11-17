import { darkColors, lightColors } from './index';

export const THEME_STORAGE_KEY = '@theme';

const FOCUS_RING = {
  dark: '#5EEAD4',
  light: '#0A5E4D',
};

const VIEWPORT_STYLE_ID = 'preco-certo-viewport-safe-area';

export function readSavedTheme() {
  if (typeof localStorage === 'undefined') return 'light';
  try {
    const saved = localStorage.getItem(THEME_STORAGE_KEY);
    if (saved === 'dark' || saved === 'light') return saved;
  } catch {
    /* storage indisponível */
  }
  return 'light';
}

export function canvasColor(theme) {
  return theme === 'dark' ? darkColors.background : lightColors.background;
}

export function applyDocumentChrome(theme) {
  if (typeof document === 'undefined') return;
  const color = canvasColor(theme);
  const root = document.documentElement;
  root.style.backgroundColor = color;
  root.style.colorScheme = theme === 'dark' ? 'dark' : 'light';
  root.style.setProperty('--app-canvas', color);
  root.style.setProperty('--focus-ring', theme === 'dark' ? FOCUS_RING.dark : FOCUS_RING.light);
  if (document.body) document.body.style.backgroundColor = color;
  const appRoot = document.getElementById('root');
  if (appRoot) appRoot.style.backgroundColor = color;

  let meta = document.querySelector('meta[name="theme-color"]');
  if (!meta) {
    meta = document.createElement('meta');
    meta.setAttribute('name', 'theme-color');
    document.head.appendChild(meta);
  }
  meta.setAttribute('content', color);
}

export function ensureWebViewport() {
  if (typeof document === 'undefined') return;
  const viewportMeta = document.querySelector('meta[name="viewport"]');
  if (viewportMeta) {
    const content = viewportMeta.getAttribute('content') || '';
    if (!content.includes('viewport-fit=cover')) {
      viewportMeta.setAttribute(
        'content',
        `${content}, viewport-fit=cover`.replace(/^,\s*/, '')
      );
    }
  }
  if (document.getElementById(VIEWPORT_STYLE_ID)) return;
  const style = document.createElement('style');
  style.id = VIEWPORT_STYLE_ID;
  style.textContent = `
    html, body, #root {
      margin: 0;
      padding: 0;
      background: var(--app-canvas, ${lightColors.background});
      overflow: hidden;
      overscroll-behavior: none;
    }
    html, body { height: 100%; }
    #root {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      max-width: 100%;
      height: var(--app-height, 100dvh);
      overflow: hidden;
    }
    :focus-visible {
      outline: 2px solid var(--focus-ring, ${FOCUS_RING.light});
      outline-offset: 2px;
    }
  `;
  document.head.appendChild(style);
}
