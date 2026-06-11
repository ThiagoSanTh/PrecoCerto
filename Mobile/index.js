import { registerRootComponent } from 'expo';
import { Platform } from 'react-native';

import App from './App';

const SW_MIGRATION_KEY = '@sw-migration-v3';

async function prepararServiceWorkerWeb() {
  if (Platform.OS !== 'web' || typeof window === 'undefined' || !('serviceWorker' in navigator)) {
    return;
  }

  try {
    const migrado = window.localStorage.getItem(SW_MIGRATION_KEY);
    if (migrado !== 'done') {
      const registrations = await navigator.serviceWorker.getRegistrations();
      await Promise.all(registrations.map((reg) => reg.unregister()));
      if ('caches' in window) {
        const keys = await caches.keys();
        await Promise.all(keys.map((key) => caches.delete(key)));
      }
      window.localStorage.setItem(SW_MIGRATION_KEY, 'done');
    }
  } catch (err) {
    console.warn('[PWA] Falha ao limpar service worker antigo:', err);
  }

  if (!document.querySelector('link[rel="manifest"]')) {
    const link = document.createElement('link');
    link.rel = 'manifest';
    link.href = '/manifest.json';
    document.head.appendChild(link);
  }

  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/service-worker.js').catch((err) => {
      console.warn('[PWA] Service worker registration failed:', err);
    });
  });
}

prepararServiceWorkerWeb();

registerRootComponent(App);
