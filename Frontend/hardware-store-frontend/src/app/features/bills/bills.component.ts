import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BillService } from '../../core/services/bill.service';
import { PaymentService } from '../../core/services/payment.service';
import { Bill } from '../../core/models/bill.model';

type StatusFilter = 'All' | 'Paid' | 'Partial' | 'Unpaid';

@Component({
  selector: 'app-bills',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './bills.component.html',
  styleUrl: './bills.component.scss'
})
export class BillsComponent implements OnInit {
  bills = signal<Bill[]>([]);
  isLoading = signal(true);
  errorMsg = signal<string | null>(null);

  statusFilter = signal<StatusFilter>('All');
  searchTerm = signal('');

  detailBill = signal<Bill | null>(null);

  payModalBill = signal<Bill | null>(null);
  paymentAmount = signal<number>(0);
  paymentNote = signal('');
  paymentMethod = signal<'Cash' | 'Online'>('Cash');
  isPaying = signal(false);
  payError = signal<string | null>(null);

  isDeleting = signal(false);

  filteredBills = computed(() => {
    const status = this.statusFilter();
    const term = this.searchTerm().toLowerCase().trim();
    return this.bills()
      .filter(b => status === 'All' || b.status === status)
      .filter(b => !term ||
        b.id.toString().includes(term) ||
        (b.customerName || 'walk-in').toLowerCase().includes(term)
      )
      .sort((a, b) => new Date(b.billDate).getTime() - new Date(a.billDate).getTime());
  });

  totals = computed(() => {
    const list = this.filteredBills();
    return {
      count: list.length,
      totalAmount: list.reduce((sum, b) => sum + b.totalAmount, 0),
      totalOutstanding: list.reduce((sum, b) => sum + b.outstandingAmount, 0)
    };
  });

  constructor(
    private billService: BillService,
    private paymentService: PaymentService
  ) {}

  ngOnInit() {
    this.loadBills();
  }

  loadBills() {
    this.isLoading.set(true);
    this.errorMsg.set(null);
    this.billService.getAll().subscribe({
      next: (data) => { this.bills.set(data); this.isLoading.set(false); },
      error: () => { this.errorMsg.set('Could not load bills. Is the API running?'); this.isLoading.set(false); }
    });
  }

  viewDetail(bill: Bill) {
    this.detailBill.set(bill);
  }

  closeDetail() {
    this.detailBill.set(null);
  }

  openPayModal(bill: Bill, event?: Event) {
    event?.stopPropagation();
    this.payModalBill.set(bill);
    this.paymentAmount.set(bill.outstandingAmount);
    this.paymentNote.set('');
    this.paymentMethod.set('Cash');
    this.payError.set(null);
  }

  closePayModal() {
    this.payModalBill.set(null);
  }

  submitPayment() {
    const bill = this.payModalBill();
    if (!bill) return;

    if (this.paymentAmount() <= 0) {
      this.payError.set('Amount must be greater than zero.');
      return;
    }
    if (this.paymentAmount() > bill.outstandingAmount) {
      this.payError.set(`Cannot exceed outstanding amount of Rs. ${bill.outstandingAmount}.`);
      return;
    }

    this.isPaying.set(true);
    this.payError.set(null);

    this.paymentService.create({
      billId: bill.id,
      amount: this.paymentAmount(),
      note: this.paymentNote() || undefined,
      paymentMethod: this.paymentMethod()
    }).subscribe({
      next: () => { this.isPaying.set(false); this.closePayModal(); this.detailBill.set(null); this.loadBills(); },
      error: (err) => {
        this.isPaying.set(false);
        this.payError.set(err?.error?.message || 'Failed to record payment.');
      }
    });
  }

  deleteBill(bill: Bill, event?: Event) {
    event?.stopPropagation();
    if (!confirm(`Delete Bill #${bill.id}? This will restore the sold items back into stock and cannot be undone.`)) return;

    this.isDeleting.set(true);
    this.billService.delete(bill.id).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.detailBill.set(null);
        this.loadBills();
      },
      error: () => {
        this.isDeleting.set(false);
        alert('Failed to delete bill.');
      }
    });
  }
}