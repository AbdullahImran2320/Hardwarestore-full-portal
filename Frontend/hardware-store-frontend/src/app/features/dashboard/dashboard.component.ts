import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReportService } from '../../core/services/report.service';
import { ProductService } from '../../core/services/product.service';
import { DailySalesReport } from '../../core/models/report.model';
import { Product } from '../../core/models/product.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  report = signal<DailySalesReport | null>(null);
  lowStockProducts = signal<Product[]>([]);
  isLoading = signal(true);
  errorMsg = signal<string | null>(null);

  constructor(
    private reportService: ReportService,
    private productService: ProductService
  ) {}

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.isLoading.set(true);
    this.errorMsg.set(null);

    this.reportService.getDailySales().subscribe({
      next: (data) => {
        this.report.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMsg.set('Could not load today\'s sales. Is the API running?');
        this.isLoading.set(false);
      }
    });

    this.productService.getLowStock().subscribe({
      next: (data) => this.lowStockProducts.set(data),
      error: () => {} // non-critical, dashboard still usable without this
    });
  }
}