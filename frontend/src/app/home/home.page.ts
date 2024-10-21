import { Component, OnInit } from '@angular/core';
import { MarketDataService } from '../services/market-data.service';

// Enum for Position State
enum PositionState {
  None,
  Long,
  Short
}

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
})
export class HomePage implements OnInit {
  marketData: any[] = [];
  currentIndex: number = -1;

  // State variables for tracking position
  position: PositionState = PositionState.None;
  entryPrice: number | null = null;
  pnl: number = 0;

  constructor(private marketDataService: MarketDataService) {}

  ngOnInit() {}

  // Getter to make PositionState accessible in the template
  get PositionState() {
    return PositionState;
  }

  fetchMarketData() {
    this.marketDataService.getRandomDayData().subscribe({
      next: (data) => {
        this.marketData = data;
        this.currentIndex = -1;
        this.position = PositionState.None;
        this.entryPrice = null;
        this.pnl = 0;
      },
      error: (error) => {
        console.error('Error fetching market data:', error);
      },
      complete: () => {
        console.log('Market data fetch completed.');
      },
    });
  }

  enterLong() {
    if (this.currentIndex >= 0) {
      this.position = PositionState.Long;
      this.entryPrice = this.marketData[this.currentIndex].close;
      console.log('Entered long position at price:', this.entryPrice);
    }
  }

  enterShort() {
    if (this.currentIndex >= 0) {
      this.position = PositionState.Short;
      this.entryPrice = this.marketData[this.currentIndex].close;
      console.log('Entered short position at price:', this.entryPrice);
    }
  }

  exitPosition() {
    this.position = PositionState.None;
    this.entryPrice = null;
    console.log('Exited position');
  }

  nextBar() {
    if (this.currentIndex < this.marketData.length - 1) {
      this.currentIndex++;
  
      // If user has an open position, calculate P&L
      if (this.position !== PositionState.None && this.entryPrice !== null) {
        const currentPrice = this.marketData[this.currentIndex].close;
  
        if (this.position === PositionState.Long) {
          this.pnl = currentPrice - this.entryPrice;
        } else if (this.position === PositionState.Short) {
          this.pnl = this.entryPrice - currentPrice;
        }
  
        console.log('Updated P&L:', this.pnl);
      }
    } else {
      console.log('No more data available.');
    }
  }
  

  
}
