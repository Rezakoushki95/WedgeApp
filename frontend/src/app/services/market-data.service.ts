import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BarData } from '../models/bar-data.model';

@Injectable({
  providedIn: 'root'
})
export class MarketDataService {
  private apiUrl = 'http://localhost:5068/api/MarketData';

  constructor(private http: HttpClient) {}

  // Method to fetch unaccessed day
  getUnaccessedDay(userId: number = 2): Observable<BarData[]> {
    const url = `${this.apiUrl}/unaccessed-day?userId=${userId}`;
    return this.http.get<BarData[]>(url);
  }
}
