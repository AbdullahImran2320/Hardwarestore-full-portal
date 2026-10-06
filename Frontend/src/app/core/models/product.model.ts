export interface Product {
  id: number;
  name: string;
  categoryId: number;
  category: string;
  unit: string;
  stockQty: number;
  salePrice: number;
  reorderLevel: number;
}

export interface CreateProduct {
  name: string;
  categoryId: number;
  unit: string;
  stockQty: number;
  purchasePrice: number;
  salePrice: number;
  reorderLevel: number;
}

export interface UpdateProduct {
  name: string;
  categoryId: number;
  unit: string;
  purchasePrice?: number;
  salePrice: number;
  reorderLevel: number;
}
export interface AdjustStock {
  quantityChange: number; // positive = add stock, negative = remove stock
  type: string; // Restock / Adjustment / Damage / Correction
  reference?: string;
}
export interface ProductCost {
  productId: number;
  productName: string;
  purchasePrice: number;
}

export interface UpdateProductCost {
  purchasePrice: number;
}