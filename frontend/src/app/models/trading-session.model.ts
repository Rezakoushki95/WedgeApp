export interface TradingSession {
    id: number;
    userId: number;
    instrument: string;
    tradingDay: string;
    currentBarIndex: number;
    hasOpenOrder: boolean;
    entryPrice: number | null;
    totalProfitLoss: number;
    totalOrders: number;
  }
  