import { Component, OnInit } from '@angular/core';
import { MarketDataService } from '../services/market-data.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
})
export class HomePage implements OnInit {
  marketData: any[] = []; // Make sure it's an array type

  constructor(private marketDataService: MarketDataService) {}

  ngOnInit() {
  }

  fetchMarketData() {
    this.marketDataService.getRandomDayData().subscribe({
      next: (data) => {
        this.marketData = data;
        console.log(this.marketData); // Log the data to verify it's being fetched
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
