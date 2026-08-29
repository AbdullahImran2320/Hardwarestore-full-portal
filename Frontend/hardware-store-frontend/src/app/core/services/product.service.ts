import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product, CreateProduct, UpdateProduct, ProductCost, UpdateProductCost, AdjustStock } from '../models/product.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private baseUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.baseUrl);
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  create(product: CreateProduct): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, product);
  }

  update(id: number, product: UpdateProduct): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, product);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getLowStock(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/low-stock`);
  }
  getCost(id: number): Observable<ProductCost> {
  return this.http.get<ProductCost>(`${this.baseUrl}/${id}/cost`);
}

updateCost(id: number, dto: UpdateProductCost): Observable<void> {
  return this.http.put<void>(`${this.baseUrl}/${id}/cost`, dto);
}

adjustStock(id: number, dto: AdjustStock): Observable<Product> {
  return this.http.post<Product>(`${this.baseUrl}/${id}/stock-adjustments`, dto);
}
}