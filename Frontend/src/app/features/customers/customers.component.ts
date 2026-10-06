import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomerService } from '../../core/services/customer.service';
import { CustomerWithBalance } from '../../core/models/customer.model';
import { CreateCustomer } from '../../core/models/customer.model';
import { ExportButtonComponent } from '../../shared/components/export-button/export-button.component';
import { ImportDialogComponent } from '../../shared/components/import-dialog/import-dialog.component';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ExportButtonComponent, ImportDialogComponent],
  templateUrl: './customers.component.html',
  styleUrl: './customers.component.scss'
})
export class CustomersComponent implements OnInit {
  authService = inject(AuthService);
  showImport = signal(false);
  onImported() { this.loadCustomers(); }

  customers = signal<CustomerWithBalance[]>([]);
  isLoading = signal(true);
  errorMsg = signal<string | null>(null);
  searchTerm = signal('');

  isModalOpen = signal(false);
  isSaving = signal(false);
  formError = signal<string | null>(null);
  form: CreateCustomer = { name: '', phone: '', address: '' };

  filteredCustomers = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.customers();
    return this.customers().filter(c =>
      c.name.toLowerCase().includes(term) || c.phone.includes(term)
    );
  });

  totalOutstandingAcrossAll = computed(() =>
    this.customers().reduce((sum, c) => sum + c.totalOutstanding, 0)
  );

  constructor(private customerService: CustomerService) {}

  ngOnInit() {
    this.loadCustomers();
  }

  loadCustomers() {
    this.isLoading.set(true);
    this.errorMsg.set(null);

    // Single request — server pre-computes balances, no N+1 forkJoin needed
    this.customerService.getWithBalances().subscribe({
      next: (data) => { this.customers.set(data); this.isLoading.set(false); },
      error: () => { this.errorMsg.set('Could not load customers. Is the API running?'); this.isLoading.set(false); }
    });
  }

  openAddModal() {
    this.form = { name: '', phone: '', address: '' };
    this.formError.set(null);
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
  }

  save() {
    if (!this.form.name.trim() || !this.form.phone.trim()) {
      this.formError.set('Name and phone are required.');
      return;
    }
    this.isSaving.set(true);
    this.formError.set(null);
    this.customerService.create(this.form).subscribe({
      next: () => { this.isSaving.set(false); this.closeModal(); this.loadCustomers(); },
      error: () => { this.isSaving.set(false); this.formError.set('Failed to add customer.'); }
    });
  }
}