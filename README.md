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
cd Backend/HardwareStorePortal.API
dotnet run
```
Swagger UI is available at `/swagger` once running.

**Frontend**
```bash
cd Frontend/hardware-store-frontend
npm install
ng serve
```
Then open `http://localhost:4200`.

The frontend's API URL is set in `src/environments/environment.ts` — make sure it points at
wherever the backend is actually running.

## Default accounts

On first run, the backend automatically creates its database and seeds three accounts:

| Username | Role  |
|----------|-------|
| admin    | Admin |
| Muneeb   | Staff |
| Shahid   | Staff |

> These are demo/internal seed accounts for a locally-run installer. Change the passwords
> (directly in the database, or via a future "change password" feature) before deploying to
> a shared or production environment.

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
