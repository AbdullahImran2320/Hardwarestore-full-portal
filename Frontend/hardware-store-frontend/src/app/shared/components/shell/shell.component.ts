import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  isMobileNavOpen = signal(false);

  navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: 'grid' },
    { path: '/inventory', label: 'Inventory', icon: 'box' },
    { path: '/billing', label: 'New Bill', icon: 'receipt' },
    { path: '/bills', label: 'All Bills', icon: 'list' },
    { path: '/customers', label: 'Customers', icon: 'users' },
  ];
  constructor(public authService: AuthService, private router: Router) {}

logout() {
  this.authService.logout();
  this.router.navigate(['/login']);
}

  toggleMobileNav() {
    this.isMobileNavOpen.update(v => !v);
  }

  closeMobileNav() {
    this.isMobileNavOpen.set(false);
  }
}