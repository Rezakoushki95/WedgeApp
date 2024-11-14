import { Component, OnInit, ViewChild } from '@angular/core';
import { LightweightChartComponent } from '../lightweight-chart/lightweight-chart.component';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss']
})
export class HomePage implements OnInit {
  @ViewChild(LightweightChartComponent) lightweightChart!: LightweightChartComponent;

  ngOnInit(): void {}

  onNextBar() {
    if (this.lightweightChart) {
      this.lightweightChart.showNextBar();
    }
  }
}
