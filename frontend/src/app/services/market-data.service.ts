import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BarData } from '../models/bar-data.model';

export interface MarketDataDay {
  id: number;
  date: string;
  fiveMinuteBars: BarData[];
  marketDataMonthId: number;
  accessedDays: any[];
}

@Injectable({
  providedIn: 'root'
})
export class MarketDataService {
  private apiUrl = 'http://localhost:5068/api/MarketData';

  constructor(private http: HttpClient) {}

  // Fetch unaccessed day and return it as a MarketDataDay
  getUnaccessedDay(userId: number = 2): Observable<MarketDataDay> {
    const url = `${this.apiUrl}/unaccessed-day?userId=${userId}`;
    return this.http.get<MarketDataDay>(url);
  }
}
