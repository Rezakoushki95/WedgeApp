# Wedge — Mobile App (React Native / Expo)

The cross-platform client for the Wedge price-action trainer. See
[`../docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md) for the full design.

## Stack
- **Expo + React Native + TypeScript**
- **@shopify/react-native-skia** for the GPU-rendered candle chart
- **@react-navigation** for screens

## Status — M1 scaffold
What's here:
- Typed API client (`src/api`) mirroring the backend DTOs
- `CandleChart` — naked candles + EMA + on-demand magnet levels (Skia)
- `HomeScreen` — journeys list with Bankroll $ and Edge state
- `TradeScreen` — deal a chart, reveal bar-by-bar (no rewind), Long/Short,
  move stop to breakeven, exit; live + closed P&L in R; submits to the backend
  which recomputes R authoritatively

Not yet: stop-placement UI (uses a default 0.4% original stop for now),
journey rename/archive UI, the cross-journey Edge ladder, the social layer.

## Run
```bash
npm install
# point the app at your backend (LAN IP for a device; localhost for web):
export EXPO_PUBLIC_API_URL=http://<your-machine-ip>:5068/api
npm run web        # or: npm run ios / npm run android
```

The backend (../backend) must be running and seeded with market data.

## Web demo
The app runs on web (Expo + react-native-web) and was rendered live against the
backend in a headless Chromium — the Home/list/navigation screens render
correctly. The Skia candle chart works natively (iOS/Android have Skia built
in); on **web** it additionally needs CanvasKit (WASM) loaded before the chart
mounts:

```bash
npx setup-skia-web            # copies canvaskit.wasm into public/
npm run web
```

Known web TODO: wrap the chart with `WithSkiaWeb` (lazy-load) so the chart
component resolves to the web Skia build after CanvasKit is ready — currently
`Canvas`/`Path` resolve to the native build on web and miss the loaded CanvasKit
global. Not a product blocker (native is the primary target).

`scripts/shoot.js` drives Chromium (playwright-core) to render and screenshot
the running app — used to verify the live stack.

## Notes
- The Skia chart is runtime-tested via web bundling; full device testing
  (iOS/Android simulators) is the remaining step. The backend it talks to is
  verified end-to-end.
