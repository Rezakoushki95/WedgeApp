import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { LightweightChartComponent } from '../../components/lightweight-chart/lightweight-chart.component';
import { MarketDataService } from '../../services/market-data.service';
import { TradingSessionService } from '../../services/trading-session.service';
import { TradingSession } from '../../models/trading-session.model';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss']
})
export class HomePage {
  @ViewChild(LightweightChartComponent) lightweightChart!: LightweightChartComponent;

  session: TradingSession | null = null;
  isShortTrade: boolean = false;
  tradeEntryBarIndex: number | null = null; // Track the bar index where the trade started


  constructor(private marketDataService: MarketDataService, private tradingSessionService: TradingSessionService, private router: Router) { }


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
    const userId = localStorage.getItem('UserId');
    if (!userId) {
      console.error('No user ID found. Redirecting to login...');
      this.router.navigate(['/login']);
      return;
    }

    const instrument = 'S&P 500'; // You can make this dynamic later if needed
    this.tradingSessionService.getSession(+userId, instrument).subscribe({
      next: (session) => {
        if (session) {
          this.session = session;
          console.log('Active session');
          this.loadDayData();
        } else {
          console.error('No active session found. Cannot load day data.');
        }
      },
      error: (error) => {
        console.error('Error fetching session:', error);
      },
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

    if (this.isLastBar) {
      console.log('Last bar already reached. Wait for "Complete Day" action.');
      return; // Stop here if the last bar is already reached
    }

    this.lightweightChart.showNextBar(); // Move to the next bar

    const currentBarIndex = this.lightweightChart.getCurrentBarIndex();

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

  onCompleteDay() {
    if (!this.lightweightChart || !this.session) {
      console.error('Chart or session is not initialized.');
      return;
    }

    if (this.session.hasOpenOrder) {
      console.error('You must close your open trade before completing the day.');
      return;
    }

    console.log('Calling completeDay with session:', this.session);

    // Call completeDay to handle end-of-day logic
    this.tradingSessionService.completeDay(this.session.sessionId).subscribe({
      next: () => {
        console.log('Day completed. Fetching updated session...');
        this.tradingSessionService.getSession(this.session?.sessionId!, this.session!.instrument!).subscribe((updatedSession) => {
          this.session = updatedSession; // Update the session with the new day
          this.loadDayData(); // Automatically fetch and set the next day's bars
          console.log('Trading day completed successfully.');
        });
      },
      error: (err) => console.error('Error completing trading day:', err),
    });
  }






  goLong(currentPrice: number) {
    if (!this.session?.hasOpenOrder && this.session) {
      this.session.hasOpenOrder = true;
      this.session.entryPrice = Math.round(currentPrice * 100) / 100; // Round to 2 decimals
      this.isShortTrade = false;
      this.tradeEntryBarIndex = this.lightweightChart.getCurrentBarIndex(); // Set the entry bar index


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
      this.session.entryPrice = Math.round(currentPrice * 100) / 100; // Round to 2 decimals
      this.isShortTrade = true;
      this.tradeEntryBarIndex = this.lightweightChart.getCurrentBarIndex(); // Set the entry bar index


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
        tradeProfitLoss = (this.session.entryPrice ?? 0) - exitPrice; // Calculate profit for short trade
      } else {
        tradeProfitLoss = exitPrice - (this.session.entryPrice ?? 0); // Calculate profit for long trade
      }

      // Check if the trade entry and exit happened on the same bar
      const currentBarIndex = this.lightweightChart.getCurrentBarIndex();
      if (this.tradeEntryBarIndex !== currentBarIndex) {
        console.log(currentBarIndex, this.tradeEntryBarIndex);
        this.session.totalOrders += 1; // Only increment if different bars
      }

      // Update cumulative session metrics
      this.session.totalProfitLoss = Math.round((this.session.totalProfitLoss + tradeProfitLoss) * 100) / 100; // Round to 2 decimals

      // Reset trade-related properties
      this.session.hasOpenOrder = false;
      this.session.entryPrice = null; // Explicitly set to null to reflect trade closure
      this.tradeEntryBarIndex = null; // Reset entry bar index


      // Persist the updated session to the backend
      this.updateTradingSession({
        currentBarIndex: this.lightweightChart.getCurrentBarIndex(),
        totalProfitLoss: this.session.totalProfitLoss,
        totalOrders: this.session.totalOrders,
        hasOpenOrder: this.session.hasOpenOrder,
        entryPrice: this.session.entryPrice, // Ensure null is sent to backend
      }, `Trade closed successfully. Profit/Loss: ${tradeProfitLoss.toFixed(2)}`, 'Error closing trade.');
    }
  }

  calculateOpenProfit(currentPrice: number): number {
    if (this.session?.hasOpenOrder && this.session.entryPrice !== null) {
      let profit = this.isShortTrade
        ? (this.session.entryPrice ?? 0) - currentPrice
        : currentPrice - (this.session.entryPrice ?? 0);

      return Math.round(profit * 100) / 100; // Round to 2 decimals
    }
    return 0;
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
          this.session = {
            ...updatedSession,
            totalProfitLoss: Math.round((updatedSession.totalProfitLoss ?? 0) * 100) / 100 // Ensure it's rounded
          };
          console.log(successMessage, updatedSession);
        },
        error: (err) => {
          console.error(errorMessage, err);
        },
      });
    }
  }

  // computed properties
  get isLastBar(): boolean {
    return this.lightweightChart?.getCurrentBarIndex() === this.lightweightChart?.getTotalBars() - 1;
  }

  get isLongDisabled(): boolean {
    return this.session?.hasOpenOrder || this.isLastBar;
  }

  get isShortDisabled(): boolean {
    return this.session?.hasOpenOrder || this.isLastBar;
  }

  get isExitTradeDisabled(): boolean {
    return !this.session?.hasOpenOrder;
  }



}


