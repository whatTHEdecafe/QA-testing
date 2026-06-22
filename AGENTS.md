# Automated QA Testing and Monitoring Platform

## 1. Purpose of this file

This file contains the permanent project requirements and working rules for this repository.

Read this file completely before:

* Planning work
* Creating files
* Modifying code
* Debugging
* Refactoring
* Adding features
* Reviewing implementation
* Updating the database
* Changing the user interface

Also read `UI_STYLE_REFERENCE.md` before creating or changing any user-interface code.

Do not delete, rename, or substantially rewrite either instruction file unless the user explicitly requests it.

This repository may initially contain only:

* `AGENTS.md`
* `UI_STYLE_REFERENCE.md`

Create the remaining application structure gradually according to the phases described below.

The complete long-term system is described in this file, but do not attempt to implement every feature in one uncontrolled change. Implement one approved phase at a time, build it, test it, and report the result before moving to the next major phase.

---

# 2. Project objective

Build an internal web-based QA testing and monitoring platform for testing deployed web applications, especially customer booking flows and Vision Agent workflows.

An authorized tester should eventually be able to:

1. Save a customer or deployment URL.
2. Open that URL using browser automation.
3. Scan relevant pages and application states.
4. Detect visible elements and actionable controls.
5. Save page and element information.
6. Capture a reference thumbnail for each discovered page.
7. Capture a cropped screenshot for each useful element.
8. Review, rename, classify, and annotate discovered pages and elements.
9. Manually build reusable test scenarios.
10. Describe a test scenario using normal language.
11. Use AI to map that description to scanned pages and elements.
12. Generate a reviewable test-script draft.
13. Approve and save test scripts.
14. Run scripts manually.
15. Schedule each saved script independently.
16. Test scheduler behavior with an accelerated simulated calendar.
17. Store complete run history and diagnostic reports.
18. Classify failures such as element, network, console, server, mobile, or configuration failures.
19. Send notifications for important failures.
20. Preserve all important data after the application restarts.
21. Reset development data safely when intentionally requested.

The platform is intended to test authorized flows and deployments. It is not intended to perform unrestricted crawling, security bypassing, or destructive interaction with unrelated websites.

---

# 3. Core technology

Use the following technology direction unless there is a documented technical reason to change it.

## Frontend

* React
* Vite
* JavaScript or TypeScript
* Prefer TypeScript for new application code unless it significantly blocks the initial setup
* CSS Modules and shared theme CSS
* The visual direction from `UI_STYLE_REFERENCE.md`

## Backend

* ASP.NET Core Web API
* C#
* A supported current .NET version
* Dependency injection
* Async APIs
* Structured logging
* Configuration through standard .NET configuration
* Cancellation tokens for long-running operations

## Browser automation

* Microsoft Playwright for .NET
* Chromium first
* Additional browsers may be added later
* Browser automation belongs in the .NET backend or a .NET worker service, not inside the React frontend

## Persistence

* SQL Server
* Entity Framework Core
* Code-first migrations
* Structured relational storage for important searchable information
* Managed application-data storage for screenshot, trace, and report files

## Testing

* Backend automated tests
* Frontend tests where valuable
* Integration tests for important workflows
* Build and test verification after each implementation phase

Do not use Node.js as the application backend. Node.js and npm are permitted for building and running the React frontend.

---

# 4. General architecture

Use a maintainable structure without introducing unnecessary microservices.

A reasonable initial structure is:

* `frontend/`

  * React application
* `src/`

  * ASP.NET Core API
  * Core domain and application logic
  * Infrastructure and persistence logic
* `tests/`

  * Backend tests
* `app-data/`

  * Managed runtime files during local development
  * This directory must not contain source code
* Documentation and setup files at the repository root

A suggested .NET structure is:

* `src/QaAutomation.Api`
* `src/QaAutomation.Core`
* `src/QaAutomation.Infrastructure`
* `tests/QaAutomation.Tests`

Codex may refine this structure if there is a clear reason, but it must remain understandable and not become unnecessarily complicated.

The React frontend communicates with the ASP.NET Core backend through APIs.

