# Hardware Store Portal — API

ASP.NET Core 8 Web API backend for a hardware store management system.

## Features
- JWT authentication, passwords hashed with BCrypt
- Product, Customer, Bill, and Payment management
- EF Core with SQL Server, code-first migrations
- Swagger/OpenAPI docs in development

## Tech Stack
.NET 8, EF Core, SQL Server, JWT Bearer Auth

## Setup
1. Set your JWT key: `dotnet user-secrets set "Jwt:Key" "your-secret-key"`
2. Update the connection string in `appsettings.json` if needed
3. `dotnet ef database update`
4. `dotnet run`
5. Swagger UI available at `https://localhost:7282/swagger`