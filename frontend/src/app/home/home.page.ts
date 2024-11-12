import { Component, OnInit } from '@angular/core';
import { MarketDataService } from '../services/market-data.service';
import { BarData } from '../models/bar-data.model';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
})
export class HomePage implements OnInit {
  bars: BarData[] = [];

  constructor(private marketDataService: MarketDataService) {}

  ngOnInit() {
    this.fetchRandomDay();
  }

  fetchRandomDay(userId?: number) {
    this.marketDataService.getUnaccessedDay(userId).subscribe({
      next: (data) => {
        console.log("Fetched data:", data); // Log to inspect the structure
        this.bars = data;
      },
      error: (err) => {
        console.error('Error fetching unaccessed day:', err);
      },
    });
  }
  
}
