import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ExportKind, ImportKind, ImportPreview, ImportResult } from '../models/import.model';

@Injectable({ providedIn: 'root' })
export class ExcelService {
  private exportUrl = `${environment.apiUrl}/export`;
  private importUrl = `${environment.apiUrl}/import`;

  constructor(private http: HttpClient) {}

  exportFile(kind: ExportKind, from?: string, to?: string): Observable<HttpResponse<Blob>> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get(`${this.exportUrl}/${kind}`, { params, responseType: 'blob', observe: 'response' });
  }

  downloadTemplate(kind: ImportKind): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.importUrl}/${kind}/template`, { responseType: 'blob', observe: 'response' });
  }

  preview(kind: ImportKind, file: File): Observable<ImportPreview> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ImportPreview>(`${this.importUrl}/${kind}/preview`, form);
  }

  commit(kind: ImportKind, file: File, skipInvalid: boolean): Observable<ImportResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('skipInvalid', skipInvalid ? 'true' : 'false');
    return this.http.post<ImportResult>(`${this.importUrl}/${kind}/commit`, form);
  }
}