Long-running browser operations should eventually run through a controlled job system so that:

* The HTTP request does not need to remain open indefinitely.
* Progress can be reported.
* Operations can be cancelled.
* Multiple jobs can be managed safely.
* Results remain available after the browser closes.

A simple in-process implementation is acceptable during the early prototype. Design boundaries so a separate worker can be introduced later without rewriting the complete application.

---

# 5. User-interface requirements

Read and follow `UI_STYLE_REFERENCE.md`.

The interface should use the reference for:

* Colors
* Typography
* Cards
* Panels
* Buttons
* Forms
* Tables
* Tabs
* Badges
* Alerts
* Status indicators
* Spacing
* Shadows
* Rounded corners
* Responsive behavior
* Accessibility
* Long-running operation loading animation

Do not blindly copy the layout of the project from which the style reference was created.

Choose the layout that best serves this QA application.

A sidebar may be appropriate because the completed platform will contain several major sections. A top navigation layout is also acceptable during the early foundation if it remains clear.

The application should eventually include areas such as:

* Dashboard
* Targets
* Scanner
* Scan Results
* Pages
* Elements
* Scenario Builder
* AI Test Builder
* Script Library
* Test Runner
* Scheduler
* Reports
* Notifications
* Settings

Do not create fake functionality. A section that is not implemented may either be omitted or clearly marked as not yet implemented.

Use the moving loader from `UI_STYLE_REFERENCE.md` for meaningful long-running actions, including scans and test runs. Do not use the blocking loader for nearly instant actions.

---

# 6. Important terminology

Use consistent domain terms.

## Target

A customer website, deployment, or application that the user is authorized to test.

## Scan

One execution that opens a target and examines one or more pages or application states.

## Page

A browser page, route, or meaningful single-page-application state.

A page may exist even when the URL does not change.

## Element

A detected visible or relevant item on a page.

## Actionable element

An item that a user can interact with, such as a button, input, link, dropdown, uploader, calendar, tab, or expandable control.

## Selector candidate

A possible Playwright locator for finding an element again.

## Scenario

An ordered definition of actions and assertions that represents a test.

## Test block

A reusable group of scenario steps, such as phone verification, photo upload, or quote review.

## Approved script

A reviewed scenario or generated script that is allowed to run.

## Test run

One execution of an approved script.

## Schedule

A saved instruction specifying when an approved script should run.

## Report

The saved summary and technical evidence from a scan or test run.

---

# 7. Target management

The platform should eventually allow the user to create and manage authorized targets.

A target should support information such as:

* Target ID
* Customer or deployment name
* Starting URL
* Allowed host or domain
* Environment

  * Development
  * Staging
  * Production
* Description or notes
* Enabled or disabled status
* Default browser
* Default desktop or mobile profile
* Creation date
* Updated date

Validate URLs carefully.

Do not allow browser automation to leave the configured allowed host by default.

Do not place credentials directly in ordinary target fields.

Sensitive authentication information must eventually use a secure configuration or secret-storage mechanism.

---

# 8. Scanning and page discovery

The scanner should use Playwright for .NET.

The scanner must be described honestly as best-effort discovery. It cannot guarantee that every page, hidden state, or possible action has been found.

## Initial scanning behavior

The first scanner version should:

* Open the authorized starting URL.
* Wait for the page to become usable.
* Record the final URL after redirects.
* Record the page title.
* Detect visible common actionable elements.
* Capture a page screenshot.
* Create a page thumbnail.
* Capture cropped screenshots for useful detected elements.
* Save scan, page, element, selector, and screenshot metadata.
* Capture important console errors.
* Capture page errors.
* Capture failed network requests.
* Report useful errors instead of only saying that the scan failed.

## Later page discovery

Later phases may support controlled discovery through:

* Normal links
* Approved navigation buttons
* Route changes
* Form steps
* Tabs
* Expandable sections
* Guided tester actions
* Previously saved scenario paths

The scanner must not automatically click every element without safeguards.

Avoid automatically activating:

