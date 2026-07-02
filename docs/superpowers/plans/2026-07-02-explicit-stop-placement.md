# Explicit Stop Placement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the default stop stepper with an arm → tap-the-chart → ENTER flow so a trade cannot be entered without an explicitly placed stop.

**Architecture:** A tiny pure-logic module (`stopPlacement.ts`) owns stop-side validity and cross-bar invalidation (unit-tested). `CandleChart` gains a tap→price callback (inverse of its y-scale) and a `pendingStop` line, and loses `proposedStops`. `TradeScreen` gains an `arming`/`pendingStop` state machine (Idle → Armed → Placed → In-trade); ENTER only exists in Placed. No backend/API changes.

**Tech Stack:** React Native (Expo SDK 54), TypeScript, `@shopify/react-native-skia` 2.2 chart, jest-expo (new, for the pure logic only).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-02-explicit-stop-placement-design.md`.
- Entry without a placed stop must be impossible **by construction** (no ENTER button until a valid stop is placed).
- Wrong-side taps are ignored + transient hint (no popups): long stop must be **below** the current close; short stop **above**. Equal-to-close counts as invalid.
- The placed stop persists across `NEXT BAR`; if the new close crosses it, clear back to Armed and hint.
- Entry uses the current bar's close as `entryPrice` and the placed level verbatim as `originalStop` and initial `liveStop` (existing semantics).
- In-trade behavior (STOP−/B/E/STOP+, EXIT, intra-bar enforcement) unchanged.
- The chart's y-scale: `y(price) = ((hi − price) / range) * height` ⇒ tap inverse `price = hi − (y / height) * range`.
- Work on branch `feature/explicit-stop-placement` off up-to-date `main`.
- Commands run from `frontend/` unless stated; `npm run typecheck` must stay clean at every commit.

---

## File Structure

- Create: `frontend/src/lib/stopPlacement.ts` — pure validity/invalidations logic.
- Create: `frontend/src/lib/__tests__/stopPlacement.test.ts` — unit tests.
- Modify: `frontend/package.json` — add jest-expo test infra (devDeps + script + jest config).
- Modify: `frontend/src/components/CandleChart.tsx` — `onPriceTap` + `pendingStop`; remove `proposedStops`.
- Modify: `frontend/src/screens/TradeScreen.tsx` — state machine + controls; remove stepper.
- Modify: `frontend/scripts/shoot_win.js` — script a full trade (arm → tap → enter → exit) so the capture exercises the new flow.

---

### Task 1: Test infra + pure stop-placement logic

**Files:**
- Modify: `frontend/package.json`
- Create: `frontend/src/lib/stopPlacement.ts`
- Create: `frontend/src/lib/__tests__/stopPlacement.test.ts`

**Interfaces:**
- Consumes: `TradeDirection` from `@/api/types` (enum with `Long` and `Short` members — check the exact declaration in `frontend/src/api/types.ts` before writing the lib; use it exactly as `TradeScreen.tsx` does today: `TradeDirection.Long` / `TradeDirection.Short`).
- Produces (used verbatim by Task 3):
  - `isValidStopSide(direction: TradeDirection, stop: number, currentClose: number): boolean`
  - `revalidatePendingStop(direction: TradeDirection, pendingStop: number | null, newClose: number): number | null`

- [ ] **Step 1: Create the branch**

```bash
cd C:/Users/rezaa/OneDrive/Skrivebord/wedge-trading && git checkout main && git pull && git checkout -b feature/explicit-stop-placement
```

- [ ] **Step 2: Add jest-expo**

Run from `frontend/`:
```bash
npx expo install jest-expo jest -- --save-dev
```
Then add to `frontend/package.json` (top level, after `"scripts"`), and add the script:
```json
  "scripts": {
    "...existing scripts unchanged...": "",
    "test": "jest"
  },
  "jest": {
    "preset": "jest-expo",
    "moduleNameMapper": { "^@/(.*)$": "<rootDir>/src/$1" }
  },
