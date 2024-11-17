import { Component, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { createChart, IChartApi, ISeriesApi, UTCTimestamp } from 'lightweight-charts';
import { BarData } from '../models/bar-data.model';

@Component({
  selector: 'app-lightweight-chart',
  template: `<div #chartContainer class="chart-container"></div>`,
  styles: [
    ':host { display: block; height: 100%; width: 100%; }',
    '.chart-container { width: 100%; height: 100%; }'
  ]
})
export class LightweightChartComponent implements AfterViewInit, OnDestroy {
  @ViewChild('chartContainer', { static: true }) chartContainer!: ElementRef;
  private chart!: IChartApi;
  private candlestickSeries!: ISeriesApi<"Candlestick">;
  private resizeObserver!: ResizeObserver;
  private currentBarIndex = 0;
  private dayData: BarData[] = []; // Stores the day's data

  ngAfterViewInit() {
    this.initializeChart();
    this.setupResizeObserver();
  }

  ngOnDestroy() {
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  private initializeChart() {
    const containerWidth = this.chartContainer.nativeElement.clientWidth;
    const containerHeight = this.chartContainer.nativeElement.clientHeight;

    this.chart = createChart(this.chartContainer.nativeElement, {
      width: containerWidth,
      height: containerHeight,
      timeScale: {
        rightOffset: 0,
        barSpacing: 10,
        tickMarkFormatter: (time: number) => {
          return time.toString(); // Custom labels for bar number
        },
      },
    });

    this.candlestickSeries = this.chart.addCandlestickSeries();
  }

  private setupResizeObserver() {
    this.resizeObserver = new ResizeObserver(entries => {
      for (let entry of entries) {
        const { width, height } = entry.contentRect;
        this.chart.resize(width, height);
      }
    });
    this.resizeObserver.observe(this.chartContainer.nativeElement);
  }

  public getCurrentBar(): BarData | null {
    if (this.currentBarIndex > 0 && this.currentBarIndex <= this.dayData.length) {
      return this.dayData[this.currentBarIndex - 1]; // Return the current bar
    }
    return null; // Return null if no valid current bar
  }
  

  public setData(data: BarData[]) {
    // Map the data to use index as `time` directly and cast to `UTCTimestamp`
    this.dayData = data.map((bar, index) => ({
      time: (index + 1) as UTCTimestamp, // Use bar index as `time`
      open: bar.open,
      high: bar.high,
      low: bar.low,
      close: bar.close,
    }));
    this.currentBarIndex = 1;
  
    // Set initial chart data with the first bar only
    this.candlestickSeries.setData([this.dayData[0]]);
  }
  
  

  // Method to display the next bar
  public showNextBar() {
    if (this.currentBarIndex < this.dayData.length) {
      this.candlestickSeries.update(this.dayData[this.currentBarIndex]);
      this.currentBarIndex += 1;
    } else {
      console.log("No more bars to show!");
    }
  }
}
