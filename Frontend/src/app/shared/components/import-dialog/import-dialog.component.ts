import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExcelService } from '../../../core/services/excel.service';
import { ImportKind, ImportPreview, ImportResult } from '../../../core/models/import.model';
import { saveResponse } from '../../../core/util/download.util';
import { httpErrorMessage, httpErrorMessageAsync } from '../../../core/util/http-error';

@Component({
  selector: 'app-import-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './import-dialog.component.html',
  styleUrl: './import-dialog.component.scss'
})
export class ImportDialogComponent {
  private excel = inject(ExcelService);

  kind = input.required<ImportKind>();
  closed = output<void>();
  imported = output<void>();

  file = signal<File | null>(null);
  preview = signal<ImportPreview | null>(null);
  result = signal<ImportResult | null>(null);
  error = signal<string | null>(null);
  busy = signal(false);
  skipInvalid = signal(false);

  title() {
    return this.kind() === 'products' ? 'Import products from Excel' : 'Import customers from Excel';
  }

  downloadTemplate() {
    this.error.set(null);
    this.excel.downloadTemplate(this.kind()).subscribe({
      next: res => saveResponse(res, `${this.kind()}-import-template.xlsx`),
      error: async err => this.error.set(await httpErrorMessageAsync(err, 'Could not download the template.'))
    });
  }

  onFileChosen(event: Event) {
    const el = event.target as HTMLInputElement;
    const chosen = el.files && el.files.length > 0 ? el.files[0] : null;
    this.file.set(chosen);
    this.preview.set(null);
    this.result.set(null);
    this.error.set(null);
    this.skipInvalid.set(false);
  }

  runPreview() {
    const file = this.file();
    if (!file) {
      this.error.set('Choose an Excel (.xlsx) file first.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);

    this.excel.preview(this.kind(), file).subscribe({
      next: p => {
        this.preview.set(p);
        this.busy.set(false);
      },
      error: err => {
        this.error.set(httpErrorMessage(err, 'Could not read the file.'));
        this.busy.set(false);
      }
    });
  }

  importableRows(): number {
    const p = this.preview();
    if (!p) return 0;
    return p.toCreate + p.toUpdate;
  }

  canImport(): boolean {
    const p = this.preview();
    if (!p || this.busy()) return false;
    if (this.importableRows() === 0) return false;
    return p.errorCount === 0 || this.skipInvalid();
  }

  runImport() {
    const file = this.file();
    if (!file || !this.canImport()) return;

    this.busy.set(true);
    this.error.set(null);

    this.excel.commit(this.kind(), file, this.skipInvalid()).subscribe({
      next: r => {
        this.result.set(r);
        this.busy.set(false);
        this.imported.emit();
      },
      error: err => {
        this.error.set(httpErrorMessage(err, 'The import failed. Nothing was saved.'));
        this.busy.set(false);
      }
    });
  }

  close() {
    this.closed.emit();
  }
}