* Delete
* Purchase
* Payment
* Final booking confirmation
* Account removal
* Message sending
* File deletion
* Data submission
* Irreversible administrative actions

Potentially destructive controls may be detected and documented without being clicked.

Provide configurable scanning limits such as:

* Maximum duration
* Maximum pages or states
* Maximum depth
* Navigation timeout
* Action timeout
* Allowed host
* Whether links may be followed
* Whether forms may be submitted
* Whether uploads may occur
* Whether approved test data may be entered

Default to safe behavior.

---

# 9. Element detection

Do not detect elements only by looking for `<button>` and `<a>` tags.

Use several signals to find useful controls:

* HTML element type
* Input type
* Visible text
* Accessible role
* Accessible name
* Associated label
* ARIA label
* Placeholder
* Name
* ID
* Test ID
* Title
* Tab index
* Content-editable state
* Click handlers or button-like behavior where detectable
* Parent form
* Nearby text
* Visible bounding box
* Enabled or disabled status

Detect common controls such as:

* Links
* Buttons
* Text inputs
* Phone inputs
* Email inputs
* Password inputs
* Number inputs
* Text areas
* Checkboxes
* Radio buttons
* Native selects
* Custom dropdowns
* File upload controls
* Date inputs
* Calendars
* Date pickers
* Time inputs
* Form submit controls
* Next and back controls
* Tabs
* Expandable sections
* Content-editable fields
* Custom elements with accessible button or input roles

Classify elements where possible as:

* Informational
* Navigational
* Input
* Action
* Submission
* Upload
* Date or time
* Potentially destructive
* Unknown custom control

Record uncertainty instead of pretending that every classification is correct.

Allow manual correction later.

---

# 10. Page identification

Do not impose a universal name such as “Checkout” or “Dashboard” on every target.

Generate the best display name from:

1. Page title
2. Main visible heading
3. Route or URL segment
4. Main section heading
5. Application state
6. Generated fallback name

Store the original information separately from the editable display name.

A page record should eventually support:

* Page ID
* Scan ID
* Original URL
* Final URL
* Route
* Original page title
* Main heading
* Generated display name
* User-edited display name
* Discovery order
* Previous or parent page
* Screenshot
* Thumbnail
* Creation date

---

# 11. Screenshots and thumbnails

## Page images

For each discovered page or meaningful state:

* Capture a full reference screenshot where practical.
* Create a smaller thumbnail for normal display.
* Show the page name beside its thumbnail.
* Keep the thumbnail small enough to avoid overcrowding the interface.
* Retain the full screenshot for detailed viewing.
* Record screenshot dimensions and metadata.

## Element images

For each useful actionable control:

* Scroll it into view.
* Capture a cropped screenshot focused on that control.
* Include a small amount of surrounding padding.
* Do not use the full page image as the element image.
* Associate it with its page and element records.
* If screenshot capture fails, record the failure without discarding the whole scan.

Element result cards should eventually display:

* Cropped image
* Element name
* Page name
* Element type
* Visible text or label
* Action classification
* Preferred selector
* Enabled or disabled state
* Notes

## File storage

Store image metadata and database relationships in SQL Server.

Store image files in a controlled application-data directory.

Use relative managed paths in the database.

Do not scatter screenshots inside source-code folders.

Do not store large screenshot binary data directly in ordinary relational rows unless there is a strong reason.

Design the file-storage interface so cloud storage could be added later.

---

# 12. Selector generation

Generate several selector candidates when possible.

Prefer Playwright locators in approximately this order:

1. Stable test ID
2. Accessible role and accessible name
3. Associated label
4. Placeholder
5. Stable unique ID
6. Stable name attribute
7. Unique visible text
8. Carefully generated CSS selector
9. XPath only as a last resort

Store:

* Selector type
* Selector value
* Priority
* Whether it was unique during the scan
* Confidence or reliability estimate
* Validation date
* Whether the user selected it manually

Do not assume a generated selector will remain stable forever.

Later phases may revalidate selectors or assist with repair.

---

# 13. Scan results interface

The scan-results interface should make discovered information understandable to a human tester.

