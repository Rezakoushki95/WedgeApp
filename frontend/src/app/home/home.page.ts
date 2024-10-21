import { Component, OnInit } from '@angular/core';
import { MarketDataService } from '../services/market-data.service';

export enum PositionState {
  None,
  Long,
  Short,
}

interface MarketData {
  time: string;
  open: number;
  high: number;
  low: number;
  close: number;
}

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
})
export class HomePage implements OnInit {
  marketData: MarketData[] = [];
  currentBarIndex: number = -1;

  position: PositionState = PositionState.None;
  entryPrice: number | null = null;
  dailyProfitLoss: number = 0;
  positionsTaken: number = 0;

  constructor(private marketDataService: MarketDataService) {}

  ngOnInit() {
    this.fetchMarketData();
  }

  get hasOpenPosition(): boolean {
    return this.position !== PositionState.None;
  }

  fetchMarketData() {
    this.marketDataService.getRandomDayData().subscribe({
      next: (data: MarketData[]) => {
        this.marketData = data;
        this.currentBarIndex = 0;
      },
      error: (error) => {
        console.error('Error fetching market data:', error);
        // Consider showing an error toast here
      },
      complete: () => {
        console.log('Market data fetch completed.');
      },
    });
  }

  nextBar() {
    if (this.currentBarIndex < this.marketData.length - 1) {
      this.currentBarIndex++;
      this.updatePnL();
    }
  }

  enterLong() {
    if (!this.hasOpenPosition && this.currentBarIndex >= 0) {
      this.position = PositionState.Long;
      this.entryPrice = this.marketData[this.currentBarIndex].close;
      this.positionsTaken++;
    }
  }

  enterShort() {
    if (!this.hasOpenPosition && this.currentBarIndex >= 0) {
      this.position = PositionState.Short;
      this.entryPrice = this.marketData[this.currentBarIndex].close;
      this.positionsTaken++;
    }
  }

  exitPosition() {
    if (this.hasOpenPosition) {
      this.updatePnL();
      this.position = PositionState.None;
      this.entryPrice = null;
    }
  }

  updatePnL() {
    if (this.hasOpenPosition && this.entryPrice !== null && this.currentBarIndex >= 0) {
      const currentPrice = this.marketData[this.currentBarIndex].close;
      this.dailyProfitLoss += this.position === PositionState.Long ? currentPrice - this.entryPrice : this.entryPrice - currentPrice;
    }
  }
}
