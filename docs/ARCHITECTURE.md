# Wedge — Architecture & Product Design

> A price-action **training** app. You trade real, anonymized historical charts
> one bar at a time — no rewind, regular trading hours only — to build genuine
> chart-reading skill. Personal strategy lab first; "the TikTok of price action" later.

This document is the design of record for the rebuild. The original
Angular/Ionic + .NET app was stripped down (it became a data-ingestion backend
only); this is the v2 design that replaces it.

---

## 1. Design principles

1. **Price action is the skill.** Naked charts. The only indicator allowed is EMA.
2. **Simplicity.** Minimal UI, one thing at a time, finishable sessions.
3. **Addictive *and* honest.** Keep the visceral money-hunger dopamine, but make
   the *ranking* reward genuine reading skill, never luck or who started first.
4. **Performance-friendly & cross-platform.**

---

## 2. Core mechanics

- **Anonymized charts** — no symbol, no date. Price action reads the same
  regardless of when it happened; anonymity also stops users recognizing the tape.
- **Regular trading hours only** (RTH). Gives clean opens, gaps, and session structure.
- **Replay one bar at a time. No rewind.** The future is hidden until you advance.
- **Trade every bar** — act (enter / move stop / exit) as each bar closes.
- **Naked chart + EMA only.** Plus **magnet levels on demand** (toggle): prior
  **day / week / month** open-high-low-close, and gap levels — the things price
  gravitates to.

### Journeys
The user organizes practice into multiple **journeys**, each one dedicated to a
single pattern/strategy (e.g. bull flags, failed breakouts). Journeys can be
renamed and continued, and each shows its own score. They let you **test
strategies separately** — an honest, same-axis verdict on which patterns you can
actually read.

> Journeys are only a UI folder. For *ranking*, a user is **one pooled identity**
> (see §4) — you cannot cherry-pick your luckiest journey.

---

## 3. The scoreboard — "Wedge Edge"

Three numbers from **one** underlying ledger, so none can drift from skill:

| Number | Role | Audience |
|---|---|---|
| **Bankroll $** | The dopamine. A virtual account (starts $10k) that **compounds bar-by-bar**. Animated, hungry, personal high-score. | Personal view |
| **Edge** | The skill score. Ranks you. Luck-proof and start-time-proof. | Personal + social ranking |
| **R** | The truth underneath both. Never shown as a headline. | Internal |

- **R (unit of truth):** `R = (exit − entry) / (entry − originalStop)`, signed by
  direction. 1R = the risk defined by your **original** stop at entry.
- **Bankroll $ (dopamine):** `Bankroll = $10k × Π(1 + Rᵢ · f)` with fixed risk
  fraction `f` (default 1%). Because `f` is fixed, the dollar curve is a faithful,
  tamper-proof image of cumulative R. **Personal only — never a leaderboard axis**
  (compounding makes it luck- and longevity-sensitive).
- **Edge (ranking):** `Edge = meanR − 1.64 · stderr(R)` — the **lower confidence
  bound** on your true edge. A lucky short streak scores ~0 until the sample
  matures. `stderr` is computed on **per-chart-day aggregated R** (not per-trade),
  so 200 correlated scalps on one chart can't fake a tight interval. Gated until
  mature (n ≥ 30 trades across ≥ 10 distinct charts); mapped to a 0–100 scale.

**"Money is the candy, Edge is the report card, R is the nutrition under both."**
Chasing the dollar number and chasing real skill become the same action.

---

## 4. Fairness & anti-gaming

- **Rank on a rate, not a total.** Edge is per-trade — joining late costs nothing.
  Cumulative Bankroll $ never ranks you.
- **Confidence lower-bound.** A veteran's only legitimate advantage is a tighter
  interval (proven edge), not seniority. Rolling window keeps it current; the
  social layer resets weekly.
- **Single pooled identity, permanently tombstoned trades.** Edge is computed over
  *every* trade across *all* journeys. Deleting a journey hides the folder but its
  trades stay counted forever → no cherry-pick, no fresh-journey farming, no
  cleanup-of-losers. A labelled **Sandbox** journey type is excluded from ranking
  for safe experimentation.
- **Per-pattern boards** so comparisons are like-for-like (R already neutralizes
  instrument differences).

### Stops & risk (resolved design)
- You set an **original stop** at entry — any distance, **tight or wide, your choice**.
- You may **move the live stop during the trade** to manage it.
- **R is always measured against the original stop.** Moving the live stop changes
  *your exit*, never *the ruler*.
- **Stops are enforced honestly intra-bar:** if price touches your live stop on any
  bar (using the bar's high/low, not just the close), you're out — loss capped at
  whatever the original stop implies.
- **No volatility floor / minimum stop size.** A tight stop is a legitimate style
  choice: bigger R when it works, but it gets hit more often, and on a fair chart
  those roughly cancel — so it can't farm Edge. Honesty rests on **honest intra-bar
  fills** + an **unrecognizable chart**, not on limiting stop size.
- **Sizing:** fixed fractional risk (1% = 1R). Conviction sizing is **off in V1**.

---

## 5. Tech stack

- **Frontend:** **React Native + TypeScript**, charts via `@shopify/react-native-skia`
  (GPU-accelerated custom candle rendering + smooth swipe feed). Cross-platform
  (iOS / Android / web).
- **Backend:** **.NET / C#** (reuses the data-ingestion layer — the
  `MarketDataFetcher`/`MarketDataImporter` seed pipeline + EF/SQLite plumbing).