It should eventually support:

* Scan status
* Start and completion time
* Target
* Starting and final URL
* Browser and viewport
* Number of pages
* Number of elements
* Warnings and errors
* Page thumbnail list
* Elements grouped by page
* Search
* Filtering by type
* Filtering by actionability
* Filtering potentially destructive controls
* Element cropped images
* Expandable technical details
* Console errors
* Network failures
* Page errors
* Screenshot viewer

Avoid showing all raw technical information at once.

Use cards, tabs, filters, and expandable sections.

---

# 14. Manual scenario builder

The manual scenario builder should eventually allow the tester to assemble a test from saved pages and elements.

Supported step concepts should include:

* Navigate to URL
* Click element
* Enter text
* Select option
* Check or uncheck control
* Upload file
* Choose date
* Wait for element
* Wait for navigation
* Wait for network response
* Verify text
* Verify URL
* Verify element exists
* Verify element is visible
* Verify element is enabled
* Verify response status
* Capture screenshot
* Retrieve approved verification code
* Enter verification code
* Save a value for a later step

Allow the tester to:

* Add steps
* Edit steps
* Remove steps
* Reorder steps
* Add expected results
* Add assertions
* Add notes
* Save drafts
* Duplicate scenarios
* Enable or disable scenarios
* Run approved scenarios

Test data should not be permanently embedded into generated code when it belongs in a reusable test-data profile.

---

# 15. Reusable test blocks

Allow modular blocks that can be reused in several complete scenarios.

Examples:

* Start booking
* Phone or email verification
* Image upload
* Vision analysis
* Detected-item review
* Address entry
* Date selection
* Quote review
* Booking confirmation
* Admin login
* Admin review

A reusable block may have several variations:

* Standard successful path
* Missing required value
* Invalid value
* Wrong verification code
* Expired verification code
* Resend verification
* Upload failure
* Large upload
* Slow response
* Desktop
* Mobile

Updating a reusable block should not require manually rewriting every scenario that references it.

Preserve scenario and block versions.

---

# 16. AI-assisted test generation

The AI feature is intended to assist the tester, not to operate production websites without review.

The AI should eventually use:

* User-written scenario
* Saved pages
* Saved elements
* Element metadata
* Page thumbnails
* Element cropped images where supported
* Selector candidates
* Navigation relationships
* Existing test blocks
* Test-data profiles

The expected flow is:

1. The user describes a test in normal language.
2. AI proposes an understandable ordered step list.
3. AI maps each step to saved pages and elements.
4. The interface shows the selected element for each step.
5. Uncertain or unresolved mappings are clearly marked.
6. AI generates a draft scenario or Playwright test.
7. The tester reviews and edits it.
8. The tester approves and saves it.
9. Only approved scripts become eligible for scheduling.

Do not silently run newly generated AI scripts.

Do not invent a selector when no matching scanned element exists.

Mark unresolved steps and allow manual correction.

Place the AI provider behind an interface.

The rest of the application must work without an AI API key.

The exact AI provider is deferred until explicitly selected.

---

# 17. Script library

The script library should eventually store approved test scripts and scenario definitions.

Each entry should support:

* Script ID
* Name
* Description
* Target
* Flow type
* Scenario
* Reusable blocks
* Version
* Enabled status
* Desktop or mobile profiles
* Test-data profile
* Verification provider requirement
* Creation date
* Updated date
* Last run
* Last result
* Associated schedules
* Notes

Allow scripts to be:

* Viewed
* Edited
* Duplicated
* Renamed
* Enabled
* Disabled
* Run immediately
* Scheduled
* Deleted with confirmation

Do not treat generated source code as the only source of truth. Preserve the structured scenario definition as well.

---

# 18. Test runner

The runner should eventually execute approved scenarios using Playwright for .NET.

Support:

* Chromium initially
* Headless mode
* Visible mode for debugging
* Desktop profiles
* Mobile emulation
* Viewport configuration
* Timeouts
* Cancellation
* Screenshots
* Console capture
* Page-error capture
* Failed-request capture
* HTTP status evidence
* Playwright traces where useful
* Step-by-step results
* Optional video in a later phase

