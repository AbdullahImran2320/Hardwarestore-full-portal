import { Component, inject } from '@angular/core';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  template: `
    <button
      type="button"
      class="theme-toggle"
      (click)="theme.toggle()"
      [attr.aria-label]="theme.theme() === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'"
      [title]="theme.theme() === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'">
      {{ theme.theme() === 'dark' ? '☀️' : '🌙' }}
    </button>
  `,
  styles: [`
    .theme-toggle {
      width: 34px;
      height: 34px;
      border-radius: 50%;
      border: 1px solid rgba(128, 128, 128, 0.45);
      background: transparent;
      cursor: pointer;
      font-size: 1rem;
      line-height: 1;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    .theme-toggle:hover { background: rgba(128, 128, 128, 0.2); }
  `]
})
export class ThemeToggleComponent {
  theme = inject(ThemeService);
}
