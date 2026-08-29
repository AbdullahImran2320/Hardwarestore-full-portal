import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Payment, CreatePayment } from '../models/payment.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private baseUrl = `${environment.apiUrl}/payments`;

  constructor(private http: HttpClient) {}

  create(payment: CreatePayment): Observable<Payment> {
    return this.http.post<Payment>(this.baseUrl, payment);
  }

  getByBill(billId: number): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.baseUrl}/bill/${billId}`);
  }
}