```
(Keep existing scripts; only add `"test": "jest"` and the `"jest"` block.)

- [ ] **Step 3: Write the failing tests**

Create `frontend/src/lib/__tests__/stopPlacement.test.ts`:
```ts
import { isValidStopSide, revalidatePendingStop } from '@/lib/stopPlacement';
import { TradeDirection } from '@/api/types';

describe('isValidStopSide', () => {
  it('long: stop below close is valid', () => {
    expect(isValidStopSide(TradeDirection.Long, 99, 100)).toBe(true);
  });
  it('long: stop at or above close is invalid', () => {
    expect(isValidStopSide(TradeDirection.Long, 100, 100)).toBe(false);
    expect(isValidStopSide(TradeDirection.Long, 101, 100)).toBe(false);
  });
  it('short: stop above close is valid', () => {
    expect(isValidStopSide(TradeDirection.Short, 101, 100)).toBe(true);
  });
  it('short: stop at or below close is invalid', () => {
    expect(isValidStopSide(TradeDirection.Short, 100, 100)).toBe(false);
    expect(isValidStopSide(TradeDirection.Short, 99, 100)).toBe(false);
  });
});

describe('revalidatePendingStop', () => {
  it('null stays null', () => {
    expect(revalidatePendingStop(TradeDirection.Long, null, 100)).toBeNull();
  });
  it('keeps a long stop still below the new close', () => {
    expect(revalidatePendingStop(TradeDirection.Long, 99, 100)).toBe(99);
  });
  it('clears a long stop the new close has crossed', () => {
    expect(revalidatePendingStop(TradeDirection.Long, 99, 98.5)).toBeNull();
  });
  it('keeps a short stop still above the new close', () => {
    expect(revalidatePendingStop(TradeDirection.Short, 101, 100)).toBe(101);
  });
  it('clears a short stop the new close has crossed', () => {
    expect(revalidatePendingStop(TradeDirection.Short, 101, 101.5)).toBeNull();
  });
});
```

- [ ] **Step 4: Run tests to verify they fail**

Run from `frontend/`: `npm test -- stopPlacement`
Expected: FAIL — cannot find module `@/lib/stopPlacement`.

- [ ] **Step 5: Implement the module**

Create `frontend/src/lib/stopPlacement.ts`:
```ts
import { TradeDirection } from '@/api/types';

// A stop is only valid on the risk side of price: strictly below the current
// close for a long, strictly above it for a short.
export function isValidStopSide(
  direction: TradeDirection,
  stop: number,
  currentClose: number
): boolean {
  return direction === TradeDirection.Long ? stop < currentClose : stop > currentClose;
}

