import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { LightweightChartComponent } from '../lightweight-chart/lightweight-chart.component';
import { MarketDataService } from '../services/market-data.service';
import { MarketDataDay } from '../services/market-data.service';
import { TradingSessionService } from '../services/trading-session.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss']
})
export class HomePage implements OnInit, AfterViewInit {
  @ViewChild(LightweightChartComponent) lightweightChart!: LightweightChartComponent;

  currentProfitLoss = 0;
  totalProfitLoss = 0;
  totalOrders = 0;
  hasOpenOrder = false;
  entryPrice: number | null = null;
  activeSession: any;

  constructor(private marketDataService: MarketDataService, private tradingSessionService: TradingSessionService) {}

  ngOnInit(): void {
    this.fetchActiveSession();
  }

  ngAfterViewInit(): void {
    // Load data only after the view, including @ViewChild, has been initialized
    this.loadDayData();
  }

  private fetchActiveSession() {
    this.tradingSessionService.getActiveSession().subscribe({
      next: (session) => {
        this.activeSession = session;
        console.log('Active session:', session);
      },
      error: (error) => {
        console.error('Error fetching active session:', error);
      }
    });
  }

  private loadDayData() {
    this.marketDataService.getUnaccessedDay().subscribe({
      next: (dayData: MarketDataDay) => {
        if (this.lightweightChart) {
          this.lightweightChart.setData(dayData.fiveMinuteBars);
        }
      },
      error: (error) => console.error("Error fetching market data:", error),
    });
  }

  onNextBar() {
    if (this.lightweightChart) {
      this.lightweightChart.showNextBar();
    }
  }

  getCurrentBarPrice(): number {
    if (this.lightweightChart) {
      const currentBar = this.lightweightChart.getCurrentBar();
      return currentBar?.close ?? 0; // Return the closing price or 0 if not available
    }
    console.warn('LightweightChartComponent is not initialized yet.');
    return 0;
  }
  

  goLong(currentPrice: number) {
    if (!this.hasOpenOrder) {
      this.hasOpenOrder = true;
      this.entryPrice = currentPrice;
  
      this.tradingSessionService.updateSession({
        currentProfitLoss: this.currentProfitLoss,
        totalProfitLoss: this.totalProfitLoss,
        totalOrders: this.totalOrders,
        hasOpenOrder: this.hasOpenOrder,
        entryPrice: this.entryPrice,
      }).subscribe({
        next: () => console.log('Trade opened successfully.'),
        error: (err) => console.error('Error opening trade:', err),
      });
    }
  }

  goShort(currentPrice: number) {
    if (!this.hasOpenOrder) {
      this.hasOpenOrder = true;
      this.entryPrice = currentPrice;
  
      this.tradingSessionService.updateSession({
        currentProfitLoss: this.currentProfitLoss,
        totalProfitLoss: this.totalProfitLoss,
        totalOrders: this.totalOrders,
        hasOpenOrder: this.hasOpenOrder,
        entryPrice: this.entryPrice,
      }).subscribe({
        next: () => console.log('Short position opened successfully.'),
        error: (err) => console.error('Error opening short position:', err),
      });
    }
  }

  calculateOpenProfit(currentPrice: number): number {
    if (this.hasOpenOrder && this.entryPrice !== null) {
      return currentPrice - this.entryPrice;
    }
    return 0;
  }

  closeTrade(exitPrice: number) {
    if (this.hasOpenOrder) {
      const tradeProfitLoss = exitPrice - (this.entryPrice ?? 0);
      this.totalProfitLoss += tradeProfitLoss;
      this.totalOrders += 1;
      this.hasOpenOrder = false;
      this.entryPrice = null;

      this.tradingSessionService.updateSession({
        currentProfitLoss: 0,
        totalProfitLoss: this.totalProfitLoss,
        totalOrders: this.totalOrders,
        hasOpenOrder: this.hasOpenOrder,
        entryPrice: this.entryPrice,
      }).subscribe({
        next: () => console.log('Trade closed successfully.'),
        error: (err) => console.error('Error closing trade:', err),
      });
    }
  }
}
