# Call Rating System — ASP.NET Core MVC

The active call-rating workflow is migrated to .NET 10. Open **CRS.slnx** in a .NET 10-compatible IDE, such as Visual Studio 2026, or use the CLI. The old Framework 4.8 applications remain untouched as references.

- **CRS.Core**: models, call/help-desk managers, SQL Server repository, calls API client, email notifications, Excel generation, and development demo adapters.
- **CRS.Web**: controllers, Razor views, authentication/authorization, application composition and Serilog. Reuses the original HW logo, theme CSS, colours, rounded buttons and floating call/rating cards with responsive MVC layout adjustments.
- **CRS.Tests**: xUnit + FakeItEasy unit tests and authenticated HTTP integration tests.
- **tests/browser**: repeatable Playwright browser walkthrough.

## Run locally without credentials

Install the .NET 10 SDK, then from this directory:

```sh
dotnet restore CRS.slnx --locked-mode
dotnet build CRS.slnx --no-restore
dotnet test CRS.slnx --no-build
dotnet run --project src/CRS.Web --launch-profile http
```

The `http` launch profile enables Development and uses port 5174. Open the app in your local browser, choose **Sign in**, then a demo operator, operator with recordings (PSP), or administrator. Development enables demo mode by default. Alternatively:

```sh
dotnet run --project src/CRS.Web --no-launch-profile --environment Development --urls http://127.0.0.1:5174
```

Demo mode is labelled on every screen, uses synthetic calls/ratings, simulates email, and provides a generated WAV rather than a real recording. Changes are in memory and reset when the server restarts. There is no development sign-in route available in live mode. Startup rejects demo mode outside Development.

## Walk through the migrated features

1. Sign in as demo operator. Select a date, refresh calls, and report a call problem.
2. Choose problems and a pharmacy. “Other” requires details. Invalid choices are rejected server-side. Saving persists the rating before notifying help desk; notification failure explicitly reports that the save succeeded. Repeated submissions do not insert duplicates.
3. Sign out and sign in as the recording-enabled operator to download a synthetic recording.
4. Sign in as administrator. Filter by status, page through ratings, review a rating, set its status/reason and add IT notes. Closing a live rating sends feedback to its user when their email exists, as well as a help-desk notification.
5. Export the selected status filter to Excel. The export writes caller-supplied text as text, not spreadsheet formulas.

The old paging procedure subtracts IDs and can skip filtered rows or gaps. The new manager pages the actual filtered, consistently ordered result in groups of five. It currently materializes `CSP_GetCallRating` for paging and export, matching the old full-result count approach; large deployments can replace this with true SQL OFFSET/count queries.

## Endpoints and permissions

| Route | Behaviour | Access |
| --- | --- | --- |
| `GET /Calls?date=yyyy-MM-dd` | Call list for the signed-in user's directory extension | Signed in |
| `GET/POST /Calls/Rate` | Show/save call rating | Signed in; server rechecks call ownership |
| `GET /Calls/Recording?callId=…&date=…` | Resolve recording through API | PSP, IT, Admin; own extension |
| `GET /Admin?page=1&statusId=…` | Filtered five-row help-desk pages | IT, Admin |
| `GET/POST /Admin/Edit` | Review/update persisted rating | IT, Admin |
| `GET /Admin/Recording?id=…` | Resolve stored rating recording | IT, Admin |
| `GET /Admin/Export?statusId=…` | Download XLSX report | IT, Admin |
| `GET /Account/Login` | Microsoft sign-in, or demo account selection | Public |
| `POST /Account/Logout` | Sign out | Signed in |

POST forms use antiforgery protection. Permissions are enforced by server policies, not only hidden buttons. Roles are exact `IT`, `Admin`, `PSP` claims. The operator extension, call details, and update author are resolved on the server rather than accepted from hidden browser fields.

Outgoing API requests preserve the legacy contracts:

- `POST api/3cx/call-logs/by-extension`: JSON `{ extension, callType: "Both", date }`.
- `GET api/3cx/call-logs/call-url/{escapedFilename}`: expects `{ "url": "https://…" }`.

