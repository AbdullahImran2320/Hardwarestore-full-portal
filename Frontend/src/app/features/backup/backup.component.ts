import { Component, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BackupService } from '../../core/services/backup.service';
import { AuthService } from '../../core/services/auth.service';
import { BackupList, RestoreInfo } from '../../core/models/backup.model';
import { saveResponse } from '../../core/util/download.util';
import { httpErrorMessage, httpErrorMessageAsync } from '../../core/util/http-error';

const FOLDER_KEY = 'backup_folder';

@Component({
  selector: 'app-backup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './backup.component.html',
  styleUrl: './backup.component.scss'
})
export class BackupComponent implements OnInit {
  private backupService = inject(BackupService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  // list of backups in the default folder
  backups = signal<BackupList | null>(null);

  // download
  isDownloading = signal(false);

  // copy to folder
  folderPath = signal('');
  isCopying = signal(false);

  // messages for backup actions
  errorMsg = signal<string | null>(null);
  successMsg = signal<string | null>(null);

  // restore
  restoreFile = signal<File | null>(null);
  restoreInfo = signal<RestoreInfo | null>(null);
  confirmText = signal('');
  isChecking = signal(false);
  isRestoring = signal(false);
  restoreError = signal<string | null>(null);

  ngOnInit() {
    if (this.isBrowser) {
      try {
        this.folderPath.set(localStorage.getItem(FOLDER_KEY) ?? '');
      } catch {
        // ignore
      }
    }
    this.loadList();
  }

  loadList() {
    this.backupService.list().subscribe({
      next: list => this.backups.set(list),
      error: err => this.errorMsg.set(httpErrorMessage(err, 'Could not load the backup list.'))
    });
  }

  // ---------- download ----------
  download() {
    this.isDownloading.set(true);
    this.errorMsg.set(null);
    this.successMsg.set(null);

    this.backupService.download().subscribe({
      next: res => {
        saveResponse(res, 'hardwarestore-backup.db');
        this.isDownloading.set(false);
        this.successMsg.set('Backup downloaded. Keep the file somewhere safe, such as a USB drive.');
      },
      error: async err => {
        this.errorMsg.set(await httpErrorMessageAsync(err, 'The backup could not be created.'));
        this.isDownloading.set(false);
      }
    });
  }

  // ---------- copy to folder ----------
  copyToFolder() {
    const folder = this.folderPath().trim();
    this.isCopying.set(true);
    this.errorMsg.set(null);
    this.successMsg.set(null);

    this.backupService.copyToFolder(folder).subscribe({
      next: r => {
        this.isCopying.set(false);
        this.successMsg.set(`Backup saved to ${r.path} (${this.formatSize(r.sizeBytes)}).`);
        if (this.isBrowser && folder) {
          try {
            localStorage.setItem(FOLDER_KEY, folder);
          } catch {
            // ignore
          }
        }
        this.loadList();
      },
      error: err => {
        this.isCopying.set(false);
        this.errorMsg.set(httpErrorMessage(err, 'The backup could not be copied.'));
      }
    });
  }

  // ---------- restore ----------
  onRestoreFile(event: Event) {
    const el = event.target as HTMLInputElement;
    this.restoreFile.set(el.files && el.files.length > 0 ? el.files[0] : null);
    this.restoreInfo.set(null);
    this.restoreError.set(null);
    this.confirmText.set('');
  }

  checkRestoreFile() {
    const file = this.restoreFile();
    if (!file) {
      this.restoreError.set('Choose a backup (.db) file first.');
      return;
    }
    this.isChecking.set(true);
    this.restoreError.set(null);
    this.restoreInfo.set(null);

    this.backupService.validateRestore(file).subscribe({
      next: info => { this.restoreInfo.set(info); this.isChecking.set(false); },
      error: err => {
        this.restoreError.set(httpErrorMessage(err, 'This file cannot be used as a backup.'));
        this.isChecking.set(false);
      }
    });
  }

  canRestore() {
    return !!this.restoreInfo() && this.confirmText() === 'RESTORE' && !this.isRestoring();
  }

  restore() {
    const file = this.restoreFile();
    if (!file || !this.canRestore()) return;

    this.isRestoring.set(true);
    this.restoreError.set(null);

    this.backupService.restore(file, this.confirmText()).subscribe({
      next: () => {
        this.isRestoring.set(false);
        // The restored database may contain different users and passwords, so sign out.
        this.authService.logout();
        this.router.navigate(['/login']);
      },
      error: err => {
        this.isRestoring.set(false);
        this.restoreError.set(httpErrorMessage(err, 'The restore failed.'));
      }
    });
  }

  // The database stores dates as text like "2026-09-01 10:30:00.123"; show only the date and time.
  shortDate(value?: string | null): string {
    return value ? value.substring(0, 16).replace('T', ' ') : '';
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
