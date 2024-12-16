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
  private chartInitialized = false;





  ngAfterViewInit() {
    if (!this.chartInitialized) {
      this.initializeChart();
      this.setupResizeObserver();
      this.chartInitialized = true;
    }
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
        rightOffset: 9, // Create space for 9 virtual bars
        barSpacing: 20, // Adjust spacing for clear visibility
        fixLeftEdge: true, // Prevent panning past the first bar
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

  public setData(data: BarData[], startBarIndex: number = 0) {
    console.log('Setting chart data:', data, 'Starting at bar index:', startBarIndex);

    if (!data || data.length === 0) {
      console.error('No data provided to setData.');
      return;
    }

    this.dayData = data.map((bar, index) => ({
      time: (index + 1) as UTCTimestamp,
      open: bar.open,
      high: bar.high,
      low: bar.low,
      close: bar.close,
    }));

    this.currentBarIndex = startBarIndex > 0 ? startBarIndex : 1;

    if (!this.chart || !this.candlestickSeries) {
      console.error('Chart or candlestick series is not initialized. Retrying in 50ms...');
      setTimeout(() => this.setData(data, startBarIndex), 50);
      return;
    }

    console.log('Chart initialized, setting data from bar:', this.currentBarIndex);

    // Display all bars up to the current bar index
    this.candlestickSeries.setData(this.dayData.slice(0, this.currentBarIndex));

    // Adjust visible range to simulate 10 bars filling the screen
    const visibleBars = Math.max(10, this.currentBarIndex + 9); // Ensure space for 10 bars
    const from: UTCTimestamp = Math.max(-9, this.dayData[0]?.time - 9) as UTCTimestamp;
    const to: UTCTimestamp = this.dayData[this.currentBarIndex - 1]?.time ?? 0;

    this.chart.timeScale().setVisibleRange({ from, to });
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

  public getTotalBars(): number {
    return this.dayData.length;
  }

  public getCurrentBarIndex(): number {
    return this.currentBarIndex;
  }

}