The configured API client uses per-request bearer authentication, finite timeouts, cancellation, safe user-facing errors, and no automatic redirects. Recording redirects require HTTPS and an explicit allowed hostname. No private Framework-only HW package is required by the new projects.

## Connect live services when you are ready

API keys and live identity/service configuration are left for the owner to supply. Set `Demo__Enabled=false`. Use environment variables, your deployment secret store, or .NET user secrets (`dotnet user-secrets --project src/CRS.Web …`). Never put secret values in tracked JSON. Configuration names:

| Setting | Purpose |
| --- | --- |
| `Database__CallRating` | SQL Server CallRatingSystem database connection string |
| `Database__Users` | SQL Server ADUsers database connection string |
| `CallApi__BaseUrl` | HTTPS base URL of calls API |
| `CallApi__AccessToken` | Bearer token, supplied securely |
| `CallApi__RecordingHosts__0` (and subsequent indices) | Allowed recording download hostnames, without URL schemes/paths |
| `CallApi__TimeoutSeconds` | Timeout, clamped to 1–120 seconds; default 30 |
| `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__ClientSecret` | Microsoft OIDC application configuration; callback `/signin-oidc` |
| `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__HelpDesk` | Mail server and recipients; TLS required |
| `Smtp__Username`, `Smtp__Password` | SMTP credentials when required |
| `Logging__Directory` | Absolute log folder override |

`IAccessTokenProvider` is the replacement point for your existing token acquisition/renewal service. The included provider reads a securely configured bearer token; it does not reproduce the unavailable private HW central-config token protocol. You can wire that provider separately. Live Microsoft sign-in validates the configured tenant issuer; configure app roles and your deployment callback/logout addresses in Entra.

The supplied SQL is retained in `database/CallRatingSystemDemo.sql` for review/import into an appropriate development database. It is not executed by app startup. It requires the `ADUsers.dbo.ADUser` directory table and existing active lookup data, neither of which was included in the supplied script. New ratings use status 1 = Open; closure uses status 2 = Closed. The new repository uses the supplied lookup/list/add/update procedures and parameterized user/rating lookups. Duplicate check + insert and update existence checks use SQL transactions and locking. The old ID-arithmetic paging procedure is not used. Review database permissions and test against a development SQL Server before production use.

Live SQL, calls API, SMTP and Microsoft sign-in were not contacted during development; adapters were exercised with controlled test dependencies. The owner will complete those connection checks. The unused, commented-out task/patient controls are not part of the active call-rating screens and are not migrated.

## Logging

Serilog writes application lifecycle, operation outcomes, request completion/failures and dependency errors to console and daily rolling files. Windows defaults to `C:\CallRatingSystem\Logs\crs-YYYYMMDD.log`; Linux defaults to `Logs` beneath the Web content root. The application identity needs write permission.

Retention is seven days, applied when a file opens/rolls (startup, daily roll, or the 100 MB size limit). No cleanup process runs while the app is stopped. Request events omit raw paths, queries, headers and bodies; a final enricher also removes request-path properties introduced by ASP.NET logging scopes. Dependency logs record error types/status codes, not tokens, connection strings or caller data. Do not add sensitive values to future log messages or exception text. Logs/build artifacts are ignored by Git. Existing legacy Web.config credentials were not copied into the new projects; previously exposed credentials/history remain a separate cleanup task.

## Repeat the browser checks

Start a fresh demo server first (the walkthrough expects initial demo state). With Node 22+:

```sh
npm ci --prefix tests/browser
cd tests/browser
npx playwright install chromium
cd ../..
npm test --prefix tests/browser
```

`CRS_BASE_URL` overrides the default local port 5174; `CHROMIUM_PATH` selects an existing browser executable. Linux may need `npx playwright install --with-deps chromium`. The script exercises operator sign-in, rating validation/save, duplicate protection, help-desk paging/filtering/updates, XLSX and recording downloads, mobile navigation and JavaScript errors. Do not run it against a live service: it requires the visible demo banner.

`.github/workflows/dotnet.yml` restores locked packages, builds/tests Release, starts a fresh demo server and runs the browser walkthrough. CI publication is not part of this change. Tests cover the migration's known success/failure branches, not a guarantee against every possible production failure; see `docs/testing.md` for the test map.
