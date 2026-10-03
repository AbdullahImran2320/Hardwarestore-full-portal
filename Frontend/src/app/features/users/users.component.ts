import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../core/services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { AppUser } from '../../core/models/user.model';
import { httpErrorMessage } from '../../core/util/http-error';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  private userService = inject(UserService);
  authService = inject(AuthService);

  users = signal<AppUser[]>([]);
  isLoading = signal(true);
  errorMsg = signal<string | null>(null);
  notice = signal<string | null>(null);

  // Add user modal
  isAddOpen = signal(false);
  newUsername = signal('');
  newPassword = signal('');
  newRole = signal('Staff');
  isSaving = signal(false);
  formError = signal<string | null>(null);

  // Reset password modal
  resetTarget = signal<AppUser | null>(null);
  resetPassword = signal('');

  ngOnInit() {
    this.load();
  }

  load() {
    this.isLoading.set(true);
    this.errorMsg.set(null);
    this.userService.getAll().subscribe({
      next: list => { this.users.set(list); this.isLoading.set(false); },
      error: err => { this.errorMsg.set(httpErrorMessage(err, 'Could not load users.')); this.isLoading.set(false); }
    });
  }

  isMe(user: AppUser) {
    return this.authService.currentUser()?.username === user.username;
  }

  // ---------- add ----------
  openAdd() {
    this.newUsername.set('');
    this.newPassword.set('');
    this.newRole.set('Staff');
    this.formError.set(null);
    this.isAddOpen.set(true);
  }

  closeAdd() {
    this.isAddOpen.set(false);
  }

  saveNew() {
    const username = this.newUsername().trim();
    const password = this.newPassword();

    if (username.length < 3 || username.length > 30) {
      this.formError.set('Username must be 3 to 30 characters.');
      return;
    }
    if (password.length < 8 || !/[A-Za-z]/.test(password) || !/[0-9]/.test(password)) {
      this.formError.set('Password needs at least 8 characters, with a letter and a number.');
      return;
    }

    this.isSaving.set(true);
    this.formError.set(null);
    this.userService.create({ username, password, role: this.newRole() }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAdd();
        this.notice.set(`User "${username}" created. They will choose their own password at first login.`);
        this.load();
      },
      error: err => {
        this.isSaving.set(false);
        this.formError.set(httpErrorMessage(err, 'Could not create the user.'));
      }
    });
  }

  // ---------- reset password ----------
  openReset(user: AppUser) {
    this.resetTarget.set(user);
    this.resetPassword.set('');
    this.formError.set(null);
  }

  closeReset() {
    this.resetTarget.set(null);
  }

  saveReset() {
    const target = this.resetTarget();
    if (!target) return;

    const password = this.resetPassword();
    if (password.length < 8 || !/[A-Za-z]/.test(password) || !/[0-9]/.test(password)) {
      this.formError.set('Password needs at least 8 characters, with a letter and a number.');
      return;
    }

    this.isSaving.set(true);
    this.formError.set(null);
    this.userService.resetPassword(target.id, password).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeReset();
        this.notice.set(`Password for "${target.username}" was reset. They must choose a new one at next login.`);
        this.load();
      },
      error: err => {
        this.isSaving.set(false);
        this.formError.set(httpErrorMessage(err, 'Could not reset the password.'));
      }
    });
  }

  // ---------- delete ----------
  remove(user: AppUser) {
    if (!confirm(`Delete the user "${user.username}"? This cannot be undone.`)) return;

    this.errorMsg.set(null);
    this.userService.delete(user.id).subscribe({
      next: () => {
        this.notice.set(`User "${user.username}" deleted.`);
        this.load();
      },
      error: err => this.errorMsg.set(httpErrorMessage(err, 'Could not delete the user.'))
    });
  }
}
