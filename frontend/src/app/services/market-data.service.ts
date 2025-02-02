import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BarData } from '../models/bar-data.model';

@Injectable({
  providedIn: 'root'
})
export class MarketDataService {
  private apiUrl = 'http://192.168.1.11:5068/api/TradingSession';

  constructor(private http: HttpClient) { }

  // Fetch bars for the current trading session
  getBarsForSession(sessionId: number) {
    const url = `http://192.168.1.11:5068/api/TradingSession/get-bars?sessionId=${sessionId}`;
    return this.http.get<BarData[]>(url);
  }

}
