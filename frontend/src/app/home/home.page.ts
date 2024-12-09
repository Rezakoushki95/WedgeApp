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
  }
  
  private fetchActiveSession() {
    const encodedInstrument = encodeURIComponent('S&P 500');
    this.tradingSessionService.getSession(2, encodedInstrument).subscribe({
      next: (session) => {
        if (session) {
          this.activeSession = session;
          console.log('Active session:', session);
          this.loadDayData(); 
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
  

  getCurrentBarPrice(): number {
    if (this.lightweightChart) {
      const currentBar = this.lightweightChart.getCurrentBar();
      return currentBar?.close ?? 0; // Return the closing price or 0 if not available
    }
    console.warn('LightweightChartComponent is not initialized yet.');
    return 0;
  }

  onNextBar() {
    if (!this.lightweightChart || !this.activeSession) {
      console.error('Chart or session is not initialized.');
      return;
    }
  
    this.lightweightChart.showNextBar();
  
    const currentBarIndex = this.lightweightChart.getCurrentBarIndex();
  
    // Check if we've reached the last bar for the day
    if (currentBarIndex === this.lightweightChart.getTotalBars()) {
      console.log('Reached the end of the trading day.');
  
      // Call completeDay to handle end-of-day logic
      this.tradingSessionService.completeDay(this.activeSession.id).subscribe({
        next: () => {
          console.log('Trading day completed. Fetching next day data.');
  
          // Fetch the next day data and reset the chart
          this.marketDataService.getBarsForSession(this.activeSession.id).subscribe({
            next: (bars) => {
              this.lightweightChart.setData(bars);
            },
            error: (err) => console.error('Error fetching next day data:', err),
          });
        },
        error: (err) => console.error('Error completing trading day:', err),
      });
  
      return; // End the method here as it's the last bar
    }
  
    // Checkpoint logic: Update the backend every 5 bars
    if (currentBarIndex % 5 === 0) {
      this.tradingSessionService.updateSession(this.activeSession.id, {
        currentBarIndex: currentBarIndex,
      }).subscribe({
        next: () => console.log(`Session updated at bar ${currentBarIndex}.`),
        error: (err) => console.error('Error updating session:', err),
      });
    }
  }
  
  
  
  

  goLong(currentPrice: number) {
    if (!this.hasOpenOrder && this.activeSession) {
      this.hasOpenOrder = true;
      this.entryPrice = currentPrice;
  
      this.updateTradingSession(
        {
          currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
          hasOpenOrder: this.hasOpenOrder,
          entryPrice: this.entryPrice,
        },
        'Trade opened successfully.',
        'Error opening trade.'
      );
    }
  }
  

  goShort(currentPrice: number) {
    if (!this.hasOpenOrder && this.activeSession) {
      this.hasOpenOrder = true;
      this.entryPrice = currentPrice;
  
      this.updateTradingSession(
        {
          currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
          hasOpenOrder: this.hasOpenOrder,
          entryPrice: this.entryPrice,
        },
        'Short position opened successfully.',
        'Error opening short position.'
      );
    }
  }
  


  closeTrade(exitPrice: number) {
    if (this.hasOpenOrder && this.activeSession) {
      const tradeProfitLoss = exitPrice - (this.entryPrice ?? 0);
      this.totalProfitLoss += tradeProfitLoss;
      this.totalOrders += 1;
      this.hasOpenOrder = false;
      this.entryPrice = null;
  
      this.updateTradingSession(
        {
          currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
          totalProfitLoss: this.totalProfitLoss,
          totalOrders: this.totalOrders,
          hasOpenOrder: this.hasOpenOrder,
          entryPrice: this.entryPrice,
        },
        'Trade closed successfully.',
        'Error closing trade.'
      );
    }
  }
  
  calculateOpenProfit(currentPrice: number): number {
    if (this.hasOpenOrder && this.entryPrice !== null) {
      return currentPrice - this.entryPrice;
    }
    return 0;
  }


  private updateTradingSession(data: {
    currentBarIndex?: number;
    currentProfitLoss?: number;
    totalProfitLoss?: number;
    totalOrders?: number;
    hasOpenOrder?: boolean;
    entryPrice?: number | null;
  }, successMessage: string, errorMessage: string): void {
    if (this.activeSession?.id) {
      this.tradingSessionService.updateSession(this.activeSession.id, data).subscribe({
        next: (updatedSession) => {
          this.activeSession = updatedSession; // Sync the updated session
          console.log(successMessage, updatedSession);
        },
        error: (err) => {
          console.error(errorMessage, err);
        },
      });
    }
  }
  
}


