# QA Automation Platform

Phase 3 is a working scanner review toolset built on the Phase 1 foundation and Phase 2 safe one-page scanner. It manages authorized targets, runs safe Playwright scans in the .NET backend, persists scan evidence in SQL Server, and lets a tester search, filter, review, rename, classify, and inspect saved scan results.

Phase 3 still does not crawl links, click controls, submit forms, enter data, run scenarios, use AI, schedule runs, send notifications, or retrieve verification codes.

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

Confirm LocalDB and apply the checked-in migrations:

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
dotnet tool update --global dotnet-ef --version 10.0.9
dotnet ef database update --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api
```

The current migration sequence is:

1. `InitialCreate`
2. `AddSafeScannerFoundation`
3. `AddScannerReviewTools`

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
2. Open **Scans**, select the target, review the safe limits, and choose **Start safe scan**.
3. The API returns a queued scan ID immediately; status polling survives browser refresh and supports cancellation.
4. Open a history row to review overview, pages, elements, diagnostics, and the saved scan settings.

The scanner is best-effort and scans only the starting page or its final in-host redirect.

## Manual review behavior

Scanner-generated values are preserved. Manual review values are stored separately and may be cleared.

- Page display names use the manual page name when present, otherwise the generated page name.
- Element display names use the manual element name when present, otherwise the best scanner-generated label/text.
- Element classification uses the manual override when present, otherwise the scanner-generated classification.
- Clearing a manual value restores the scanner-generated fallback.

No reviewer identity fields are stored yet because authentication and permissions are not implemented.

## Selector review behavior

The UI shows every saved selector candidate for an element:

- selector type and value
- priority
- uniqueness during the scan
- confidence
- scanner-preferred marker
- manual-preferred marker
- effective preferred marker

A tester may select one existing selector candidate as the manual preferred selector or clear the manual selection. Selector text cannot be edited in Phase 3, and selecting a selector does not reopen or rescan the website. A selector that was unique during one scan is not guaranteed to remain stable forever.

## Search, filtering, and pagination

The scan history supports server-side filtering and pagination by target, status, and general text search. Element and diagnostic results are queried separately so large scans do not render all records at once.

Element filters include text search, effective classification, destructive-control status, manual review status, and manual selector status. Diagnostic filters include text search, category, severity, and HTTP status.

## Screenshot viewer

Page thumbnails, full-page screenshots, and element crops continue to load through identifier-based API endpoints. The frontend viewer supports fit-to-view, zoom in, zoom out, reset zoom, visible close control, Escape close, focus restoration, and responsive scrolling. Filesystem paths are never exposed.

## Per-scan limit configuration

Backend configuration remains the default source. Before starting a scan, the frontend asks the API for defaults and allowed ranges. The frontend validates edits, and the backend independently rejects unsafe values.

Supported Phase 3 overrides:

- overall timeout
- navigation timeout
- action timeout
- maximum detected elements
- maximum diagnostic records
- element screenshot padding
- viewport width
- viewport height

The effective settings are stored as a snapshot on each new scan. Existing Phase 2 scans remain readable even when those snapshot fields are empty.

## Fixed safety restrictions

These are displayed in the UI as safety information, not editable controls:

- one starting page only
- no link clicking
- no form submission
- no typing
- no uploads
- no downloads
- no CAPTCHA bypass
- no authentication bypass
- main-frame navigation restricted to the allowed host
- third-party resources may load only to render the approved page
- TLS certificate errors are not ignored

## Artifacts and development cleanup

Artifacts live under ignored `app-data/scans/{scan-id}/`; SQL stores only managed relative paths. Artifact APIs accept database identifiers, never filesystem paths.

For a complete development-only reset, stop the API, then:

```powershell
dotnet ef database drop --force --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api
Remove-Item app-data/scans -Recurse -Force -ErrorAction SilentlyContinue
dotnet ef database update --project src/QaAutomation.Infrastructure --startup-project src/QaAutomation.Api
```

This removes targets and all scan history. Never run it against shared or production data.

## Structure and scope

- `src/QaAutomation.Core` — target/scan domain, safety rules, contracts, review DTOs, and service boundaries
- `src/QaAutomation.Infrastructure` — EF persistence, managed storage, Playwright scanner, queue, and review/query services
- `src/QaAutomation.Api` — REST endpoints, background worker, health, logging, and Problem Details
- `tests/QaAutomation.Tests` — safety, state, queue, API, storage, scanner, and review-tool tests
- `frontend` — dashboard, target management, scanner, history, scan review tabs, filters, and screenshot viewer
- `app-data` — ignored local artifacts and optional browser cache; never source code

## Current known limitations

- Phase 3 still scans one page only.
- It does not crawl, click, submit forms, or execute scenarios.
- It does not support manual screenshot annotations.
- Selector review works only with already-saved selector candidates.
- No authentication, reviewer identity, or multi-user permissions exist yet.
- Reports, notifications, scheduler, AI, guided discovery, and verification integrations remain future phases.
