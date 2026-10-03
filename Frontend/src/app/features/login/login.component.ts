import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ThemeToggleComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  username = signal('');
  password = signal('');
  isLoading = signal(false);
  errorMsg = signal<string | null>(null);

  constructor(private authService: AuthService, private router: Router) {}

  submit() {
    if (!this.username().trim() || !this.password().trim()) {
      this.errorMsg.set('Enter username and password.');
      return;
    }
    this.isLoading.set(true);
    this.errorMsg.set(null);

    this.authService.login(this.username(), this.password()).subscribe({
      next: res => { this.isLoading.set(false); this.router.navigate([res.mustChangePassword ? '/account' : '/dashboard']); },
      error: () => { this.isLoading.set(false); this.errorMsg.set('Invalid username or password.'); }
    });
  }
}