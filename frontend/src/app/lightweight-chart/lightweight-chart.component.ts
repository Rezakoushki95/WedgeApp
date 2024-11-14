import { Component, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { createChart, IChartApi, ISeriesApi, UTCTimestamp } from 'lightweight-charts';

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

  // Sample data with numeric "time" values
  private data = [
    { time: (1 as UTCTimestamp), open: 584.59, high: 584.9, low: 584.44, close: 584.7198 },
    { time: (2 as UTCTimestamp), open: 584.72, high: 584.84, low: 584.35, close: 584.4001 },
    { time: (3 as UTCTimestamp), open: 584.44, high: 584.64, low: 584.35, close: 584.36 },
    { time: (4 as UTCTimestamp), open: 584.36, high: 584.73, low: 584.32, close: 584.64 },
    { time: (5 as UTCTimestamp), open: 584.64, high: 584.72, low: 584.4, close: 584.72 },
    { time: (6 as UTCTimestamp), open: 584.73, high: 584.83, low: 584.11, close: 584.215 },
    { time: (7 as UTCTimestamp), open: 584.21, high: 584.63, low: 584.2, close: 584.53 },
    { time: (8 as UTCTimestamp), open: 584.54, high: 584.57, low: 584.175, close: 584.19 },
    { time: (9 as UTCTimestamp), open: 584.18, high: 584.2, low: 583.75, close: 583.88 },
    { time: (10 as UTCTimestamp), open: 583.87, high: 584.18, low: 583.81, close: 584.18 },
    { time: (11 as UTCTimestamp), open: 584.17, high: 584.175, low: 583.8, close: 583.96 },
    { time: (12 as UTCTimestamp), open: 583.96, high: 584.0, low: 583.19, close: 583.2576 },
    { time: (13 as UTCTimestamp), open: 583.24, high: 583.31, low: 582.14, close: 582.4999 },
    { time: (14 as UTCTimestamp), open: 582.48, high: 582.55, low: 581.65, close: 581.79 },
    { time: (15 as UTCTimestamp), open: 581.79, high: 582.28, low: 581.29, close: 581.9099 },
    { time: (16 as UTCTimestamp), open: 581.89, high: 582.29, low: 581.7, close: 582.04 },
    { time: (17 as UTCTimestamp), open: 582.0171, high: 582.82, low: 581.94, close: 582.64 },
    { time: (18 as UTCTimestamp), open: 582.63, high: 582.8101, low: 581.9, close: 581.95 },
    { time: (19 as UTCTimestamp), open: 581.97, high: 582.49, low: 581.8201, close: 582.13 },
    { time: (20 as UTCTimestamp), open: 582.14, high: 582.3, low: 581.87, close: 581.9527 },
    { time: (21 as UTCTimestamp), open: 581.95, high: 582.32, low: 581.83, close: 582.31 },
    { time: (22 as UTCTimestamp), open: 582.31, high: 582.8, low: 582.29, close: 582.5 },
    { time: (23 as UTCTimestamp), open: 582.51, high: 582.64, low: 582.3101, close: 582.34 },
    { time: (24 as UTCTimestamp), open: 582.3301, high: 582.42, low: 582.2215, close: 582.32 },
    { time: (25 as UTCTimestamp), open: 582.32, high: 582.66, low: 582.2, close: 582.6 },
    { time: (26 as UTCTimestamp), open: 582.58, high: 582.65, low: 582.1501, close: 582.21 },
    { time: (27 as UTCTimestamp), open: 582.2, high: 582.53, low: 581.9701, close: 582.1071 },
    { time: (28 as UTCTimestamp), open: 582.09, high: 582.09, low: 581.64, close: 581.81 },
    { time: (29 as UTCTimestamp), open: 581.82, high: 582.06, low: 581.33, close: 581.36 },
    { time: (30 as UTCTimestamp), open: 581.33, high: 581.73, low: 581.25, close: 581.3994 },
    { time: (31 as UTCTimestamp), open: 581.405, high: 581.6, low: 581.34, close: 581.36 },
    { time: (32 as UTCTimestamp), open: 581.365, high: 582.08, low: 581.34, close: 582.08 },
    { time: (33 as UTCTimestamp), open: 582.1, high: 582.23, low: 581.81, close: 581.9699 },
    { time: (34 as UTCTimestamp), open: 581.96, high: 582.27, low: 581.75, close: 582.25 },
    { time: (35 as UTCTimestamp), open: 582.25, high: 582.31, low: 581.87, close: 582.073 },
    { time: (36 as UTCTimestamp), open: 582.07, high: 582.22, low: 582.02, close: 582.12 },
    { time: (37 as UTCTimestamp), open: 582.14, high: 582.55, low: 582.04, close: 582.44 },
    { time: (38 as UTCTimestamp), open: 582.47, high: 582.4896, low: 582.23, close: 582.2901 },
    { time: (39 as UTCTimestamp), open: 582.33, high: 582.36, low: 581.77, close: 581.92 },
    { time: (40 as UTCTimestamp), open: 581.93, high: 582.02, low: 581.73, close: 581.84 },
    { time: (41 as UTCTimestamp), open: 581.84, high: 581.94, low: 581.64, close: 581.69 },
    { time: (42 as UTCTimestamp), open: 581.7, high: 581.84, low: 581.64, close: 581.8 },
    { time: (43 as UTCTimestamp), open: 581.79, high: 581.96, low: 581.74, close: 581.82 },
    { time: (44 as UTCTimestamp), open: 581.8205, high: 581.9399, low: 581.56, close: 581.57 },
    { time: (45 as UTCTimestamp), open: 581.56, high: 581.56, low: 581.1, close: 581.39 },
    { time: (46 as UTCTimestamp), open: 581.41, high: 581.63, low: 581.07, close: 581.3101 },
    { time: (47 as UTCTimestamp), open: 581.32, high: 581.47, low: 581.18, close: 581.21 },
    { time: (48 as UTCTimestamp), open: 581.21, high: 581.21, low: 580.82, close: 581.08 },
    { time: (49 as UTCTimestamp), open: 581.07, high: 581.28, low: 580.85, close: 581.03 },
    { time: (50 as UTCTimestamp), open: 581.03, high: 581.435, low: 580.95, close: 581.36 },
    { time: (51 as UTCTimestamp), open: 581.37, high: 581.75, low: 581.37, close: 581.6099 },
    { time: (52 as UTCTimestamp), open: 581.58, high: 581.74, low: 581.4, close: 581.485 },
    { time: (53 as UTCTimestamp), open: 581.49, high: 581.61, low: 581.27, close: 581.33 },
    { time: (54 as UTCTimestamp), open: 581.31, high: 581.31, low: 580.87, close: 580.98 },
    { time: (55 as UTCTimestamp), open: 581.0, high: 581.42, low: 580.75, close: 581.14 },
    { time: (56 as UTCTimestamp), open: 581.14, high: 581.21, low: 580.85, close: 580.885 },
    { time: (57 as UTCTimestamp), open: 580.89, high: 580.96, low: 580.63, close: 580.699 },
    { time: (58 as UTCTimestamp), open: 580.69, high: 580.69, low: 580.39, close: 580.43 },
    { time: (59 as UTCTimestamp), open: 580.42, high: 581.01, low: 580.36, close: 580.8516 },
    { time: (60 as UTCTimestamp), open: 580.85, high: 580.87, low: 580.39, close: 580.54 },
    { time: (61 as UTCTimestamp), open: 580.54, high: 580.78, low: 580.44, close: 580.46 },
    { time: (62 as UTCTimestamp), open: 580.46, high: 580.5399, low: 580.25, close: 580.51 },
    { time: (63 as UTCTimestamp), open: 580.51, high: 580.57, low: 580.25, close: 580.52 },
    { time: (64 as UTCTimestamp), open: 580.5102, high: 580.57, low: 580.19, close: 580.25 },
    { time: (65 as UTCTimestamp), open: 580.26, high: 580.455, low: 580.2, close: 580.34 },
    { time: (66 as UTCTimestamp), open: 580.335, high: 580.335, low: 580.0, close: 580.11 },
    { time: (67 as UTCTimestamp), open: 580.12, high: 580.19, low: 579.31, close: 579.4092 },
    { time: (68 as UTCTimestamp), open: 579.36, high: 579.586, low: 578.88, close: 579.22 },
    { time: (69 as UTCTimestamp), open: 579.23, high: 579.4602, low: 579.06, close: 579.1 },
    { time: (70 as UTCTimestamp), open: 579.12, high: 579.3, low: 579.01, close: 579.29 },
    { time: (71 as UTCTimestamp), open: 579.3, high: 579.88, low: 579.21, close: 579.6618 },
    { time: (72 as UTCTimestamp), open: 579.68, high: 579.8199, low: 579.28, close: 579.31 },
    { time: (73 as UTCTimestamp), open: 579.34, high: 579.48, low: 578.82, close: 578.8799 },
    { time: (74 as UTCTimestamp), open: 578.88, high: 579.2161, low: 578.545, close: 579.09 },
    { time: (75 as UTCTimestamp), open: 579.1, high: 579.69, low: 579.1, close: 579.22 },
    { time: (76 as UTCTimestamp), open: 579.21, high: 579.64, low: 579.0541, close: 579.55 },
    { time: (77 as UTCTimestamp), open: 579.56, high: 580.195, low: 579.31, close: 580.02 },
    { time: (78 as UTCTimestamp), open: 580.01, high: 580.01, low: 579.11, close: 579.79 },
  ];
  

  ngAfterViewInit() {
    this.initializeChart();

    // Set up ResizeObserver to watch for size changes on chartContainer
    this.resizeObserver = new ResizeObserver(entries => {
      for (let entry of entries) {
        const { width, height } = entry.contentRect;
        this.chart.resize(width, height); // Resize chart to container's dimensions
      }
    });

    // Observe the chart container for resizing
    this.resizeObserver.observe(this.chartContainer.nativeElement);
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
          return time.toString(); // Custom labels: use "time" value as x-axis label
        },
      },
    });

    this.candlestickSeries = this.chart.addCandlestickSeries();

    // Show only the first bar initially
    if (this.data.length > 0) {
      this.candlestickSeries.setData([this.data[0]]);
      this.currentBarIndex = 1;
    }
  }

  // Method to display the next bar
  public showNextBar() {
    if (this.currentBarIndex < this.data.length) {
      this.candlestickSeries.update(this.data[this.currentBarIndex]);
      this.currentBarIndex += 1;
    } else {
      console.log("No more bars to show!");
    }
  }
}
