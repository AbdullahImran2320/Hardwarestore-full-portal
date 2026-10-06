# Hardware Store Portal

A full-stack management portal for a hardware store — inventory, billing, customer ledger,
and daily sales reporting — built with Angular and ASP.NET Core, packaged as a self-contained
Windows desktop installer.

## Features

- **Inventory management** — products with categories, units, stock levels, reorder alerts,
  and cost/sale pricing
- **Categories** — managed separately from products (admin-only add/delete) to prevent
  duplicate or misspelled category entries
- **Billing** — cart-based bill creation with per-item discounts, printable receipts, and
  automatic stock deduction
- **Customer ledger** — outstanding balances, payment history, partial payments
- **Manual stock adjustments** — restock, damage/loss, and manual corrections, each logged
  as a stock transaction
- **Role-based access** — `Admin` and `Staff` roles; cost/purchase price editing and category
  management are Admin-only
- **Daily reports** — sales totals, collected amounts, outstanding amounts, bill counts
- **Excel export** — products, customers and bills as `.xlsx` (purchase prices only for Admins)
- **Excel import** (Admin) — products and customers, with a template, a preview of every row and
  an all-or-nothing save
- **Backup & restore** (Admin) — download the database, copy it to a folder or USB drive, and restore
  from a backup file (a safety copy is taken first)
- **User management** (Admin) — add users, reset passwords, delete users
- **Change password** — every user can change their own password; accounts that still use a
  default or admin-set password must change it at next login
- **Light and dark theme** — toggle in the sidebar; the choice is remembered

## Tech stack

| Layer     | Technology                                    |
|-----------|------------------------------------------------|
| Frontend  | Angular 22 (standalone components, signals)     |
| Backend   | ASP.NET Core 8 Web API                          |
| Database  | SQLite (via Entity Framework Core)              |
| Auth      | JWT bearer tokens                               |
| Packaging | Self-contained `win-x64` publish + Inno Setup    |

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for a fuller breakdown of the codebase.

## Project structure

```
Hardware-store-portal/
├── Backend/            ASP.NET Core Web API
├── Frontend/            Angular app
├── docs/                 Architecture & build documentation
├── build.bat            Builds & combines frontend + backend
├── compile-installer.bat Packages the build into a Windows installer
├── installer.iss          Inno Setup script
└── LaunchHardwareStorePortal.ps1   Startup launcher used by the installed app
```

## Running locally (development)

**Backend**
```bash
cd Backend
dotnet run
```
Swagger UI is available at `/swagger` once running.

**Frontend**
```bash
cd Frontend
npm install
npm start
```
Then open `http://localhost:4200`.

The frontend's API URL is set in `src/environments/environment.ts` — make sure it points at
wherever the backend is actually running.

## Default accounts

On first run, the backend automatically creates its database and seeds three accounts:

| Username | Role  | Default Password |
|----------|-------|------------------|
| admin    | Admin | Admin123!        |
| Muneeb   | Admin | muneeb786        |
| Shahid   | Staff | Shahid123!       |

> These passwords are public (they are in this repository), so any account that still uses one
> is forced to choose a new password at its next login. Users created by an Admin must also
> choose their own password at first login. Users can change their password any time from the
> **My Account** page (click your name in the sidebar).

## Data, backups and security

- The database is stored outside the install folder, in
  `%ProgramData%\HardwareStorePortal\hardwarestore.db`, so upgrading or uninstalling the app never
  deletes your data.
- Backups made from the **Backup & Restore** page go to `%ProgramData%\HardwareStorePortal\Backups`
  by default (the newest 10 are kept), or to any folder you type, for example a USB drive.
- Each installation creates its own random login-signing key on first run
  (`%ProgramData%\HardwareStorePortal\jwt.key`). Nothing secret is needed in the repository.
- Only an Admin can create users. Every API endpoint except login requires a signed-in user.

## Building the Windows installer

See [`docs/BUILD.md`](docs/BUILD.md) for full details. Short version:

```bat
build.bat
compile-installer.bat
```

Requires Node.js, the .NET 8 SDK, and Inno Setup 6 on the build machine only — the installed
app is self-contained and needs no separate runtime.

## License

MIT — see [`LICENSE`](LICENSE).
