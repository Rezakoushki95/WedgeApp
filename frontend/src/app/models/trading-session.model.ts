export interface TradingSession {
  sessionId: number;
  instrument: string;
  tradingDay: string; // ISO string format
  currentBarIndex: number;
  hasOpenOrder: boolean;
  entryPrice: number | null;
  totalProfitLoss: number;
  totalOrders: number;
  openProfit: number; // New field for calculated open profit
}
