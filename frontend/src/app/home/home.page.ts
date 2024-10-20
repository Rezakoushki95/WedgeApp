import { Component, OnInit } from '@angular/core';
import { MarketDataService } from '../services/market-data.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
})
export class HomePage implements OnInit {
  marketData: any[] = [];
  currentIndex: number = -1;

  // State variables for tracking position
  position: 'long' | 'short' | null = null; // To track user's position
  entryPrice: number | null = null; // The price at which the user enters a position
  pnl: number = 0; // Track profit or loss for the position

  constructor(private marketDataService: MarketDataService) {}

  ngOnInit() {}

  fetchMarketData() {
    this.marketDataService.getRandomDayData().subscribe({
      next: (data) => {
        this.marketData = data;
        this.currentIndex = -1; // Reset index when new data is fetched
      },
      error: (error) => {
        console.error('Error fetching market data:', error);
      },
      complete: () => {
        console.log('Market data fetch completed.');
      },
    });
  }
}
