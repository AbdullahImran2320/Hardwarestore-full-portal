export interface BackupResult {
  path: string;
  sizeBytes: number;
  createdAt: string;
}

export interface BackupFile {
  name: string;
  sizeBytes: number;
  createdAt: string;
}

export interface BackupList {
  defaultFolder: string;
  files: BackupFile[];
}

export interface RestoreInfo {
  users: number;
  products: number;
  customers: number;
  bills: number;
  newestBillDate?: string | null;
  lastMigration?: string | null;
}
