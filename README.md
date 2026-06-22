# QA Automation Platform

Phase 1 is a working application foundation for managing authorized QA targets. It includes an ASP.NET Core API, React/Vite UI, SQL Server persistence, EF Core migration, health check, structured logs, consistent errors, and backend tests. Browser scanning and later roadmap features are intentionally absent.

## Required software

- .NET 10 SDK (the repository pins the installed stable SDK `10.0.203` in `global.json`)
- Node.js 20 or newer and npm
- SQL Server 2019 or newer, SQL Server Express, or SQL Server LocalDB
- Entity Framework Core 10.0.9 packages and matching `dotnet-ef` 10 tooling

## Configuration

The API reads standard ASP.NET Core configuration. Safe defaults use Windows LocalDB and allow the Vite development origin. Do not commit passwords.

For a normal local override, copy `src/QaAutomation.Api/appsettings.Local.example.json` to `src/QaAutomation.Api/appsettings.Local.json`, replace the placeholder connection string, and start the API with:

```powershell
$env:DOTNET_ENVIRONMENT = "Local"
dotnet run --project src/QaAutomation.Api
```

Alternatively, set the connection string without writing it to a file:

```powershell
$env:ConnectionStrings__QaAutomation = "Server=localhost;Database=QaAutomation;User Id=qa_app;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True"
```

For the frontend, copy `frontend/.env.example` to `frontend/.env.local` only when the API base path differs. Vite proxies `/api` to `http://localhost:5086` during development.

## SQL Server setup

The checked-in default is suitable for SQL Server LocalDB on Windows:

```text
Server=(localdb)\mssqllocaldb;Database=QaAutomation;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Confirm that the default LocalDB instance exists and is running before applying migrations:

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

If the instance already exists, the create command reports that fact and can be skipped.

For full SQL Server, create an empty `QaAutomation` database (or grant the application login permission to create it), give the application login database-owner permissions for local development or the narrower schema/data permissions required by your environment, then configure `ConnectionStrings__QaAutomation` as shown above. Never place a real password in a committed settings file.

## Restore and database migrations

Run commands from the repository root:

```powershell
dotnet restore QaAutomation.sln
dotnet tool install --global dotnet-ef --version 10.0.9
dotnet ef database update --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api
```

If `dotnet-ef` is already installed, update it with:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.9
dotnet ef --version
```

To create a future migration after changing the EF model:

```powershell
dotnet ef migrations add DescriptiveMigrationName --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api --output-dir Persistence/Migrations
```

## Backend build, test, and run

```powershell
dotnet build QaAutomation.sln --no-restore
dotnet test QaAutomation.sln --no-build
dotnet run --project src/QaAutomation.Api
```

The API starts at `http://localhost:5086`. Its health endpoint is `GET http://localhost:5086/api/health`.

## Frontend install, validate, build, and run

```powershell
cd frontend
npm install
npm run lint
npm run build
npm run dev
```

Open `http://localhost:5173`. Keep the API running in a separate terminal.

## Application structure

- `src/QaAutomation.Core` — target domain, contracts, validation, and service boundary
- `src/QaAutomation.Infrastructure` — EF Core SQL Server context, migrations, and target persistence
- `src/QaAutomation.Api` — HTTP endpoints, health checks, configuration, structured logs, and error handling
- `tests/QaAutomation.Tests` — validation and persistence behavior tests
- `frontend` — React/TypeScript application shell, dashboard, and target management
- `app-data` — ignored managed runtime files for later phases; never source code

## Current scope

Targets can be listed, read, created, updated, enabled/disabled, and deleted. Dashboard counts come from the database. The platform does not yet scan websites, run tests, schedule work, generate tests with AI, or send notifications.
