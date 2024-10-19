import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class MarketDataService {
  private apiUrl = 'http://localhost:5068/api/marketdata/randomday';

  constructor(private http: HttpClient) {}

  getRandomDayData(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }
}
