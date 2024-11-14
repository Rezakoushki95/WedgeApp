import { Component, OnInit, ViewChild } from '@angular/core';
import { LightweightChartComponent } from '../lightweight-chart/lightweight-chart.component';
import { MarketDataService } from '../services/market-data.service';
import { MarketDataDay } from '../services/market-data.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss']
})
export class HomePage implements OnInit {
  @ViewChild(LightweightChartComponent) lightweightChart!: LightweightChartComponent;

  constructor(private marketDataService: MarketDataService) {}

  ngOnInit(): void {
    this.loadDayData(); // Fetch and display data on component load
  }

  private loadDayData() {
    this.marketDataService.getUnaccessedDay().subscribe({
      next: (dayData: MarketDataDay) => {
        // Assuming `dayData.fiveMinuteBars` contains the array of bar data
        if (this.lightweightChart) {
          this.lightweightChart.setData(dayData.fiveMinuteBars);
        }
      },
      error: (error) => console.error("Error fetching market data:", error),
    });
  }

  onNextBar() {
    if (this.lightweightChart) {
      this.lightweightChart.showNextBar();
    }
  }
}
