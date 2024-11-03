import { Component, OnInit } from '@angular/core';
import { MarketDataService } from '../services/market-data.service';

export enum PositionState {
  None,
  Long,
  Short,
}

interface MarketDataEntry {
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
  marketData: MarketDataEntry[] = [];
  currentBarIndex: number = 0;

  constructor(private marketDataService: MarketDataService) {}

  ngOnInit() {
    this.fetchMarketData();
  }

  fetchMarketData() {
    this.marketDataService.getRandomDayData().subscribe({
      next: (data: MarketDataEntry[]) => {
        this.marketData = data
      },
      error: (error) => {
        console.error('Error fetching market data:', error);
      },
    });
  }
}
