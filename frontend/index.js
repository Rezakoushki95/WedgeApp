import { registerRootComponent } from 'expo';
import { Platform } from 'react-native';
import App from './App';

// On web, Skia needs CanvasKit (WASM) loaded before any <Canvas> renders.
// On native it's built in, so register immediately.
if (Platform.OS === 'web') {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  const { LoadSkiaWeb } = require('@shopify/react-native-skia/lib/module/web');
  LoadSkiaWeb({ locateFile: () => '/canvaskit.wasm' })
    .then(() => registerRootComponent(App))
    .catch((e) => {
      // Surface load failures instead of a blank screen.
      console.error('Failed to load CanvasKit', e);
    });
} else {
  registerRootComponent(App);
}
