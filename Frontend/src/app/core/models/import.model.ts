export type ImportKind = 'products' | 'customers';
export type ExportKind = 'products' | 'customers' | 'bills';

export interface ImportRow {
  rowNumber: number;
  action: 'Create' | 'Update' | 'Error';
  messages: string[];
  name: string;
  category?: string | null;
  unit?: string | null;
  stock?: number | null;
  purchasePrice?: number | null;
  salePrice?: number | null;
  reorderLevel?: number | null;
  phone?: string | null;
  address?: string | null;
}

export interface ImportPreview {
  totalRows: number;
  toCreate: number;
  toUpdate: number;
  errorCount: number;
  newCategories: string[];
  rows: ImportRow[];
}

export interface ImportResult {
  created: number;
  updated: number;
  skipped: number;
  categoriesCreated: number;
}
