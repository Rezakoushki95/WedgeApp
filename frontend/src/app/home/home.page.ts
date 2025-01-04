import { Component, OnInit, ViewChild } from '@angular/core';
import { LightweightChartComponent } from '../lightweight-chart/lightweight-chart.component';
import { MarketDataService } from '../services/market-data.service';
import { TradingSessionService } from '../services/trading-session.service';
import { TradingSession } from '../models/trading-session.model';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss']
})
export class HomePage {
  @ViewChild(LightweightChartComponent) lightweightChart!: LightweightChartComponent;

  session: TradingSession | null = null;
  isShortTrade: boolean = false;

  constructor(private marketDataService: MarketDataService, private tradingSessionService: TradingSessionService) { }


  ionViewDidEnter(): void {
    console.log('Page entered, reinitializing chart.');
    if (this.session) {
      this.loadDayData(); // Re-fetch and reinitialize chart
    } else {
      this.fetchActiveSession();
    }
  }

  ionViewWillLeave(): void {
    this.lightweightChart?.cleanup(); // Tell the child to clean itself up
  }


  private fetchActiveSession() {
    const encodedInstrument = encodeURIComponent('S&P 500');
    this.tradingSessionService.getSession(2, encodedInstrument).subscribe({
      next: (session) => {
        if (session) {
          this.session = session;
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
    if (!this.session) {
      console.error('No active session found. Cannot load day data.');
      return;
    }

    this.marketDataService.getBarsForSession(this.session.sessionId).subscribe({
      next: (bars) => {
        if (this.lightweightChart) {
          console.log('Passing bars and current bar index to chart:', bars, this.session?.currentBarIndex);
          this.lightweightChart.setData(bars, this.session?.currentBarIndex); // Pass bars and index
        } else {
          console.error('LightweightChartComponent is not initialized.');
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
    if (!this.lightweightChart || !this.session) {
      console.error('Chart or session is not initialized.');
      return;
    }

    this.lightweightChart.showNextBar();

    const currentBarIndex = this.lightweightChart.getCurrentBarIndex();

    // Check if we've reached the last bar for the day
    if (currentBarIndex === this.lightweightChart.getTotalBars()) {
      console.log('Reached the end of the trading day.');

      if (this.session.hasOpenOrder) {
        console.error('You must close your open trade before completing the day.');
        return;
      }

      console.log('Calling completeDay with session:', this.session);


      // Call completeDay to handle end-of-day logic
      this.tradingSessionService.completeDay(this.session.sessionId).subscribe({
        next: () => {
          console.log('Day completed. Fetching updated session...');
          // Refetch the session after completing the day
          const encodedInstrument = encodeURIComponent('S&P 500');
          this.tradingSessionService.getSession(2, encodedInstrument).subscribe((updatedSession) => {
            this.session = updatedSession; // Update the session with the new day
            this.loadDayData(); // Automatically fetch and set the next day's bars
            console.log('Trading day completed successfully.');
          });
        },
        error: (err) => console.error('Error completing trading day:', err),
      });

      return; // End the method here as it's the last bar
    }

    // Checkpoint logic: Update the backend every 5 bars
    if (currentBarIndex % 5 === 0) {
      this.tradingSessionService.updateSession(this.session.sessionId, {
        currentBarIndex: currentBarIndex,
      }).subscribe({
        next: () => console.log(`Session updated at bar ${currentBarIndex}.`),
        error: (err) => console.error('Error updating session:', err),
      });
    }
  }




  goLong(currentPrice: number) {
    if (!this.session?.hasOpenOrder && this.session) {
      this.session.hasOpenOrder = true;
      this.session.entryPrice = currentPrice;
      this.isShortTrade = false;

      this.updateTradingSession(
        {
          currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
          hasOpenOrder: this.session.hasOpenOrder,
          entryPrice: this.session.entryPrice,
        },
        'Trade opened successfully.',
        'Error opening trade.'
      );
    }
  }


  goShort(currentPrice: number) {
    if (!this.session?.hasOpenOrder && this.session) {
      this.session.hasOpenOrder = true;
      this.session.entryPrice = currentPrice;
      this.isShortTrade = true;

      this.updateTradingSession(
        {
          currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
          hasOpenOrder: this.session.hasOpenOrder,
          entryPrice: this.session.entryPrice,
        },
        'Short position opened successfully.',
        'Error opening short position.'
      );
    }
  }



  closeTrade(exitPrice: number) {
    if (this.session?.hasOpenOrder && this.session) {
      let tradeProfitLoss = 0;
      if (this.isShortTrade) {
        tradeProfitLoss = (this.session.entryPrice ?? 0) - exitPrice;
      } else {
        tradeProfitLoss = exitPrice - (this.session.entryPrice ?? 0);
      }

      this.session.totalProfitLoss += tradeProfitLoss;
      this.session.totalOrders += 1;
      this.session.hasOpenOrder = false;
      this.session.entryPrice = null;

      this.updateTradingSession({
        currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
        totalProfitLoss: this.session.totalProfitLoss,
        totalOrders: this.session.totalOrders,
        hasOpenOrder: this.session.hasOpenOrder,
        entryPrice: this.session.entryPrice,
      }, `Trade closed successfully. Profit/Loss: ${tradeProfitLoss.toFixed(2)}`, 'Error closing trade.');
    }

  }


  calculateOpenProfit(currentPrice: number): number {
    if (this.session?.hasOpenOrder && this.session.entryPrice !== null) {
      if (this.isShortTrade) {
        return (this.session.entryPrice ?? 0) - currentPrice; // Short trade calculation
      } else {
        return currentPrice - (this.session.entryPrice ?? 0); // Long trade calculation
      }
    }
    return 0; // Default to 0 if no open order or entry price is null
  }



  private updateTradingSession(data: {
    currentBarIndex?: number;
    totalProfitLoss?: number;
    totalOrders?: number;
    hasOpenOrder?: boolean;
    entryPrice?: number | null;
  }, successMessage: string, errorMessage: string): void {
    if (this.session?.sessionId) {
      const updatePayload = {
        sessionId: this.session.sessionId,
        instrument: this.session.instrument,
        tradingDay: this.session.tradingDay,
        ...data,
      };

      console.log('Sending update payload:', updatePayload);

      this.tradingSessionService.updateSession(this.session.sessionId, updatePayload).subscribe({
        next: (updatedSession) => {
          this.session = updatedSession; // Sync the updated session
          console.log(successMessage, updatedSession);
        },
        error: (err) => {
          console.error(errorMessage, err);
        },
      });
    }
  }


}


