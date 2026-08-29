export interface BillItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  lineTotal: number;
}
export interface Bill {
  id: number;
  customerId?: number;
  customerName?: string;
  billDate: string;
  totalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  status: 'Paid' | 'Partial' | 'Unpaid';
  paymentMethod: 'Cash' | 'Online';
  items: BillItem[];
}

export interface CreateBillItem {
  productId: number;
  quantity: number;
  discountAmount: number;
}

export interface CreateBill {
  customerId?: number | null;
  paidAmount: number;
  paymentMethod: 'Cash' | 'Online';
  items: CreateBillItem[];
}
export interface CartItem {
  productId: number;
  name: string;
  unit: string;
  price: number;
  availableStock: number;
  quantity: number;
  discount: number; // flat Rs. off this line
}