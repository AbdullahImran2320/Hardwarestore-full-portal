export interface Payment {
  id: number;
  billId: number;
  amount: number;
  paymentDate: string;
  note?: string;
  paymentMethod: 'Cash' | 'Online';
}

export interface CreatePayment {
  billId: number;
  amount: number;
  note?: string;
  paymentMethod: 'Cash' | 'Online';
}