**Rationale (incl. the Denmark goal):** the app doubles as a portfolio piece to
help land a job in Denmark. Market research showed Denmark is React/TypeScript-heavy
in startups/product and .NET/C#-heavy in enterprise/public sector; React Native is
far more hireable there than Flutter (~dozens of React roles vs ~1 Flutter posting
in Copenhagen). RN + a .NET backend straddles both demand poles. Skia closes the
only gap (custom high-perf charting) where Flutter would have been easier.

---

## 6. Data model

```
Instrument   symbol, assetClass                      # internal only; never shown
Chart        anonymized RTH window: context bars + hidden future bars,
             precomputed magnet levels (prior D/W/M OHLC, gap), pattern tag
Bar          open, high, low, close, sequenceIndex   # index, NOT a real date
User
Journey      userId, name, patternTag, archived?     # UI folder only
Trade        # immutable ledger — the source of truth
             journeyId, userId, chartId, direction,
             entryBarIndex, entryPrice,
             originalStop (defines 1R, frozen),
             liveStop (movable),
             exitBarIndex, exitPrice, exitReason (stop|manual|EOD),
             resultR, riskFraction, createdAt
```

Trades are **append-only and tombstoned**. Edge and Bankroll are derived from the
Trade ledger (materialized per user/pattern for fast reads).

---

## 7. Replay + scoring engine

**Replay authority evolves with the product:**
- **V1 (personal):** ship the full chart to the client, reveal bars locally — simple,
  fast, fine when it's just you.
- **V2 (ranked/social):** server deals bars on demand and grades server-side — this
  is what makes the social board cheat-proof (no peeking ahead).

**Per-trade scoring loop:**
1. On each revealed bar, test the **live stop against the bar's high/low**. Touched → exit at stop.
2. On exit (stop / manual / end-of-day) compute `R` against the **original** stop.
3. Append the Trade → compound **Bankroll** by `R · f` → recompute pooled **Edge**.

Rendering (candles, EMA, magnet levels) is client-side (Skia) for 60fps; the
*reveal* of future bars is the thing guarded server-side in V2.

---

## 8. Screens (React Native)

1. **Home** — journeys as cards: Bankroll $ + equity sparkline + Edge state.
2. **Trade view** — full-screen naked chart, EMA toggle, magnet toggles;
   Long / Short / set & move stop / exit; "Next bar"; live Bankroll ticking.
3. **Trade result** — R + one-line coach note.
4. **Journey stats** — equity curve, Edge card, expectancy breakdown.
5. **Cross-journey ladder** — your strategies ranked by Edge ("Bull flags +0.4R,
   established — keep / Breakouts −0.1R — cut"). The V1 payoff.
6. **(V2) Social** — per-pattern leagues (weekly cohorts), shareable "run" clips.

---

## 9. Roadmap

| Milestone | Scope |
|---|---|
| **M0 – Data pipeline** | Ingest historical RTH bars, slice into anonymized chart windows, precompute magnet levels. *Built: a `MarketDataFetcher` (Alpha Vantage → gitignored raw cache) + `MarketDataImporter` (cache → SQLite, offline) + a backend `fetch`/`import` CLI persist real SPY 5-min RTH bars; runtime needs no API key/network. Remaining M0 work: run the real pull, then chart-window slicing + magnet precompute.* |
| **M1 – Replay + chart UI** | Client reveal, no rewind, trade every bar, EMA, magnet levels. "Can I play a chart." |
| **M2 – Ledger + stops + R + Bankroll** | Honest intra-bar stop-outs, R scoring, compounding bankroll. |
| **M3 – Journeys** | Create / rename / archive, pooled identity, Sandbox type. |
| **M4 – Edge + stats + ladder** | Confidence-bounded Edge, per-journey stats, cross-journey ladder. **→ V1 personal lab complete.** |
| **M5 – Server-authoritative dealing** | Bars dealt on demand + server grading; accounts/sync. Anti-cheat foundation. |
| **M6 – Social layer** | Per-pattern weekly-cohort leagues, shareable run clips. **→ V2 "TikTok of price action."** |
| **Throughout** | Portfolio polish: RN-web live demo, clean repo, README/case study (Denmark job piece). |

---

## 10. Open decisions

- **Instruments:** confirmed start = US index + liquid stocks. Add FX/crypto later
  (note: RTH/gap concepts don't map to 24h markets).
- **Cold-start:** new users see an honest "calibrating / could be luck" Edge for the
  first ~30 trades while Bankroll may be up big — accept, or add a softer provisional rank?
- **Conviction sizing:** walled out of Edge and off in V1; revisit as a cosmetic
  Bankroll amplifier in V2?
- **Anonymization strength:** how much to invest (era-splicing, no-repeat pools) so
  users can't recognize the tape — this underpins the entire social board's honesty.
- **Constant calibration:** z, n-gates, distinct-chart gates, per-pattern stop
  conventions — tune from a real beta trade distribution.

---

*Design developed through adversarial multi-agent brainstorms (concept divergence
→ research grounding → red-team → synthesis) plus iterative review.*
