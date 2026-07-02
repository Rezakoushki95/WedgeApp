# Explicit Stop Placement Before Entry — Design

_Date: 2026-07-02 · Branch context: frontend (TradeScreen + CandleChart)_

## Goal

Remove the default stop (the 0.4% stepper and its always-visible proposed-stop
lines). The trader must place their own stop on the chart before a trade can be
entered; **entry without a stop is impossible by construction** — the ENTER
button does not exist until a valid stop is placed.

This matches the locked product design ("you set the original stop — any
distance, tight or wide, your choice") and Al Brooks practice: the stop is a
deliberate read of structure (under the bar / beyond the swing), not a default
percentage.

## Interaction — four states on the Trade screen

### 1. Idle (no position, not armed)
- Chart is clean: candles + EMA (+ magnets if toggled). **No red lines at all.**
- Controls: `LONG` · `SHORT` · `NEXT BAR`.
- Tapping LONG or SHORT does NOT enter a trade — it arms one.

### 2. Armed (direction chosen, no stop yet)
- Hint bar: **"Tap the chart to place your stop"**.
- The armed button is highlighted; the opposite direction button is hidden.
- Controls: `CANCEL` · `NEXT BAR`.
- **Tap on the chart** → the tap's y-coordinate converts to a price (inverse of
  the chart's y-scale) → a red stop line appears at that price.
- **Wrong-side taps are ignored**: armed LONG, tap at/above the current close →
  no line; the hint flashes **"Long stop must be below price"** (mirrored for
  short). No popups.

### 3. Placed (armed + valid stop line set)
- Risk readout row: stop price + distance, e.g. `Stop 7521.25 · 0.31% below`.
- Controls: **`ENTER LONG`** (or `ENTER SHORT`) · `CANCEL` · `NEXT BAR`.
- **Re-tap anywhere valid** → moves the line (last tap wins).
- **ENTER** → opens the position at the current bar's close with the placed
  level as `originalStop` (the R anchor) and `liveStop` — identical semantics
  to the existing `enter()`, minus the derived-from-percentage stop.
- **NEXT BAR** is allowed while armed/placed (waiting a bar before pulling the
  trigger is legitimate). The placed level persists across bars; if the new
  close crosses to the wrong side of the level (long stop no longer below /
  short stop no longer above), the line clears and the state returns to Armed
  with the hint shown.
- **CANCEL** → back to Idle; line gone.

### 4. In a trade — unchanged
Entry/stop lines, `STOP −` / `B/E` / `STOP +` live-stop nudges, `EXIT`, and
intra-bar stop enforcement on reveal all stay exactly as they are.

## Component changes

**`CandleChart`** (`frontend/src/components/CandleChart.tsx`)
- New optional prop `onPriceTap?: (price: number) => void`. Implemented with a
  plain React Native touch handler on the chart container (locationY →
  price via the existing layout math — the inverse of `layout.y`). No gesture
  library needed for a tap.
- New optional prop `pendingStop?: number | null` — drawn as the red stop line
  (same rendering as `liveStop`).
- **Remove the `proposedStops` prop** and its faint-red-lines rendering.

**`TradeScreen`** (`frontend/src/screens/TradeScreen.tsx`)
- Remove `stopPct` state, the stepper row, and `proposedStops` computation.
- New state: `arming: TradeDirection | null` and `pendingStop: number | null`.
- `enter(direction)` becomes `arm(direction)`; a new `confirmEntry()` performs
  the current `enter()` logic using `pendingStop` as the original stop.
- Tap handling: `onPriceTap` sets `pendingStop` only when the price is on the
  valid side of the current close for the armed direction; invalid taps trigger
  the transient hint message.
- On `nextBar()` while armed/placed: re-validate `pendingStop` against the new
  close; clear it (back to Armed) if it is no longer on the valid side.

No backend, API, or scoring changes — `originalStop`/`liveStop` semantics and
`submitTrade` are untouched.

## Error handling

- Wrong-side tap → ignored + transient hint (no popup, no state change).
- Tap while not armed → ignored (chart taps mean nothing in Idle/in-trade).
- Stop invalidated by price movement across bars → auto-clear to Armed + hint.
- `atLastBar` while armed → arming is moot; CANCEL/NEW CHART behave as today.

## Testing

- Unit (pure logic, extracted where practical): tap-price validity per
  direction; pending-stop invalidation on cross; entry uses the placed level as
  `originalStop` verbatim.
- Manual/visual: web capture script still passes (update `shoot_win.js`: the
  flow is now LONG → tap chart → ENTER; it currently only reveals bars, so only
  the reveal path is exercised — verify it still works, and extend it to take
  one scripted trade if practical).

## Out of scope (add-later-able)

- Dragging the stop line; tap-to-move the live stop mid-trade; numeric price
  input; volatility-based stop suggestions.
