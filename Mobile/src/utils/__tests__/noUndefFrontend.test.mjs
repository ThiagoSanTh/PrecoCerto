import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MOBILE_ROOT = path.resolve(__dirname, '../../..');
const require = createRequire(path.join(MOBILE_ROOT, 'package.json'));
const babel = require('@babel/core');

const ALLOWED_GLOBALS = new Set([
  'window',
  'document',
  'navigator',
  'localStorage',
  'sessionStorage',
  'caches',
  'fetch',
  'URL',
  'URLSearchParams',
  'FormData',
  'Blob',
  'File',
  'FileReader',
  'AbortController',
  'Headers',
  'Request',
  'Response',
  'WebSocket',
  'setTimeout',
  'clearTimeout',
  'setInterval',
  'clearInterval',
  'requestAnimationFrame',
  'cancelAnimationFrame',
  'queueMicrotask',
  'console',
  'process',
  'global',
  'globalThis',
  '__DEV__',
  'Buffer',
  'atob',
  'btoa',
  'crypto',
  'Intl',
  'Math',
  'Date',
  'JSON',
  'Object',
  'Array',
  'String',
  'Number',
  'Boolean',
  'Symbol',
  'BigInt',
  'RegExp',
  'Error',
  'TypeError',
  'RangeError',
  'SyntaxError',
  'URIError',
  'EvalError',
  'ReferenceError',
  'Uint8Array',
  'TextDecoder',
  'TextEncoder',
  'Promise',
  'Map',
  'Set',
  'WeakMap',
  'WeakSet',
  'Proxy',
  'Reflect',
  'parseInt',
  'parseFloat',
  'isNaN',
  'isFinite',
  'encodeURI',
  'decodeURI',
  'encodeURIComponent',
  'decodeURIComponent',
  'undefined',
  'NaN',
  'Infinity',
  'alert',
  'confirm',
  'require',
  'CustomEvent',
  'Event',
  'MessageChannel',
  'performance',
  'IntersectionObserver',
  'ResizeObserver',
  'MutationObserver',
]);

function listarArquivosJs(dir) {
  const resultado = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === 'node_modules' || entry.name === 'dist' || entry.name === '__tests__') {
      continue;
    }
    const completo = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      resultado.push(...listarArquivosJs(completo));
    } else if (/\.(js|jsx)$/.test(entry.name) && !entry.name.endsWith('.test.js')) {
      resultado.push(completo);
    }
  }
  return resultado;
}

test('nenhum componente, tela ou serviço do frontend referencia identificadores não declarados (evita ReferenceError e tela branca)', () => {
  const arquivos = [
    path.join(MOBILE_ROOT, 'App.js'),
    path.join(MOBILE_ROOT, 'index.js'),
    ...listarArquivosJs(path.join(MOBILE_ROOT, 'src')),
  ];

  const naoDeclarados = [];

  for (const arquivo of arquivos) {
    const codigo = fs.readFileSync(arquivo, 'utf8');
    const rel = path.relative(MOBILE_ROOT, arquivo);
    babel.transformSync(codigo, {
      filename: arquivo,
      cwd: MOBILE_ROOT,
      presets: [['babel-preset-expo', { jsxRuntime: 'automatic' }]],
      plugins: [
        function pluginDetectGlobals() {
          return {
            visitor: {
              Program(programPath) {
                const globals = programPath.scope.globals;
                for (const nome of Object.keys(globals)) {
                  if (!ALLOWED_GLOBALS.has(nome)) {
                    const loc = globals[nome].loc?.start;
                    naoDeclarados.push(
                      `${rel}:${loc?.line ?? '?'}:${loc?.column ?? '?'} -> "${nome}" não declarado`
                    );
                  }
                }
              },
            },
          };
        },
      ],
    });
  }

  assert.deepEqual(naoDeclarados, []);
});
