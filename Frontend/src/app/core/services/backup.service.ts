import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BackupList, BackupResult, RestoreInfo } from '../models/backup.model';

@Injectable({ providedIn: 'root' })
export class BackupService {
  private baseUrl = `${environment.apiUrl}/backup`;

  constructor(private http: HttpClient) {}

  download(): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/download`, { responseType: 'blob', observe: 'response' });
  }

  copyToFolder(folderPath: string): Observable<BackupResult> {
    return this.http.post<BackupResult>(`${this.baseUrl}/copy`, { folderPath });
  }

  list(): Observable<BackupList> {
    return this.http.get<BackupList>(`${this.baseUrl}/list`);
  }

  validateRestore(file: File): Observable<RestoreInfo> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<RestoreInfo>(`${this.baseUrl}/restore/validate`, form);
  }

  restore(file: File, confirm: string): Observable<{ message: string; safetyBackup: string }> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('confirm', confirm);
    return this.http.post<{ message: string; safetyBackup: string }>(`${this.baseUrl}/restore`, form);
  }
}
