import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CustomerService } from '../../core/services/customer.service';
import { BillService } from '../../core/services/bill.service';
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

  constructor(
    private customerService: CustomerService,
    private billService: BillService
  ) {}

  ngOnInit() {
    this.loadCustomers();
  }

  loadCustomers() {
    this.isLoading.set(true);
    this.errorMsg.set(null);

    this.customerService.getAll().subscribe({
      next: (customers) => {
        if (customers.length === 0) {
          this.customers.set([]);
          this.isLoading.set(false);
          return;
        }
        // fetch each customer's bills in parallel to compute outstanding totals
        const billCalls = customers.map(c => this.billService.getByCustomer(c.id));
        forkJoin(billCalls).subscribe({
          next: (billLists) => {
            const withBalance: CustomerWithBalance[] = customers.map((c, i) => ({
              ...c,
              totalOutstanding: billLists[i].reduce((sum, b) => sum + b.outstandingAmount, 0),
              billCount: billLists[i].length
            }));
            this.customers.set(withBalance);
            this.isLoading.set(false);
          },
          error: () => { this.errorMsg.set('Could not load customer balances.'); this.isLoading.set(false); }
        });
      },
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