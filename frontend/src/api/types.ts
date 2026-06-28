// Mirrors the backend DTOs (backend/DTOs). Keep in sync.

export interface Bar {
  index: number;
  open: number;
  high: number;
  low: number;
  close: number;
}

export interface MagnetLevels {
  prevDayOpen: number | null;
  prevDayHigh: number | null;
  prevDayLow: number | null;
  prevDayClose: number | null;
  prevWeekOpen: number | null;
  prevWeekHigh: number | null;
  prevWeekLow: number | null;
  prevWeekClose: number | null;
  prevMonthOpen: number | null;
  prevMonthHigh: number | null;
  prevMonthLow: number | null;
  prevMonthClose: number | null;
  gapPoints: number | null;
  gapPercent: number | null;
}

export interface DealtChart {
  chartId: number;
  bars: Bar[];
  magnets: MagnetLevels;
}

export enum TradeDirection {
  Long = 0,
  Short = 1,
}

export enum ExitReason {
  Stop = 0,
  Manual = 1,
  EndOfDay = 2,
}

export enum EdgeState {
  Calibrating = 0,
  Noise = 1,
  Promising = 2,
  Established = 3,
}

export interface SubmitTrade {
  journeyId: number;
  chartId: number;
  direction: TradeDirection;
  entryBarIndex: number;
  entryPrice: number;
  originalStop: number;
  liveStop?: number | null;
  exitBarIndex: number;
  exitPrice: number;
  exitReason: ExitReason;
  riskFraction?: number;
}

export interface TradeResult {
  tradeId: number;
  resultR: number;
  bankrollAfter: number;
}

export interface JourneyStats {
  journeyId: number;
  name: string;
  patternTag: string;
  isSandbox: boolean;
  archived: boolean;
  tradeCount: number;
  distinctCharts: number;
  bankroll: number;
  expectancy: number;
  edge: number;
  edgeState: EdgeState;
  winRate: number;
  avgWinR: number;
  avgLossR: number;
  profitFactor: number;
  maxDrawdownR: number;
}

export interface CreateJourney {
  name: string;
  patternTag: string;
  isSandbox?: boolean;
}

export interface Journey {
  id: number;
  userId: number;
  name: string;
  patternTag: string;
  isSandbox: boolean;
  archived: boolean;
}
