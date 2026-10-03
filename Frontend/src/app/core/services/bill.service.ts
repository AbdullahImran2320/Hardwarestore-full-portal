import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Bill, CreateBill } from '../models/bill.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BillService {
  private baseUrl = `${environment.apiUrl}/bills`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Bill[]> {
    return this.http.get<Bill[]>(this.baseUrl);
  }

  getById(id: number): Observable<Bill> {
    return this.http.get<Bill>(`${this.baseUrl}/${id}`);
  }

  create(bill: CreateBill): Observable<Bill> {
    return this.http.post<Bill>(this.baseUrl, bill);
  }

  getByCustomer(customerId: number): Observable<Bill[]> {
    return this.http.get<Bill[]>(`${this.baseUrl}/customer/${customerId}`);
  }

  getToday(): Observable<Bill[]> {
    return this.http.get<Bill[]>(`${this.baseUrl}/today`);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}