For each step, save:

* Step number
* Step name
* Start time
* Finish time
* Status
* Current URL
* Referenced page
* Referenced element
* Action
* Expected result
* Actual result
* Error category
* Error message
* Screenshot
* Relevant network or console evidence

Do not reduce failures to only “Test failed.”

---

# 19. Scheduler

Each approved script should eventually have independent schedules.

Support concepts such as:

* Run once
* Hourly
* Daily
* Weekly
* Selected weekdays
* Monthly
* Selected day of month
* Selected date
* Starting date
* Repeat every selected number of days
* Repeat every selected number of weeks
* Start time
* Time zone
* Optional end date
* Enabled or paused state

Persist schedules in SQL Server.

Prevent unintended duplicate runs.

Record why a run started:

* Manual
* Real schedule
* Simulated schedule
* Retry
* Future API trigger

Do not choose a final scheduling library until the scheduler phase is being implemented and available options have been evaluated.

---

# 20. Accelerated simulated calendar

The application should eventually include a development-only scheduling simulator.

Examples:

* One simulated day equals one real second.
* One simulated day equals two real seconds.
* A month or year may be tested quickly.
* Simulation can begin from a selected date.

Requirements:

* Do not change the operating-system clock.
* Keep simulated time separate from real time.
* Clearly label simulated runs.
* Support start, pause, resume, stop, and reset.
* Allow speed selection.
* Display the current simulated date and time.
* Do not accidentally trigger real production notifications.
* Do not accidentally affect real customer data.
* Save simulated execution reports as simulated runs.

This feature belongs to a later scheduler phase.

Do not implement it during the initial foundation.

---

# 21. Reports

Reports should be organized primarily by script and run date.

The high-level view should show:

* Script name
* Target
* Flow type
* Latest status
* Latest run date
* Success or failure summary

Each run row should show:

* Date and time
* Status
* Duration
* Browser or device profile
* Trigger type

Possible statuses include:

* Success
* Failed
* Warning
* Cancelled
* Timed out
* Partially completed

Run rows should be expandable or open a detail page.

Detailed sections should eventually include:

* Summary
* Failed step
* Step results
* Screenshots
* Browser console
* Page errors
* Network failures
* HTTP errors
* Timing
* Desktop or mobile context
* Browser information
* Verification details
* Trace files
* Raw diagnostics

Classify likely causes such as:

* Application behavior
* Missing element
* Changed element
* Selector failure
* Navigation failure
* Network failure
* Console error
* Server error
* Timeout
* Authentication
* Verification
* Upload
* Desktop layout
* Mobile layout
* Test configuration
* Unknown

Do not claim certainty when the cause is only inferred.

---

# 22. Notifications

Design notifications behind an interface.

Possible future providers:

* Email
* Slack
* Microsoft Teams
* Webhook
* In-application notification

Failure notifications should eventually include:

* Target
* Script
* Run time
* Failed step
* Error category
* Error summary
* Device or browser
* Link or reference to the report

Avoid repeatedly sending identical alerts without a configurable repeat policy.

Do not implement a provider until requested.

---

# 23. Phone and email verification workflows

Some tested applications may require one-time verification codes.

Verification retrieval should use a provider abstraction.

Possible future implementations include:

* Dedicated QA phone number
* Approved SMS provider
* Twilio incoming-message integration
* Dedicated test email
* Gmail API
* Staging-only controlled verification provider

Requirements:

* Do not create an undocumented universal production bypass.
* Do not weaken production authentication.
* Do not hardcode codes or credentials.
* Keep secrets out of source control.
* Associate messages with the correct run and request time.
* Prevent concurrent test runs from consuming each other’s codes.
* Use correlation identifiers where supported.
* Apply expiration and timeout rules.
* Redact codes from ordinary reports.
* Do not retain verification codes longer than needed.
* Limit verification requests to avoid message flooding.

The exact provider is deferred.

---

# 24. Persistence

Persist important information so it remains available after restart.

This eventually includes:

