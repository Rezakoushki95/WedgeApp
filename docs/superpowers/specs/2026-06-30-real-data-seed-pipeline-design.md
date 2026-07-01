# Real-Data Seed Pipeline — Design

_Date: 2026-06-30 · Branch: `claude/app-review-27s2yp`_

## Goal

Pull real SPY 5-minute regular-trading-hours (RTH) bars **once**, persist them as a
durable, gitignored on-disk cache, and rebuild the SQLite database from that cache
**offline**. A fresh clone must run with **no Alpha Vantage API key and no network**
at runtime — the runtime random-month fetch is removed.

## Why (plain terms)

- A free Alpha Vantage key allows **25 requests/day, 5/min**. Ten years of SPY at
  5-min = ~120 month-requests = roughly **5 days** of running daily to collect it all.
- If that data lived only inside the database, any schema reset or DB wipe would force
  another ~5-day re-download. Unacceptable.
- So every download is written verbatim to a local cache folder (`data/raw/`). That
  folder is the permanent source of truth: the DB can be rebuilt from it in seconds,
  offline, as many times as we like.
- The cache is **gitignored** because (1) Alpha Vantage permits use but not
  redistribution — committing raw OHLCV to a public repo would republish it — and
  (2) it is data, not code; git holds the code that fetches/loads the data.

**One line:** download the expensive data once, keep a permanent local copy so we never
re-download, and keep that copy off GitHub.

## Architecture

Refactor the current `MarketDataService` (which conflates fetch + parse + store) into
two single-purpose units, driven by a CLI command mode added to `Program.cs`.

### 1. `MarketDataFetcher` — network only
For each requested `(symbol, month)`:
- If `data/raw/{symbol}-{yyyy-MM}.json` already exists, **skip** it.
- Otherwise call Alpha Vantage and write the **verbatim** response body to that file.

Constraints:
- Throttle to **≤5 requests/min**.
- Stop the run cleanly after **25 successful fetches** (the daily cap).
- Knows nothing about EF Core / the database.

### 2. `MarketDataImporter` — disk → DB, fully offline
- Reads every `data/raw/*.json`.
- Parses via the existing `MarketDataResponse` model.
- Normalizes to **real prices** — the current `Open*10` (and High/Low/Close ×10)
  scaling is **removed**; true OHLCV is stored.
- Upserts into `MarketDataMonths → MarketDataDays → FiveMinuteBars`.
- **Idempotent**: a month already present in the DB is skipped, so re-running is safe
  and re-import after a schema reset costs nothing.

## CLI surface (on the backend project)

| Command | Effect |
|---|---|
| `dotnet run -- fetch SPY 2014 2024` | Fill the raw cache for symbol/range. Resumable, respects the daily cap. Prints `fetched N, skipped M, cap reached / range complete`. |
| `dotnet run -- import` | Rebuild the DB from the raw cache. |
| `dotnet run` (no args) | Run the web app as today. |

## Data flow

```
Alpha Vantage
  → MarketDataFetcher
    → data/raw/SPY-2020-03.json   (gitignored; source of truth)
      → MarketDataImporter
        → WedgeApp.db             (disposable, derived)
          → ChartService
```

## Touchpoints to change

- **`Program.cs`**
  - Add arg-based command dispatch (`fetch`, `import`, default = web app).
  - On web startup, when the DB is empty, **import from the cache if present** instead
    of fetching a random month over the network. No network at runtime, ever.
- **Remove the runtime fetch** from startup: `EnsureInitialMonthlyData` /
  `FetchRandomHistoricalMonth` no longer run on web boot.
- **`MarketDataController`** — the live-fetch POST endpoints (`fetch-random-month`,
  `fetch-next-unique-month`) are **removed**. They are "testing only" per their own
  comment and re-spend the scarce daily budget unpredictably.
- **`appsettings`** — fetcher reads `AlphaVantage:ApiKey`; importer requires no key.
  (`appsettings.json` is gitignored; the key lives there locally.)

## Error handling

- **Fetcher:** on a non-200 response, an Alpha Vantage rate-limit `Note`/`Information`
  payload, or an empty `Time Series (5min)`, log the reason and **stop the run** —
  without writing a partial or garbage file, so resume stays clean. Never overwrite an
  existing good cache file.
- **Importer:** skip and log any unparseable or empty file rather than aborting the
  whole import.

## Testing

- **Importer unit test:** feed a small captured SPY month fixture (real JSON, trimmed)
  → assert bar count, **real (un-scaled) prices**, correct per-day grouping, and
  idempotency (importing twice yields the same rows).
- **Fetcher unit test:** with a stubbed `HttpClient` (no live calls), verify the
  cache-skip behavior and that the run stops at the 25-fetch daily cap.

## Default pull scope

**SPY only, last ~10 years.** ~120 requests ≈ 5 days on the free 25/day cap. The
fetcher is parameterized by symbol + year range, so this is just the recommended
starting target.

## Out of scope (YAGNI)

- Multi-symbol schema (a `Symbol` column on `MarketDataMonth`) — SPY-only for now; the
  cache filenames already carry the symbol, so this is an additive change later.
- QQQ / large-cap pulls.
- Adjusted-price handling (`adjusted=false` retained).
