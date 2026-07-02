// Web entry. Skia needs CanvasKit (WASM) loaded BEFORE any module that imports
// Skia components is evaluated — otherwise those modules capture an undefined
// CanvasKit at import time. So we load CanvasKit first, then dynamically import
// App (and transitively the chart). The native entry is index.js.
import { registerRootComponent } from 'expo';
import { LoadSkiaWeb } from '@shopify/react-native-skia/lib/module/web';

LoadSkiaWeb({ locateFile: () => '/canvaskit.wasm' })
  .then(async () => {
    const App = (await import('./App')).default;
    registerRootComponent(App);
  })
  .catch((e) => {
    // eslint-disable-next-line no-console
    console.error('Failed to load CanvasKit', e);
  });