* Targets
* Scans
* Pages
* Elements
* Selector candidates
* Screenshot metadata
* Manual names
* Manual classifications
* Scenarios
* Test blocks
* Script versions
* Schedules
* Scheduler history
* Test runs
* Step results
* Logs
* Error classifications
* Report files
* Settings

Use relational tables and appropriate indexes.

Flexible supplementary metadata may use JSON columns where appropriate, but do not put the entire application into one large JSON document.

Use Entity Framework Core migrations.

Do not require the user to manually create every database table.

---

# 25. Reset functionality

Provide a deliberate development reset later.

The reset may remove:

* Targets
* Scans
* Pages
* Elements
* Screenshots
* Scenarios
* Blocks
* Scripts
* Schedules
* Reports
* Run history
* Development settings

Requirements:

* Never run automatically.
* Require clear confirmation.
* Explain exactly what will be removed.
* Prefer typed confirmation for a complete reset.
* Stop active jobs safely.
* Remove managed files associated with deleted database records.
* Do not delete source code.
* Do not delete `AGENTS.md`.
* Do not delete `UI_STYLE_REFERENCE.md`.
* Do not delete external customer information.
* Log the reset.

---

# 26. Production safety

The application may eventually run against production URLs.

Use safe defaults.

Do not:

* Create real paid bookings during ordinary monitoring.
* Submit real payments.
* Delete customer data.
* Send uncontrolled messages.
* Repeatedly request verification codes.
* Bypass CAPTCHA.
* Bypass authentication.
* Bypass security controls.
* Crawl unrelated domains.
* Store unnecessary personal information.
* expose secrets in screenshots or reports.
* run destructive actions without explicit approval.

Support:

* Destructive-action classification
* Explicit approval
* Controlled test identities
* Controlled test data
* Secret redaction
* Phone and email redaction
* Cancellation
* Rate limits
* Concurrency protection
* Allowed-domain restrictions

Only test systems for which the user has authorization.

---

# 27. Error handling

Handle and report errors such as:

* Invalid URL
* DNS failure
* Connection refused
* TLS or certificate failure
* Navigation timeout
* Action timeout
* Browser installation missing
* Browser crash
* Element not found
* Element not visible
* Element disabled
* Selector not unique
* Screenshot failure
* File-storage failure
* SQL connection failure
* Migration failure
* Scheduler failure
* Verification timeout
* Upload failure
* AI provider unavailable
* Missing configuration
* User cancellation

Show understandable user-facing messages.

Preserve technical details in structured logs and reports.

Do not swallow exceptions silently.

---

# 28. Security and configuration

Never hardcode:

* Database passwords
* API keys
* Email credentials
* SMS credentials
* OAuth secrets
* Customer passwords
* Authentication tokens

Use:

* Environment variables
* .NET user secrets for local development
* Configuration files for safe non-secret defaults
* An example environment file without real secrets

Ensure local secret files and runtime data are excluded from source control.

Validate and sanitize user input.

Restrict file uploads by size and allowed type.

Do not allow arbitrary file-system paths from users.

---

# 29. Code-quality expectations

Use:

* Clear names
* Small focused classes
* Small focused methods
* Dependency injection
* Async browser, database, and file operations
* Cancellation tokens
* Interfaces around external services
* Structured logging
* Validation
* Database migrations
* Automated tests
* Consistent formatting
* Comments where they add real context

Avoid:

* One enormous controller
* One enormous service
* Static global state
* Hardcoded machine paths
* Hardcoded URLs
* Swallowed exceptions
* Fake completed features
* Unnecessary packages
* Premature microservices
* Copying business logic from the UI style source project
* Replacing working code without investigation

---

# 30. Development phases

Use this roadmap unless the user explicitly changes it.

## Phase 1: Application foundation

* Create .NET solution.
* Create ASP.NET Core API.
* Create React frontend.
* Apply `UI_STYLE_REFERENCE.md`.
* Configure local development.
* Configure SQL Server and Entity Framework Core.
* Create initial migration.
* Add logging and global error handling.
* Add health endpoint.
* Create target entity and target management.
* Create initial navigation and application shell.
* Add backend and frontend setup documentation.
* Add initial tests.

