import { Routes } from '@angular/router';
import { ShellComponent } from './shared/components/shell/shell.component';
import { LoginComponent } from './features/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { InventoryComponent } from './features/inventory/inventory.component';
import { BillingComponent } from './features/billing/billing.component';
import { BillsComponent } from './features/bills/bills.component';
import { CustomersComponent } from './features/customers/customers.component';
import { CustomerDetailComponent } from './features/customers/customer-detail/customer-detail.component';
import { AccountComponent } from './features/account/account.component';
import { UsersComponent } from './features/users/users.component';
import { BackupComponent } from './features/backup/backup.component';
import { authGuard, adminGuard, mustChangePasswordGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    canActivateChild: [mustChangePasswordGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'inventory', component: InventoryComponent },
      { path: 'billing', component: BillingComponent },
      { path: 'bills', component: BillsComponent },
      { path: 'customers', component: CustomersComponent },
      { path: 'customers/:id', component: CustomerDetailComponent },
      { path: 'account', component: AccountComponent },
      { path: 'users', component: UsersComponent, canActivate: [adminGuard] },
      { path: 'backup', component: BackupComponent, canActivate: [adminGuard] },
    ]
  }
];
