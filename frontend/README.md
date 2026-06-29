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

## Web demo (working)
The app runs on web (Expo + react-native-web) and the full UI — including the
Skia candle chart + EMA — renders live against the backend, verified in a
headless Chromium. On web, Skia needs CanvasKit (WASM):

```bash
npx setup-skia-web            # copies canvaskit.wasm into public/
npm run web
```

The web entry (`index.js`) loads CanvasKit, then **dynamically imports** `App`
so the Skia modules initialise after CanvasKit is ready (a static import would
capture an undefined CanvasKit). On native, Skia is built in and `App` is
imported directly.

`scripts/shoot.js` drives Chromium (playwright-core) to render and screenshot
the running app — used to verify the live stack end to end.

## Notes
- Verified live: Home + Trade (chart, EMA, bar reveal, stop controls) render in
  a browser against the backend. Remaining step is device testing on the
  iOS/Android simulators. The backend is verified end-to-end.
