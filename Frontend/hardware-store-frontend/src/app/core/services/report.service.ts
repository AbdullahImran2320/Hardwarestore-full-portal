import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DailySalesReport } from '../models/report.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private baseUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getDailySales(date?: string): Observable<DailySalesReport> {
    const url = date ? `${this.baseUrl}/daily-sales?date=${date}` : `${this.baseUrl}/daily-sales`;
    return this.http.get<DailySalesReport>(url);
  }
}