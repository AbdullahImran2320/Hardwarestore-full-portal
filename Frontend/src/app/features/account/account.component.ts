import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { httpErrorMessage } from '../../core/util/http-error';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss'
})
export class AccountComponent {
  authService = inject(AuthService);
  private router = inject(Router);

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  showPasswords = signal(false);

  isSaving = signal(false);
  errorMsg = signal<string | null>(null);
  successMsg = signal<string | null>(null);

  mustChange() {
    return this.authService.currentUser()?.mustChangePassword === true;
  }

  submit() {
    this.errorMsg.set(null);

    const current = this.currentPassword();
    const next = this.newPassword();
    const confirm = this.confirmPassword();

    if (!current || !next || !confirm) {
      this.errorMsg.set('Fill in all three fields.');
      return;
    }
    if (next.length < 8 || !/[A-Za-z]/.test(next) || !/[0-9]/.test(next)) {
      this.errorMsg.set('The new password needs at least 8 characters, with a letter and a number.');
      return;
    }
    if (next !== confirm) {
      this.errorMsg.set('New password and confirmation do not match.');
      return;
    }
    if (next === current) {
      this.errorMsg.set('The new password must be different from the current one.');
      return;
    }

    this.isSaving.set(true);
    this.authService.changePassword({ currentPassword: current, newPassword: next, confirmPassword: confirm }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.successMsg.set('Password changed. Please sign in again with your new password.');
        setTimeout(() => {
          this.authService.logout();
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: err => {
        this.isSaving.set(false);
        this.errorMsg.set(httpErrorMessage(err, 'Could not change the password.'));
      }
    });
  }
}
