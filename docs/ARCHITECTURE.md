# Architecture Overview

## Stack

| Layer     | Technology                                              |
|-----------|-----------------------------------------------------------|
| Frontend  | Angular 22 (standalone components, signals)               |
| Backend   | ASP.NET Core 8 Web API                                     |
| ORM       | Entity Framework Core                                       |
| Database  | SQLite (file-based, stored outside the app folder)          |
| Auth      | JWT bearer tokens, role-based (`Admin` / `Staff`)            |
| Packaging | Self-contained `win-x64` publish + Inno Setup installer      |

## Backend layout (`Backend/HardwareStorePortal.API/`)

- `Controllers/` — API endpoints (Products, Categories, Bills, Customers, Payments, Auth, Reports)
- `Services/` + `I*Service.cs` — business logic, one interface + implementation per domain
- `Repositories/` — data access layer wrapping EF Core
- `DTOs/` — request/response shapes (never expose EF entities directly over the API)
- `Models/` — EF Core entities
- `Data/AppDbContext.cs` — EF Core context, including `OnModelCreating` decimal precision config
- `Program.cs` — app startup, DI registration, and a scoped block that runs
  `Database.Migrate()` plus first-run seeding (default accounts, category backfill)

## Frontend layout (`Frontend/hardware-store-frontend/src/app/`)

- `core/models/` — TypeScript interfaces mirroring backend DTOs
- `core/services/` — one Angular service per API resource (thin HTTP wrappers)
- `core/guards/`, `core/interceptors/` — route protection and JWT attachment
- `features/` — one folder per screen (dashboard, inventory, billing, bills, customers, login)
- `shared/components/shell/` — app shell (sidebar nav, layout)

## Database

- Single SQLite file, created and migrated automatically on first run
  (`db.Database.Migrate()` in `Program.cs`).
- Stored under `%ProgramData%\HardwareStorePortal\hardwarestore.db` — not inside the app's
  install folder — so reinstalls/updates never touch it and every Windows user account can
  write to it.
- Three accounts are seeded automatically if the `Users` table is empty: `admin`, `Muneeb`,
  `Shahid`.

## Key domain decisions

- **Categories** are a first-class entity (`Categories` table), not free text on `Product`.
  Only `Admin` role can create or delete a category; deleting a category that's still assigned
  to a product is blocked server-side.
- **Stock** auto-deducts immediately when a bill is created; manual stock adjustments (restock,
  damage, correction) go through a dedicated endpoint that also logs a `StockTransaction`.
- **Cost/purchase price** is edited through a separate `Admin`-only endpoint, kept out of the
  regular product edit form that `Staff` accounts can access.
- **Discounts** on bill line items are a flat Rs. amount, editable by any staff member (not
  admin-restricted).

## For anyone (human or AI) picking this project up

- Start the backend from `Backend/HardwareStorePortal.API` (`dotnet run`), the frontend from
  `Frontend/hardware-store-frontend` (`ng serve`). See the root `README.md` for exact commands.
- See `docs/BUILD.md` for how to produce the distributable Windows installer.
- The backend's Swagger UI (`/swagger`) is the fastest way to test API endpoints directly
  without going through the Angular UI.
