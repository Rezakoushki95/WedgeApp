import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { LightweightChartComponent } from '../lightweight-chart/lightweight-chart.component';
import { MarketDataService } from '../services/market-data.service';
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
    // Load data will be called after session is fetched
  }
  
  private fetchActiveSession() {
    const encodedInstrument = encodeURIComponent('S&P 500');
    this.tradingSessionService.getSession(2, encodedInstrument).subscribe({
      next: (session) => {
        if (session) {
          this.activeSession = session;
          console.log('Active session:', session);
          this.loadDayData(); // Only load data after session is fetched
        } else {
          console.error('No active session found. Cannot load day data.');
        }
      },
      error: (error) => {
        console.error('Error fetching session:', error);
      }
    });
  }
  
  private loadDayData() {
    if (!this.activeSession) {
      console.error('No active session found. Cannot load day data.');
      return;
    }
  
    this.marketDataService.getBarsForSession(this.activeSession.id).subscribe({
      next: (bars) => {
        if (this.lightweightChart) {
          this.lightweightChart.setData(bars);
        }
      },
      error: (error) => console.error('Error fetching market data:', error),
    });
  }
  
  onNextBar() {
    if (this.lightweightChart) {
      this.lightweightChart.showNextBar();
  
      if (this.activeSession) {
        this.tradingSessionService.updateSession(this.activeSession.id, {
          currentBarIndex: this.lightweightChart.getCurrentBarIndex(), // Use the getter here
        }).subscribe({
          next: () => console.log('Progress saved.'),
          error: (err) => console.error('Error saving progress:', err),
        });
      }
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
    if (!this.hasOpenOrder && this.activeSession) {
      this.hasOpenOrder = true;
      this.entryPrice = currentPrice;
  
      this.tradingSessionService.updateSession(this.activeSession.id, {
        currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
        hasOpenOrder: this.hasOpenOrder,
        entryPrice: this.entryPrice,
      }).subscribe({
        next: () => console.log('Trade opened successfully.'),
        error: (err) => console.error('Error opening trade:', err),
      });
    }
  }

  goShort(currentPrice: number) {
    if (!this.hasOpenOrder && this.activeSession?.id) {
      this.hasOpenOrder = true;
      this.entryPrice = currentPrice;
  
      this.tradingSessionService.updateSession(this.activeSession.id, {
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
    if (this.hasOpenOrder && this.activeSession) {
      const tradeProfitLoss = exitPrice - (this.entryPrice ?? 0);
      this.totalProfitLoss += tradeProfitLoss;
      this.totalOrders += 1;
      this.hasOpenOrder = false;
      this.entryPrice = null;
  
      this.tradingSessionService.updateSession(this.activeSession.id, {
        currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
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
