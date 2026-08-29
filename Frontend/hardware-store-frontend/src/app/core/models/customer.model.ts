export interface Customer {
  id: number;
  name: string;
  phone: string;
  address?: string;
}

export interface CreateCustomer {
  name: string;
  phone: string;
  address?: string;
}
export interface CustomerWithBalance extends Customer {
  totalOutstanding: number;
  billCount: number;
}