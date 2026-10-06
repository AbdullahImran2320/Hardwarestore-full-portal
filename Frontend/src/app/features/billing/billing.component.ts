import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { CustomerService } from '../../core/services/customer.service';
import { BillService } from '../../core/services/bill.service';
import { Product } from '../../core/models/product.model';
import { Customer } from '../../core/models/customer.model';
import { Bill, CreateBill, CartItem } from '../../core/models/bill.model';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './billing.component.html',
  styleUrl: './billing.component.scss'
})
export class BillingComponent implements OnInit {
  storeName = 'SM Traders';

  products = signal<Product[]>([]);
  customers = signal<Customer[]>([]);
  cart = signal<CartItem[]>([]);
  isLoadingProducts = signal(true);
  loadError = signal<string | null>(null);

  searchTerm = signal('');
  selectedCustomerId = signal<number | null>(null);
  paidAmount = signal<number>(0);
  paymentMethod = signal<'Cash' | 'Online'>('Cash');

  isSubmitting = signal(false);
  submitError = signal<string | null>(null);
  completedBill = signal<Bill | null>(null);

  filteredProducts = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.products().slice(0, 20); // show a reasonable default list
    return this.products().filter(p =>
      p.name.toLowerCase().includes(term) || p.category.toLowerCase().includes(term)
    );
  });

subtotal = computed(() =>
  this.cart().reduce((sum, item) => sum + (item.price * item.quantity - item.discount), 0)
);

totalDiscount = computed(() =>
  this.cart().reduce((sum, item) => sum + item.discount, 0)
);

  outstanding = computed(() => Math.max(this.subtotal() - this.paidAmount(), 0));

  status = computed<'Paid' | 'Partial' | 'Unpaid'>(() => {
    const paid = this.paidAmount();
    const total = this.subtotal();
    if (paid <= 0) return 'Unpaid';
    if (paid >= total) return 'Paid';
    return 'Partial';
  });

  constructor(
    private productService: ProductService,
    private customerService: CustomerService,
    private billService: BillService
  ) {}

  ngOnInit() {
    this.loadProducts();
    this.customerService.getAll().subscribe({
      next: (data) => this.customers.set(data),
      error: () => {} // customer list is non-critical — walk-in sale still works
    });
  }

  loadProducts() {
    this.isLoadingProducts.set(true);
    this.loadError.set(null);
    this.productService.getAll().subscribe({
      next: (data) => { this.products.set(data); this.isLoadingProducts.set(false); },
      error: () => { this.loadError.set('Could not load products. Is the API running?'); this.isLoadingProducts.set(false); }
    });
  }

addToCart(p: Product) {
  if (p.stockQty <= 0) return;

  const existing = this.cart().find(i => i.productId === p.id);
  if (existing) {
    if (existing.quantity >= p.stockQty) return;
    this.updateQuantity(existing, existing.quantity + 1);
  } else {
    this.cart.update(items => [...items, {
      productId: p.id,
      name: p.name,
      unit: p.unit,
      price: p.salePrice,
      availableStock: p.stockQty,
      quantity: 1,
      discount: 0
    }]);
    }
  }
  updateDiscount(item: CartItem, newDiscount: number) {
  const grossLineTotal = item.price * item.quantity;
  const clamped = Math.max(0, Math.min(newDiscount || 0, grossLineTotal));
  this.cart.update(items =>
    items.map(i => i.productId === item.productId ? { ...i, discount: clamped } : i)
  );
}
  

updateQuantity(item: CartItem, newQty: number) {
  const clamped = Math.max(1, Math.min(newQty, item.availableStock));
  this.cart.update(items =>
    items.map(i => {
      if (i.productId !== item.productId) return i;
      const newGross = i.price * clamped;
      const adjustedDiscount = Math.min(i.discount, newGross);
      return { ...i, quantity: clamped, discount: adjustedDiscount };
    })
  );
}

  removeFromCart(item: CartItem) {
    this.cart.update(items => items.filter(i => i.productId !== item.productId));
  }

  setPaidFull() {
    this.paidAmount.set(this.subtotal());
  }

  setPaidZero() {
    this.paidAmount.set(0);
  }

  clearBill() {
    this.cart.set([]);
    this.selectedCustomerId.set(null);
    this.paidAmount.set(0);
    this.paymentMethod.set('Cash');
    this.submitError.set(null);
  }

  submitBill() {
    if (this.cart().length === 0) {
      this.submitError.set('Add at least one item to the bill.');
      return;
    }
    if (this.paidAmount() < 0) {
      this.submitError.set('Paid amount cannot be negative.');
      return;
    }
    if (this.paidAmount() > this.subtotal()) {
      this.submitError.set('Paid amount cannot exceed the bill total.');
      return;
    }
    if (!this.selectedCustomerId() && this.paidAmount() < this.subtotal()) {
      this.submitError.set('Walk-in sales must be paid in full. Select a customer to allow credit/partial payment.');
      return;
    }

  const dto: CreateBill = {
  customerId: this.selectedCustomerId(),
  paidAmount: this.paidAmount(),
  paymentMethod: this.paymentMethod(),
  items: this.cart().map(i => ({ productId: i.productId, quantity: i.quantity, discountAmount: i.discount }))
};

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.billService.create(dto).subscribe({
      next: (bill) => {
        this.isSubmitting.set(false);
        this.completedBill.set(bill);
        this.clearBill();
        this.loadProducts(); // refresh stock levels after sale
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err?.error?.message || 'Failed to create bill. Please check stock levels and try again.';
        this.submitError.set(msg);
      }
    });
  }

  closeReceipt() {
    this.completedBill.set(null);
  }

  printReceipt() {
    window.print();
  }
}