## Phase 2: First safe scanner

* Add Playwright for .NET.
* Open one authorized starting URL.
* Detect common visible actionable elements.
* Save scan, page, element, and selector data.
* Capture page screenshot and thumbnail.
* Capture cropped element screenshots.
* Show scan results in React.
* Capture basic console, page, and network errors.

## Phase 3: Scanner review tools

* Search and filter results.
* Manual page naming.
* Manual element naming.
* Manual classification.
* Screenshot viewer.
* Selector review.
* Scan diagnostics.
* Scan limits and advanced safety settings.

## Phase 4: Guided page discovery

* Controlled link and navigation discovery.
* Single-page application states.
* Approved actions.
* Navigation relationships.
* Duplicate-page detection.
* Progress and cancellation.

## Phase 5: Manual scenario builder

* Ordered steps.
* Assertions.
* Test data.
* Reusable test blocks.
* Scenario persistence.
* Draft and approved states.

## Phase 6: Playwright test runner

* Execute approved scenarios.
* Save step results.
* Screenshots and traces.
* Desktop and mobile profiles.
* Cancellation.
* Run history.

## Phase 7: AI-assisted test builder

* AI provider interface.
* Normal-language scenario input.
* Element mapping.
* Unresolved-step review.
* Draft generation.
* Human approval.

## Phase 8: Scheduler

* Independent script schedules.
* Real-time execution.
* Concurrency protection.
* History.
* Pause and resume.

## Phase 9: Simulated calendar

* Accelerated time.
* Isolated scheduler testing.
* Simulated reports.
* Production protections.

## Phase 10: Reports and notifications

* Script/date report organization.
* Detailed diagnostics.
* Failure classification.
* Notification providers.
* Duplicate alert policy.

## Phase 11: Verification integrations

* SMS or email provider interface.
* Dedicated test identity.
* Correlation and concurrency.
* Secure code retrieval.
* Redaction.

## Phase 12: Hardening

* Permissions
* Security review
* Performance
* Selector validation
* Rescan strategy
* Deployment architecture
* Backup and retention
* Production readiness

---

# 31. Deferred decisions

Do not decide or implement these until the related phase:

* Final AI provider
* Final SMS provider
* Final email provider
* Final scheduling library
* Final notification provider
* Cloud hosting
* Distributed workers
* Automatic rescanning
* Automatic selector repair
* Multi-user permissions
* Final report retention policy
* Production payment testing

Create clean interfaces where useful, but do not fill the project with unused speculative implementations.

---

# 32. Codex working procedure

Before starting any task:

1. Read this file completely.
2. Read `UI_STYLE_REFERENCE.md` when the task affects the frontend.
3. Inspect the current repository.
4. Identify the current implementation phase.
5. Check whether requested behavior already exists.
6. State important assumptions.
7. Keep the work within the requested scope.

During implementation:

1. Prefer one working vertical slice over many empty classes.
2. Do not implement future phases without being asked.
3. Preserve existing working behavior.
4. Use migrations for schema changes.
5. Keep secrets out of source control.
6. Add or update tests for meaningful logic.
7. Keep documentation accurate.

Before finishing:

1. Restore dependencies.
2. Build backend.
3. Run backend tests.
4. Build frontend.
5. Run applicable frontend tests or linting.
6. Verify database migration creation when relevant.
7. Report failures honestly.
8. Do not claim something was tested when it was not.

At the end of every task, report:

* Phase worked on
* Files created
* Files changed
* Database changes
* Features implemented
* Commands executed
* Build results
* Test results
* Known limitations
* Recommended next step

---

# 33. Current starting state

The repository currently contains only the project instructions and UI style reference.

The first Codex task should implement Phase 1 only.

Do not implement the scanner, AI generator, scheduler, simulated calendar, reports, alerts, or verification integrations during the initial foundation task unless the user explicitly changes the scope.

The Phase 1 result must be a real working foundation, not a collection of disconnected placeholders.
