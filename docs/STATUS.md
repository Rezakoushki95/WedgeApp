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
- **Data seed pipeline** (`MarketDataFetcher` + `MarketDataImporter` + a backend
  CLI) — pulls real SPY 5-min RTH bars from Alpha Vantage once into a gitignored
  raw cache (`backend/data/raw/`), then rebuilds SQLite from that cache offline.
  Real un-scaled prices, idempotent + atomic import, free-tier-aware fetch. 11
  unit tests (real in-memory SQLite + stubbed HTTP). Replaced the old
  `MarketDataService`/`MarketDataController` runtime-fetch path. See "Data" below.
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

## Data — seed pipeline (built) + the one manual step left
A fresh clone still has **no real market data on disk** — but the pipeline to get
it is now built, and the app no longer needs the API/key/network *at runtime*.
Design/plan: `docs/superpowers/specs|plans/2026-06-30-real-data-seed-pipeline-*.md`.

**How it works (two isolated units + a CLI):**
- `MarketDataFetcher` — Alpha Vantage → **verbatim** raw JSON in the gitignored
  cache `backend/data/raw/{SYMBOL}-{yyyy-MM}.json`, one file per symbol-month.
  Free-tier aware (≤5 req/min, stops at 25/day), skips already-cached months
  (never re-fetches), and stops **without writing** on rate-limit/non-200/empty.
- `MarketDataImporter` — raw cache → SQLite, fully offline. **Real un-scaled
  prices** (the old `×10` is gone), idempotent, atomic per-month, single-symbol
  guard. The DB is a disposable artifact rebuilt from the cache.
- CLI on the backend: `dotnet run -- fetch SPY 2015 2025` and `dotnet run --
  import`; web startup imports from the cache when the DB is empty.

**The one manual step (spends real API budget — not automated):**
```bash
# backend/appsettings.json (gitignored): { "AlphaVantage": { "ApiKey": "..." } }
dotnet run --project backend -- fetch SPY 2015 2025   # resumable; ~25/day on free tier
dotnet run --project backend -- import
dotnet run --project backend                          # real SPY candles
```
Free tier = 25 req/day, so a full ~10y SPY pull is a few days of re-running
`fetch` (it skips what it already has). **Redistribution caveat:** the cache + DB
are gitignored — don't commit raw OHLCV to a public repo.

**Why Alpha Vantage:** the only cheap source with 10+ years of 5-min RTH intraday
via `month=YYYY-MM`, with `extended_hours=false` (RTH) and `adjusted` toggles. For
a one-sitting bulk pull you could pay one month of the $49.99 tier (75 req/min),
pull everything, then downgrade to free — the fetcher is parameterized either way.

## Suggested next steps
1. ✅ **Done** — seed-import + fetch CLI built (see "Data" above). Remaining:
   actually run the SPY pull on a machine with the API key (a few resumable
   free-tier days), then optionally widen to QQQ/large caps (additive — the
   cache filenames already carry the symbol; needs a `Symbol` column for the DB).
2. Device-test the app on iOS/Android simulators (only web-rendered so far).
3. Finish the design pass (chart styling / theme direction still in flux).
4. Feature depth: journey rename/sandbox UI, pooled per-pattern Edge ladder,
   then the social/feed layer (M5–M6 in ARCHITECTURE.md).
