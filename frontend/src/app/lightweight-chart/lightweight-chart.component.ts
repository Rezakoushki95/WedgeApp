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
export class LightweightChartComponent implements AfterViewInit {
  @ViewChild('chartContainer', { static: true }) chartContainer!: ElementRef;
  private chart!: IChartApi;
  private candlestickSeries!: ISeriesApi<"Candlestick">;
  private resizeObserver!: ResizeObserver;
  private currentBarIndex = 0;
  private dayData: BarData[] = []; // Stores the day's data
  private chartInitialized = false;

  ngAfterViewInit() {
    requestAnimationFrame(() => {
      const containerWidth = this.chartContainer.nativeElement.clientWidth;
      const containerHeight = this.chartContainer.nativeElement.clientHeight;

      if (containerWidth > 0 && containerHeight > 0) {
        console.log('Chart container dimensions are valid. Initializing chart...');
        this.initializeChart();
        this.setupResizeObserver();
        this.chartInitialized = true;
      } else {
        console.warn('Chart container not ready. Retrying...');
        setTimeout(() => this.ngAfterViewInit(), 50);
      }
    });
  }



  public cleanup() {
    console.log('Disconnecting ResizeObserver and cleaning up chart.');
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
      this.resizeObserver = null as unknown as ResizeObserver;
    }
    if (this.chart) {
      this.chart.remove();
      this.chart = null as unknown as IChartApi;
    }
  }

  private initializeChart() {
    const containerWidth = this.chartContainer.nativeElement.clientWidth;
    const containerHeight = this.chartContainer.nativeElement.clientHeight;

    this.chart = createChart(this.chartContainer.nativeElement, {
      width: containerWidth,
      height: containerHeight,
      timeScale: {
        fixLeftEdge: true,
        lockVisibleTimeRangeOnResize: true,
        tickMarkFormatter: (time: UTCTimestamp) => {
          return Math.round(time).toString();
        },
      },
      handleScroll: {
        vertTouchDrag: false,
        horzTouchDrag: false,
      },
      handleScale: {
        axisPressedMouseMove: false,
        pinch: false,
        mouseWheel: false,
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
      return this.dayData[this.currentBarIndex - 1];
    }
    return null;
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

    this.candlestickSeries.setData(this.dayData.slice(0, this.currentBarIndex));

    // Adjust the viewport consistently
    this.adjustViewport();
  }

  public showNextBar() {
    if (this.currentBarIndex < this.dayData.length) {
      this.candlestickSeries.update(this.dayData[this.currentBarIndex]);
      this.currentBarIndex += 1;

      // Adjust the viewport consistently
      this.adjustViewport();
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

  private adjustViewport() {
    const from = this.dayData[0]?.time ?? 0; // Always start from the first bar
    const to = this.dayData[this.currentBarIndex - 1]?.time ?? 0; // End at the current bar

    // After 10 bars, show all bars dynamically
    this.chart.timeScale().setVisibleRange({ from, to });
    console.log(`After 10 bars: Adjusted range from ${from} to ${to}`);

  }
}
