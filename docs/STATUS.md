# Wedge — Project Status & Handoff

Snapshot of where the project stands, so a fresh session can continue without
losing context. Read this together with [`ARCHITECTURE.md`](./ARCHITECTURE.md)
(the full product + technical design).

_Branch: `claude/app-review-27s2yp`._

## What this is
A price-action **training** app: trade real, anonymized historical charts one
bar at a time (no rewind, regular trading hours only) to build chart-reading
skill. Personal strategy lab first; "TikTok of price action" later. Doubles as a
portfolio piece (target: getting hired in Denmark — see stack rationale below).

## Stack (decided)
- **Frontend:** React Native + TypeScript (Expo), charts hand-drawn on
  `@shopify/react-native-skia`. Cross-platform iOS/Android + web.
- **Backend:** .NET 8 / C# Web API, EF Core + SQLite.
- Rationale: RN+TS is the most hireable mobile stack in Denmark; .NET backend
  reuses existing C# and covers the enterprise pole. Custom Skia (not a charting
  lib) chosen for visual control + performance in a future swipe feed — both
  conclusions backed by research (see chat history).

## What's built & verified
**Backend (tested end-to-end):**
- `ChartService` — deals anonymized RTH charts (sequence-indexed bars, no
  date/symbol) + computes magnet levels (prior day/week/month OHLC, gap).
- `ScoringService` — R-multiples vs the original stop, compounding Bankroll
  ($10k start, fixed fractional risk), confidence-bounded Edge
  (`meanR − 1.64·stderr` per chart-day) with maturity gating (≥30 trades, ≥10
  charts).
- `JourneyService` — create/rename/archive journeys, submit trades (R recomputed
  server-side), pooled stats. Archiving is tombstoned (trades stay counted).
- Endpoints under `ChartController` / `JourneyController`. Verified against a
  seeded DB: scoring math exact, magnets/gap correct, pooled identity works.

**Frontend (typechecks, web-bundles, renders live):**
- Typed API client mirroring backend DTOs.
- `CandleChart` (Skia): **black & white candles** — up = white body, down =
  black body, black outline on each; on a **white** chart background; blue EMA;
  magnet levels; entry/stop lines. Reveal one bar at a time.
- Screens: Home (journeys w/ Bankroll + Edge), Trade (chart + stop stepper +
  Long/Short/Exit + bar reveal), Ladder (journeys ranked by Edge), navigation.
- Verified live in a headless browser against the backend (Home + Trade + chart
  render correctly).

## Key design decisions (don't re-litigate)
- **Scoreboard = "Wedge Edge":** three numbers from one ledger — Bankroll $
  (dopamine, personal), Edge (skill rank, luck/start-time proof), R (truth).
  Ranking is per-pattern over a **single pooled identity** (no cherry-picking
  journeys).
- **Stops:** you set the original stop (any distance, no floor); you may move the
  **live** stop during a trade; **R is always measured vs the original stop**;
  stops enforced honestly intra-bar. Tight stops aren't a cheat as long as
  intra-bar fills are honest + the chart is unrecognizable.
- **Sizing:** fixed fractional risk (1% = 1R). Conviction sizing off in V1.
- **Chart look:** classic B&W candlesticks on white (latest user direction).

## How to run locally
```bash
# backend
cd backend
dotnet run                 # serves http://localhost:5068 ; Swagger in Dev

# frontend (new terminal)
cd frontend
npm install
npx setup-skia-web         # web only: copies canvaskit.wasm
# point the app at the backend (LAN IP for a phone; localhost for web):
#   set EXPO_PUBLIC_API_URL=http://<your-ip>:5068/api
npx expo start             # scan QR with Expo Go, or press w for web
```
Backend needs `backend/appsettings.json` (gitignored) with at least:
```json
{ "AlphaVantage": { "ApiKey": "your-key" } }
```

## Data — IMPORTANT (open item)
The app currently has **no real market data** in a fresh clone. `MarketDataService`
fetches real SPY 5-min RTH bars from **Alpha Vantage**, but:
- It needs a (free) Alpha Vantage API key.
- The cloud sandbox blocks all market-data hosts (egress policy), so fetching
  must happen on a machine with network access (e.g. yours).

**Decision from research:** Alpha Vantage is the right source — it's the only
cheap provider with 10+ years of 5-min RTH intraday via the `month=YYYY-MM`
param, with `extended_hours=false` (RTH) and `adjusted` toggles. For a bulk
historical pull, pay one month of the $49.99 tier (75 req/min), pull everything,
then downgrade to free. **Redistribution caveat:** keep fetched bars as internal
app data (gitignored seed), don't commit raw OHLCV to a public repo.

**Recommended next step (not yet built):** a seed-import path + a fetch script so
real data is pulled once and loaded from a (gitignored) seed file — making the
app independent of the API/key/network at runtime.

## Suggested next steps
1. Build seed-import + fetch script; pull real SPY/QQQ + a few large caps × ~10y
   (`extended_hours=false`, `adjusted=false`), validate for gaps, store as
   gitignored seed.
2. Device-test the app on iOS/Android simulators (only web-rendered so far).
3. Finish the design pass (chart styling / theme direction still in flux).
4. Feature depth: journey rename/sandbox UI, pooled per-pattern Edge ladder,
   then the social/feed layer (M5–M6 in ARCHITECTURE.md).
