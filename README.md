# QA Automation Platform

Phase 2 is a working one-page QA scanner built on the Phase 1 foundation. It manages authorized targets, queues safe Playwright scans in the .NET backend, persists scan evidence in SQL Server, and presents scan history and details in React. It does not crawl links, click controls, submit forms, enter data, or run test scenarios.

## Required software

- .NET 10 SDK (`global.json` pins installed stable SDK `10.0.203`)
- Node.js 20 or newer and npm
- SQL Server 2019 or newer, SQL Server Express, or SQL Server LocalDB
- Entity Framework Core and `dotnet-ef` 10.0.9
- PowerShell 7 (`pwsh`) to run the official Playwright browser installer

## Configuration and SQL Server

The API uses standard ASP.NET Core configuration. Safe defaults use Windows LocalDB and allow the Vite origin. Never commit passwords.

```text
Server=(localdb)\mssqllocaldb;Database=QaAutomation;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Confirm LocalDB, then apply both checked-in migrations:

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
dotnet tool update --global dotnet-ef --version 10.0.9
dotnet ef database update --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api
```

For another SQL Server, copy `src/QaAutomation.Api/appsettings.Local.example.json` to the ignored `appsettings.Local.json`, replace its placeholder, set `$env:DOTNET_ENVIRONMENT = "Local"`, or configure `ConnectionStrings__QaAutomation` as an environment variable.

## Restore, build, test, and run

Run from the repository root:

```powershell
dotnet restore QaAutomation.sln
dotnet build QaAutomation.sln --no-restore
dotnet test QaAutomation.sln --no-build --no-restore
dotnet run --project src/QaAutomation.Api
```

The API starts at `http://localhost:5086`; health is `GET /api/health`.

In another terminal:

```powershell
cd frontend
npm install
npm run lint
npm run build
npm run dev
```

Open `http://localhost:5173`.

## Install Playwright Chromium

Build first, then install the Chromium revision matching `Microsoft.Playwright`:

```powershell
pwsh src/QaAutomation.Api/bin/Debug/net10.0/playwright.ps1 install chromium
```

Browser binaries go to Playwright's user cache and must not be committed. To use an ignored workspace-local cache, set the same variable for installation and API startup:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = "$PWD/app-data/playwright"
pwsh src/QaAutomation.Api/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet run --project src/QaAutomation.Api
```

## Start and review a scan

1. Create or enable an authorized target on **Targets**.
2. Open **Scans**, select it, and choose **Start safe scan**.
3. The API returns a queued scan ID immediately; status polling survives browser refresh and supports cancellation.
4. Open a history row for its thumbnail, full screenshot, element crops, preferred selector, candidates, and diagnostics.

This scanner is best-effort and scans only the starting page or its final in-host redirect. It does not click links, discover every route or SPA state, submit forms, bypass security controls, or execute scenarios.

## Scanner configuration

Safe defaults are under `Scanner` in `src/QaAutomation.Api/appsettings.json`:

- `OverallTimeoutSeconds`
- `NavigationTimeoutMilliseconds`
- `ActionTimeoutMilliseconds`
- `MaximumDetectedElements`
- `ScreenshotDirectory`
- `ElementScreenshotPadding`
- `Headless`
- `ViewportWidth` and `ViewportHeight`
- `MaximumDiagnosticRecords`

One scan executes at a time. Active scans left by an unexpected shutdown are marked failed on the next API startup.

## Artifacts and development cleanup

Artifacts live under ignored `app-data/scans/{scan-id}/`; SQL stores only managed relative paths. Artifact APIs accept database identifiers, never filesystem paths.

For a complete **development-only** reset, stop the API, then:

```powershell
dotnet ef database drop --force --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api
Remove-Item app-data/scans -Recurse -Force -ErrorAction SilentlyContinue
dotnet ef database update --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api
```

This removes targets and all scan history. Never run it against shared or production data.

## Structure and scope

- `src/QaAutomation.Core` — target/scan domain, safety rules, contracts, and service boundaries
- `src/QaAutomation.Infrastructure` — EF persistence, managed storage, Playwright scanner, and queue
- `src/QaAutomation.Api` — REST endpoints, background worker, health, logging, and Problem Details
- `tests/QaAutomation.Tests` — safety, state, queue, API, storage, and controlled real-browser tests
- `frontend` — dashboard, target management, scanner, history, and scan details
- `app-data` — ignored local artifacts and optional browser cache; never source code

Guided crawling, Phase 3 manual review tools, scenarios, test execution, AI, scheduling, reports, notifications, and verification integrations remain intentionally unimplemented.
