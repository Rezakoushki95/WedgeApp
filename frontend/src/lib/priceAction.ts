import { Bar, TradeDirection } from '@/api/types';

// Exponential moving average over close prices. Returns an array aligned with
// `bars` (index 0..n-1); the first value seeds on the first close.
export function ema(bars: Bar[], period: number): number[] {
  if (bars.length === 0) return [];
  const k = 2 / (period + 1);
  const out: number[] = [];
  let prev = bars[0].close;
  out.push(prev);
  for (let i = 1; i < bars.length; i++) {
    prev = bars[i].close * k + prev * (1 - k);
    out.push(prev);
  }
  return out;
}

// R against the ORIGINAL stop (client-side preview; the backend is authoritative).
export function computeR(
  direction: TradeDirection,
  entry: number,
  originalStop: number,
  exit: number,
): number {
  const risk = Math.abs(entry - originalStop);
  if (risk === 0) return 0;
  const pnl = direction === TradeDirection.Long ? exit - entry : entry - exit;
  return pnl / risk;
}

// Open P&L of a running position, in R, at the current price.
export function openR(
  direction: TradeDirection,
  entry: number,
  originalStop: number,
  current: number,
): number {
  return computeR(direction, entry, originalStop, current);
}

// Did the live stop get hit by this bar's range? (honest intra-bar check)
export function stopHit(
  direction: TradeDirection,
  liveStop: number,
  bar: Bar,
): boolean {
  return direction === TradeDirection.Long ? bar.low <= liveStop : bar.high >= liveStop;
}
