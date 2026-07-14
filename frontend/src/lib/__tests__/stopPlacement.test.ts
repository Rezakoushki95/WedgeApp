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
