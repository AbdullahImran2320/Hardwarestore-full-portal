import { Injectable, signal, PLATFORM_ID, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { isPlatformBrowser } from '@angular/common';
import { ChangePassword } from '../models/user.model';

export interface AuthResponse {
  token: string;
  username: string;
  role: string;
  mustChangePassword: boolean;
}

export interface SessionUser {
  username: string;
  role: string;
  mustChangePassword: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = `${environment.apiUrl}/auth`;
  private isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  currentUser = signal<SessionUser | null>(null);

  constructor(private http: HttpClient) {
    // Restore the session after a page refresh.
    this.currentUser.set(this.loadFromStorage());
  }

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { username, password }).pipe(
      tap(res => this.saveSession(res))
    );
  }

  changePassword(dto: ChangePassword): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, dto);
  }

  logout() {
    if (this.isBrowser) {
      localStorage.removeItem('auth_token');
      localStorage.removeItem('auth_user');
    }
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
    const user: SessionUser = {
      username: res.username,
      role: res.role,
      mustChangePassword: !!res.mustChangePassword
    };
    if (this.isBrowser) {
      localStorage.setItem('auth_token', res.token);
      localStorage.setItem('auth_user', JSON.stringify(user));
    }
    this.currentUser.set(user);
  }

  private loadFromStorage(): SessionUser | null {
    if (!this.isBrowser) return null;
    try {
      const raw = localStorage.getItem('auth_user');
      if (!raw || !localStorage.getItem('auth_token')) return null;
      const parsed = JSON.parse(raw);
      return {
        username: parsed.username,
        role: parsed.role,
        mustChangePassword: !!parsed.mustChangePassword
      };
    } catch {
      return null;
    }
  }
}
