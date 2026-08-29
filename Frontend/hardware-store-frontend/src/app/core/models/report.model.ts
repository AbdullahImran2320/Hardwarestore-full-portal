export interface TopProduct {
  productName: string;
  quantitySold: number;
  revenue: number;
}

export interface DailySalesReport {
  date: string;
  totalSales: number;
  totalPaid: number;
  totalOutstanding: number;
  billCount: number;
  topProducts: TopProduct[];
}