// Native entry (iOS/Android). Skia is built into the native module — no
// CanvasKit needed. The web entry lives in index.web.js (Metro picks it for
// platform=web); keeping the web-only require out of this file matters because
// Metro bundles statically — a runtime Platform.OS guard still pulls
// canvaskit-wasm (which requires 'fs') into the native bundle and crashes it.
import { registerRootComponent } from 'expo';
import App from './App';

registerRootComponent(App);