// Re-check a placed-but-unentered stop after a new bar reveals: keep it while
// it is still on the valid side of the new close, otherwise clear it (the
// screen falls back to Armed and shows a hint).
export function revalidatePendingStop(
  direction: TradeDirection,
  pendingStop: number | null,
  newClose: number
): number | null {
  if (pendingStop == null) return null;
  return isValidStopSide(direction, pendingStop, newClose) ? pendingStop : null;
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `npm test -- stopPlacement`
Expected: PASS — 9 tests green.

- [ ] **Step 7: Typecheck + commit**

Run: `npm run typecheck` → clean, then:
```bash
git add frontend/package.json frontend/package-lock.json frontend/src/lib/stopPlacement.ts frontend/src/lib/__tests__/stopPlacement.test.ts
git commit -m "feat: stop-placement validity logic + jest-expo test infra"
```

---

### Task 2: CandleChart — tap→price callback + pending stop line

**Files:**
- Modify: `frontend/src/components/CandleChart.tsx`

**Interfaces:**
- Consumes: nothing new from Task 1.
- Produces (used verbatim by Task 3): two new optional props on `CandleChart` —
  `pendingStop?: number | null` (red line, same color as the live stop) and
  `onPriceTap?: (price: number) => void` (fired with the tapped price). The
  `proposedStops` prop is **removed**.

No unit test is practical for a Skia canvas; the gate is typecheck + the web capture in Task 3 exercising the tap. Keep this task purely mechanical.

- [ ] **Step 1: Update the props and layout extents**

In `frontend/src/components/CandleChart.tsx`:

Replace the `proposedStops` prop with the two new props in `interface Props`:
```ts
interface Props {
  bars: Bar[]; // revealed bars only (caller slices to currentIndex)
  width: number;
  height: number;
  emaPeriod?: number;
  showMagnets?: boolean;
  magnets?: MagnetLevels | null;
  entryPrice?: number | null;
  liveStop?: number | null;
  pendingStop?: number | null; // placed-but-unentered stop (Armed/Placed state)
  onPriceTap?: (price: number) => void; // tap on the chart -> price at that y
}
```
Update the destructuring in the component signature accordingly (`pendingStop`, `onPriceTap` instead of `proposedStops = []`).

In the `useMemo` layout, replace the `proposedStops` extent handling with `pendingStop` (keep magnets):
```ts
    const magnetLevels = showMagnets && magnets ? collectMagnets(magnets) : [];
    let hi = Math.max(...bars.map((b) => b.high), ...magnetLevels);
    let lo = Math.min(...bars.map((b) => b.low), ...magnetLevels);
    if (entryPrice != null) { hi = Math.max(hi, entryPrice); lo = Math.min(lo, entryPrice); }
    if (liveStop != null) { hi = Math.max(hi, liveStop); lo = Math.min(lo, liveStop); }
    if (pendingStop != null) { hi = Math.max(hi, pendingStop); lo = Math.min(lo, pendingStop); }
```
Update the `useMemo` dependency array: remove `proposedStops`, add `pendingStop`.

Note: with no magnets, no position and no pending stop, `...magnetLevels` spreads to nothing and `Math.max(...bars.map(...))` alone still works — preserve that.

- [ ] **Step 2: Replace the faint proposed-stop lines with the pending stop line**

Delete the `proposedStops` render block:
```tsx
      {/* proposed stops (before entry) */}
      {proposedStops.map((s, i) => (
        <Line key={`ps${i}`} p1={vec(0, layout.y(s))} p2={vec(width, layout.y(s))} color="rgba(239,83,80,0.35)" strokeWidth={1} />
      ))}
```
Add, next to the existing `liveStop` line render:
```tsx
      {/* placed-but-unentered stop (Armed/Placed) */}
      {pendingStop != null && (
        <Line p1={vec(0, layout.y(pendingStop))} p2={vec(width, layout.y(pendingStop))} color={STOP_COLOR} strokeWidth={1} />
      )}
```

- [ ] **Step 3: Add the tap handler (inverse y-scale)**

Import `Pressable` from `react-native` (alongside `View`). Wrap the `<Canvas>` in a `Pressable` that converts the tap's y to a price:
```tsx
  const handlePress = (e: { nativeEvent: { locationY: number } }) => {
    if (!onPriceTap || !layout) return;
    const { hi, range } = layout;
    onPriceTap(hi - (e.nativeEvent.locationY / height) * range);
  };

  return (
    <Pressable onPress={handlePress} disabled={!onPriceTap}>
      <Canvas style={{ width, height }}>
        {/* ...existing children unchanged... */}
      </Canvas>
    </Pressable>
  );
```
(`layout` already exposes `hi` and `range`; `locationY` is relative to the Pressable, which is exactly the canvas box. react-native-web maps offsetY to `locationY`, so this works on web too.)

- [ ] **Step 4: Verify**

Run from `frontend/`:
```bash
npm run typecheck
```
Expected: FAIL in `TradeScreen.tsx` only (it still passes the removed `proposedStops` prop) — that is Task 3's file. If there are errors in `CandleChart.tsx` itself, fix them now. To confirm the chart file itself is sound in isolation: `npx tsc --noEmit 2>&1 | grep -v TradeScreen` → no CandleChart errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/CandleChart.tsx
git commit -m "feat: chart tap->price callback + pending stop line; drop proposedStops"
```
(The transient TradeScreen typecheck break is resolved by the very next task; if your process requires green-at-every-commit, squash-land Tasks 2+3 together instead — but prefer committing here for review granularity.)

---

### Task 3: TradeScreen state machine + capture-script trade

**Files:**
- Modify: `frontend/src/screens/TradeScreen.tsx`
- Modify: `frontend/scripts/shoot_win.js`

**Interfaces:**
- Consumes: `isValidStopSide`, `revalidatePendingStop` from `@/lib/stopPlacement` (Task 1); `pendingStop` + `onPriceTap` props on `CandleChart` (Task 2).
- Produces: the user-facing flow — Idle (`LONG`/`SHORT` arm) → Armed (hint "Tap the chart to place your stop", `CANCEL`) → Placed (risk readout + `ENTER LONG|SHORT` + `CANCEL`) → In-trade (unchanged).

- [ ] **Step 1: Rewrite the pre-entry portion of TradeScreen**

In `frontend/src/screens/TradeScreen.tsx`, make these exact changes:

1. Extend imports:
```ts
import { isValidStopSide, revalidatePendingStop } from '@/lib/stopPlacement';
```

2. Replace the `stopPct` state with the new state (delete the `stopPct` line and its comment):
```ts
  // Arm-then-place: pick a direction, then tap the chart to place the stop.
  // ENTER only exists once a valid stop is placed — entry without a stop is
  // impossible by construction.
  const [arming, setArming] = useState<TradeDirection | null>(null);
  const [pendingStop, setPendingStop] = useState<number | null>(null);
  const [hint, setHint] = useState<string | null>(null);
```

3. Add a transient hint helper (inside the component, after the state):
```ts
  const hintTimer = React.useRef<ReturnType<typeof setTimeout> | null>(null);
  const flashHint = (msg: string) => {
    setHint(msg);
    if (hintTimer.current) clearTimeout(hintTimer.current);
    hintTimer.current = setTimeout(() => setHint(null), 2500);
  };
  useEffect(() => () => { if (hintTimer.current) clearTimeout(hintTimer.current); }, []);
```

4. Reset arming state when dealing a new chart — inside `deal()` add:
```ts
    setArming(null);
    setPendingStop(null);
```

5. Delete `const stopDist = price * stopPct;` and the `proposedStops` line. Replace `enter` with:
```ts
  const arm = (direction: TradeDirection) => {
    if (position) return;
    setArming(direction);
    setPendingStop(null);
  };

  const cancelArming = () => {
    setArming(null);
    setPendingStop(null);
    setHint(null);
  };

  const onPriceTap = (tapped: number) => {
    if (!arming || position || !currentBar) return;
    if (isValidStopSide(arming, tapped, price)) {
      setPendingStop(tapped);
    } else {
      flashHint(arming === TradeDirection.Long
        ? 'Long stop must be below price'
        : 'Short stop must be above price');
    }
  };

  const confirmEntry = () => {
    if (!arming || pendingStop == null || position || !currentBar) return;
    setPosition({
      direction: arming,
      entryBarIndex: revealed - 1,
      entryPrice: price,
      originalStop: pendingStop,
      liveStop: pendingStop,
    });
    setArming(null);
    setPendingStop(null);
    setHint(null);
  };
```

6. In `nextBar()`, after the stop-hit check and before `setRevealed`, re-validate a pending stop against the newly revealed bar's close:
```ts
    if (arming && pendingStop != null) {
      const kept = revalidatePendingStop(arming, pendingStop, nb.close);
      if (kept == null) {
        setPendingStop(null);
        flashHint('Price crossed your stop level — place it again');
      }
    }
```

7. Update the `CandleChart` usage — drop `proposedStops`, add the new props:
```tsx
      <CandleChart
        bars={visible}
        width={width}
        height={360}
        showMagnets={showMagnets}
        magnets={chart.magnets}
        entryPrice={position?.entryPrice ?? null}
        liveStop={position?.liveStop ?? null}
        pendingStop={pendingStop}
        onPriceTap={onPriceTap}
      />
```

8. Replace the whole `{!position ? (...) : (...)}` controls block's pre-entry branch with the three-state version:
```tsx
        {!position ? (
          arming == null ? (
            <View style={styles.row}>
              <Btn label="LONG" color="#26a69a" disabled={atLastBar} onPress={() => arm(TradeDirection.Long)} />
              <Btn label="SHORT" color="#ef5350" disabled={atLastBar} onPress={() => arm(TradeDirection.Short)} />
            </View>
          ) : (
            <>
              <Text style={styles.hint}>
                {hint ?? (pendingStop == null
                  ? 'Tap the chart to place your stop'
                  : `Stop ${pendingStop.toFixed(2)} · ${(Math.abs(price - pendingStop) / price * 100).toFixed(2)}% ${arming === TradeDirection.Long ? 'below' : 'above'}`)}
              </Text>
              <View style={styles.row}>
                {pendingStop != null && (
                  <Btn
                    label={arming === TradeDirection.Long ? 'ENTER LONG' : 'ENTER SHORT'}
                    color={arming === TradeDirection.Long ? '#26a69a' : '#ef5350'}
                    onPress={confirmEntry}
                  />
                )}
                <Btn label="CANCEL" color="#455a64" onPress={cancelArming} />
              </View>
            </>
          )
        ) : (
          /* in-trade branch UNCHANGED — keep the existing STOP−/B/E/STOP+ and EXIT rows */
        )}
```
(Keep the existing in-trade branch and the NEXT BAR/NEW CHART button exactly as they are.)

9. Add the hint style to the StyleSheet:
```ts
  hint: { color: '#e0b30a', fontSize: 13, fontWeight: '600', textAlign: 'center' },
```
Also delete the now-unused `stopLabel` style and `stopRow` usages in the pre-entry branch (the in-trade `stopRow` stays).

- [ ] **Step 2: Typecheck + unit tests**

Run from `frontend/`:
```bash
npm run typecheck && npm test -- stopPlacement
```
Expected: both clean/green (this closes Task 2's transient break).

- [ ] **Step 3: Extend the capture script to take a scripted trade**

In `frontend/scripts/shoot_win.js`, replace the "reveal ~30 bars" section (everything between the trade-start screenshot and `browser.close()`) with:
```js
  const magnets = page.getByText('Magnets', { exact: false });
  if (await magnets.count()) { await magnets.first().click().catch(() => {}); await sleep(400); }

  // Reveal ~30 bars.
  for (let i = 0; i < 30; i++) {
    const next = page.getByText(/NEXT BAR/i);
    if (!(await next.count())) break;
    await next.first().click({ timeout: 5000 }).catch(() => {});
    await sleep(60);
  }
  await sleep(1000);
  const bars = await page.evaluate(() => (document.body.innerText.match(/Bar (\d+)\/(\d+)/) || [])[0]);
  console.log('revealed:', bars);
  await page.screenshot({ path: `${OUT}/app_trade_revealed.png` });
  console.log('trade-revealed shot');

  // Scripted trade: arm LONG -> tap low on the chart (valid long stop) ->
  // ENTER LONG -> reveal a few bars -> EXIT. Exercises the whole new flow.
  await page.getByText('LONG', { exact: true }).first().click();
  await page.waitForFunction(() => /Tap the chart to place your stop/.test(document.body.innerText), { timeout: 10000 });
  const canvas = page.locator('canvas').first();
  const box = await canvas.boundingBox();
  await page.mouse.click(box.x + box.width * 0.5, box.y + box.height * 0.9); // near bottom = well below price
  await page.waitForFunction(() => /ENTER LONG/.test(document.body.innerText), { timeout: 10000 });
  await page.screenshot({ path: `${OUT}/app_stop_placed.png` });
  console.log('stop-placed shot');
  await page.getByText('ENTER LONG', { exact: true }).first().click();
  await page.waitForFunction(() => /Open [+-]/.test(document.body.innerText), { timeout: 10000 });
  for (let i = 0; i < 5; i++) {
    const next = page.getByText(/NEXT BAR/i);
    if (!(await next.count())) break;
    await next.first().click({ timeout: 5000 }).catch(() => {});
    await sleep(80);
  }
  await page.getByText('EXIT', { exact: true }).first().click();
  await page.waitForFunction(() => /Closed [+-]/.test(document.body.innerText), { timeout: 10000 });
  await page.screenshot({ path: `${OUT}/app_trade_closed.png` });
  console.log('trade-closed shot:', await page.evaluate(() => (document.body.innerText.match(/Closed [+-][\d.]+R/) || [])[0]));

  await browser.close();
  console.log('DONE');
```

- [ ] **Step 4: Live verification (backend + Metro must be running)**

Preconditions: backend on :5068 with data + a journey; Metro on :8081 (`CI=1 EXPO_PUBLIC_API_URL=http://localhost:5068/api npx expo start --port 8081` — localhost is fine for this headless web check). If they're already running from the session, reuse them.

Run from `frontend/`:
```bash
SHOT_OUT=<scratchpad-dir> node scripts/shoot_win.js
```
Expected output includes: `stop-placed shot`, then `trade-closed shot: Closed ±X.XXR`. Inspect `app_stop_placed.png`: red stop line below price, "Stop … % below" readout, ENTER LONG + CANCEL buttons, **no stepper row, no always-on faint red lines**.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/screens/TradeScreen.tsx frontend/scripts/shoot_win.js
git commit -m "feat: arm->tap->ENTER stop placement; no entry without a stop"
```

---

## Manual verification (user, on the phone)

Reload in Expo Go: LONG → hint appears → tap below price → red line + readout → ENTER LONG → position opens; try SHORT mirror; try tapping the wrong side (hint flashes, nothing placed); place a stop, hit NEXT BAR until price crosses it (line clears + hint); CANCEL works at both stages.

## Self-Review Notes

- **Spec coverage:** Idle/Armed/Placed states + controls → Task 3 step 1 (items 5, 8). Wrong-side ignore + hint → `onPriceTap` + `flashHint`. Re-tap moves line → `setPendingStop(tapped)` (last tap wins). ENTER-only-when-placed → conditional `ENTER` button render. NEXT BAR persistence + cross-invalidation → Task 3 step 1 item 6 + `revalidatePendingStop` (Task 1). Entry semantics (close as entry, placed level as originalStop+liveStop) → `confirmEntry`. Chart: `onPriceTap` inverse y-scale + `pendingStop` line + `proposedStops` removal → Task 2. In-trade unchanged → explicitly preserved. Tests → Task 1 (pure logic, 9 cases) + Task 3 capture (end-to-end trade). Deal reset → Task 3 step 1 item 4.
- **Type consistency:** `isValidStopSide(direction, stop, currentClose)` and `revalidatePendingStop(direction, pendingStop, newClose)` identical in Task 1 exports, tests, and Task 3 imports/usages. `pendingStop?: number | null` / `onPriceTap?: (price: number) => void` identical between Task 2 props and Task 3 usage.
- **Placeholders:** none — all code concrete. The one intentional ellipsis ("existing children unchanged", "in-trade branch UNCHANGED") refers to code the implementer must NOT modify, with its location named.
