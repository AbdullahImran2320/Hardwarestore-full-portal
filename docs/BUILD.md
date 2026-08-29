# Building & Packaging Hardware Store Portal

This document explains how to go from source to a distributable Windows installer.

## Folder structure

```
Hardware-store-portal/
├── Backend/                        ASP.NET Core 8 Web API (EF Core + SQLite)
│   └── HardwareStorePortal.API/
├── Frontend/                       Angular 22 app
│   └── hardware-store-frontend/
├── setup/                          Build output (git-ignored, created automatically)
│   ├── Build/                      Published, combined app (frontend + backend)
│   └── Output/                     Final installer .exe
├── build.bat                       Step 1 — builds & combines frontend + backend
├── compile-installer.bat           Step 2 — packages the build into an installer
├── installer.iss                   Inno Setup script used by compile-installer.bat
└── LaunchHardwareStorePortal.ps1   Startup launcher bundled into the installed app
```

## Requirements (build machine only)

- [Node.js](https://nodejs.org/) (LTS)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isdl.php)

End users of the installed app do **not** need any of the above — the backend is published
self-contained for `win-x64`, so the .NET runtime is bundled in.

## Steps

1. Run `build.bat` from the repository root.
   - Installs frontend dependencies (`npm ci` / `npm install`)
   - Runs `npm run build` for the Angular app
   - Publishes the ASP.NET Core API as a self-contained `win-x64` app
   - Copies the Angular build output into the published API's `wwwroot`
   - Copies the launcher script alongside the published app
   - Output lands in `setup\Build\Backend\`

2. Run `compile-installer.bat` from the repository root.
   - Locates the published `.exe` from step 1
   - Compiles `installer.iss` with Inno Setup
   - Output: `setup\Output\HardwareStorePortal_Setup.exe`

Both scripts must be run from the repo root (they resolve all paths relative to their own location).

## How the launcher works

The installed shortcut runs `LaunchHardwareStorePortal.ps1`, which:

1. Starts `HardwareStorePortal.API.exe`
2. Polls `http://localhost:5000` until the server responds (up to 30s)
3. Opens the default browser to the app

This avoids opening the browser before ASP.NET Core has finished starting. The script is
launched via `powershell.exe -ExecutionPolicy Bypass -File ...`, which only affects that one
process — it does not change the user's system-wide PowerShell execution policy.

> **Note:** this launcher is intentionally kept as PowerShell rather than VBScript. It already
> handles retry/wait logic and error dialogs correctly and has been tested end-to-end; a VBScript
> rewrite would need to reproduce that logic from scratch for no functional benefit.

## Database safety

The installer excludes `*.db`, `*.db-shm`, and `*.db-wal` from packaging (see the `Excludes` line
in `installer.iss`). This means:

- A fresh install creates a new, empty database on first run (see `Program.cs` — it calls
  `Database.Migrate()` and seeds the default accounts automatically).
- Reinstalling over an existing install does **not** wipe an existing database, since the
  installer never touches `.db` files it finds in `{app}`.

## Default port

The launcher and the app currently assume `http://localhost:5000`. If the backend's configured
URL changes, update the `$url` variable in `LaunchHardwareStorePortal.ps1` to match.

## Before distributing

Always test the generated `HardwareStorePortal_Setup.exe` on a clean Windows machine (or VM)
that has never had the app or its dependencies installed before.
