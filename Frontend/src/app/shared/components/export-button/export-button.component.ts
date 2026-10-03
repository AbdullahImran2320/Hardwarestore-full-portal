import { Component, inject, input, signal } from '@angular/core';
import { ExcelService } from '../../../core/services/excel.service';
import { ExportKind } from '../../../core/models/import.model';
import { saveResponse } from '../../../core/util/download.util';
import { httpErrorMessageAsync } from '../../../core/util/http-error';

@Component({
  selector: 'app-export-button',
  standalone: true,
  template: `
    <button type="button" class="btn btn-secondary" (click)="download()" [disabled]="busy()">
      {{ busy() ? 'Preparing...' : '⬇ Export Excel' }}
    </button>
    @if (error()) {
      <span class="export-error">{{ error() }}</span>
    }
  `,
  styles: [`
    .export-error { color: var(--color-danger); font-size: 0.8rem; margin-left: 0.5rem; }
  `]
})
export class ExportButtonComponent {
  private excel = inject(ExcelService);

  kind = input.required<ExportKind>();

  busy = signal(false);
  error = signal<string | null>(null);

  download() {
    const kind = this.kind();
    this.busy.set(true);
    this.error.set(null);

    this.excel.exportFile(kind).subscribe({
      next: res => {
        saveResponse(res, `${kind}.xlsx`);
        this.busy.set(false);
      },
      error: async err => {
        this.error.set(await httpErrorMessageAsync(err, 'Export failed.'));
        this.busy.set(false);
      }
    });
  }
}
