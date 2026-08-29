import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CustomerService } from '../../../core/services/customer.service';
import { BillService } from '../../../core/services/bill.service';
import { PaymentService } from '../../../core/services/payment.service';
import { Customer } from '../../../core/models/customer.model';
import { Bill } from '../../../core/models/bill.model';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './customer-detail.component.html',
  styleUrl: './customer-detail.component.scss'
})
export class CustomerDetailComponent implements OnInit {
  customer = signal<Customer | null>(null);
  bills = signal<Bill[]>([]);
  isLoading = signal(true);
  errorMsg = signal<string | null>(null);

  payModalBill = signal<Bill | null>(null);
  paymentAmount = signal<number>(0);
  paymentNote = signal('');
  paymentMethod = signal<'Cash' | 'Online'>('Cash');
  isPaying = signal(false);
  payError = signal<string | null>(null);

  totalOutstanding = computed(() =>
    this.bills().reduce((sum, b) => sum + b.outstandingAmount, 0)
  );
  totalPurchased = computed(() =>
    this.bills().reduce((sum, b) => sum + b.totalAmount, 0)
  );

  private customerId!: number;

  constructor(
    private route: ActivatedRoute,
    private customerService: CustomerService,
    private billService: BillService,
    private paymentService: PaymentService
  ) {}

  ngOnInit() {
    this.customerId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  load() {
    this.isLoading.set(true);
    this.errorMsg.set(null);

    this.customerService.getById(this.customerId).subscribe({
      next: (c) => this.customer.set(c),
      error: () => this.errorMsg.set('Customer not found.')
    });

    this.billService.getByCustomer(this.customerId).subscribe({
      next: (bills) => { this.bills.set(bills); this.isLoading.set(false); },
      error: () => { this.errorMsg.set('Could not load bills for this customer.'); this.isLoading.set(false); }
    });
  }

  openPayModal(bill: Bill) {
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
      next: () => { this.isPaying.set(false); this.closePayModal(); this.load(); },
      error: (err) => {
        this.isPaying.set(false);
        this.payError.set(err?.error?.message || 'Failed to record payment.');
      }
    });
  }
}