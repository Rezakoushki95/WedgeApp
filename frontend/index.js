import { registerRootComponent } from 'expo';
import { Platform } from 'react-native';

// On web, Skia needs CanvasKit (WASM) loaded BEFORE any module that imports
// Skia components is evaluated — otherwise those modules capture an undefined
// CanvasKit at import time. So we load CanvasKit first, then dynamically import
// App (and transitively the chart). On native, Skia is built in.
if (Platform.OS === 'web') {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  const { LoadSkiaWeb } = require('@shopify/react-native-skia/lib/module/web');
  LoadSkiaWeb({ locateFile: () => '/canvaskit.wasm' })
    .then(async () => {
      const App = (await import('./App')).default;
      registerRootComponent(App);
    })
    .catch((e) => {
      // eslint-disable-next-line no-console
      console.error('Failed to load CanvasKit', e);
    });
} else {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  const App = require('./App').default;
  registerRootComponent(App);
}
