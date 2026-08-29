import { Injectable, signal, PLATFORM_ID, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { isPlatformBrowser } from '@angular/common';

export interface AuthResponse {
  token: string;
  username: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = `${environment.apiUrl}/auth`;
  private isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  currentUser = signal<{ username: string; role: string } | null>(null);

 

  constructor(private http: HttpClient) {}

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { username, password }).pipe(
      tap(res => this.saveSession(res))
    );
  }

  logout() {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');
    this.currentUser.set(null);
  }

getToken(): string | null {
  if (!this.isBrowser) return null;
  return localStorage.getItem('auth_token');
}

  isAdmin(): boolean {
    return this.currentUser()?.role === 'Admin';
  }

private saveSession(res: AuthResponse) {
  if (this.isBrowser) {
    localStorage.setItem('auth_token', res.token);
    localStorage.setItem('auth_user', JSON.stringify({ username: res.username, role: res.role }));
  }
  this.currentUser.set({ username: res.username, role: res.role });
}

private loadFromStorage(): { username: string; role: string } | null {
  if (!this.isBrowser) return null;
  const raw = localStorage.getItem('auth_user');
  return raw ? JSON.parse(raw) : null;
}
}