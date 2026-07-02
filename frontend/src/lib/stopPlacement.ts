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
