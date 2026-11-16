# Call Rating System — ASP.NET Core MVC

Open the root **CRS.slnx** in a .NET 10-compatible IDE. It contains:

```text
CallRatingSystem/             Existing Framework 4.8 project (default build excluded)
src/CRS.Web/                  MVC views, workflow/error controllers, middleware, Program.cs
src/CRS.Core/                 Models/DTO, Models/ViewModels, managers, DataAccess entities and EF Core contexts
tests/CRS.Tests/             Unit/integration tests and all fake/demo implementations
```

## Application structure

- **CallListController**: normal-user call cards, rating popup, save and recording download.
- **AdminController**: help-desk/IT review, filters, paging, updates, recordings and Excel export.
- **CallApiDataAccess**: one ordinary class receiving an `HttpClient`. Calls the existing call-list and recording endpoints. `Program.cs` sets its base URL. No API auth/token provider/options hierarchy.
- **CallRatingContext**: EF Core context mapped to the existing ratings and lookup tables.
- **ADUsersContext**: EF Core mapping for `ADUsers.dbo.ADUser`, which lives in the legacy system's separate directory database. Configure the Users connection to that database.
- **CallRatingDataAccess**: EF LINQ queries and `SaveChangesAsync` for reads/writes. No raw SQL executor or stored-procedure repository. Managers still validate and coordinate saves and email notifications.
- **ControllerLoggingMiddleware**: runs after routing and logs only matched MVC controller actions, including outcome, elapsed time and trace reference. Static assets and unmatched routes are ignored. Functions use Serilog directly for their operation/error events.

The old `CRS/CRS/CallRatingSystem` duplicate and nested solution files are removed. The single legacy project is in the same root solution but requires Windows/Visual Studio WebApplication tooling to build; enable its Build checkbox in Configuration Manager when those dependencies are available.

## Configure and run the real application

Install .NET 10, then:

```sh
dotnet restore CRS.slnx --locked-mode
dotnet build CRS.slnx --no-restore
dotnet test CRS.slnx --no-build
dotnet run --project src/CRS.Web --launch-profile http
```

Set connection values externally using environment variables, user secrets (`dotnet user-secrets --project src/CRS.Web …`) or deployment configuration:

| Setting | Purpose |
| --- | --- |
| `CallApi__BaseUrl` | Base URL, configured on `AddHttpClient<CallApiDataAccess>` in Program.cs |
| `ConnectionStrings__CallRating` | Existing CallRatingSystem SQL Server database |
| `ConnectionStrings__Users` | Existing ADUsers SQL Server database |
| `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__HelpDesk` | Email delivery settings; default port 587 with TLS |
| `Smtp__Username`, `Smtp__Password` | Supply both for authenticated SMTP; leave both empty only for an unauthenticated relay |
| `Smtp__EnableSsl`, `Smtp__TimeoutSeconds` | TLS enabled by default; delivery timeout defaults to 300 seconds |
| `Smtp__DebugEmail`, `Smtp__DebugEmailAddress` | `0` = live; `1` = redirect all mail to the debug address; `2` = no email |
| `Logging__Directory` | Absolute log folder override |

Entra ID sign-in is configured entirely in `Program.cs` with Microsoft.Identity.Web and the `AzureAd` configuration section. Replace the `#{AzureAd.ClientId}#`, `#{AzureAd.TenantId}#`, and `#{AzureAd.ClientSecret}#` placeholders through deployment substitution or external configuration (`AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__ClientSecret`). Do not commit a real client secret. Register the application's HTTPS origin plus `/signin-oidc` as a Web redirect URI in Entra. The supplied domain is `healthwindow.co.za`.

Both controllers require an authenticated user with a nonempty directory GUID in the Entra `oid`/objectidentifier claim (or `ClaimTypes.NameIdentifier` for other identities). Anonymous requests challenge Entra sign-in; invalid directory identities receive 403. Controllers use the scoped `IUserSession`/`UserSession` service for the current user's directory ID, display name, and email. The object ID is an identifier, not an access token. Call List is available to authenticated application users. Every Admin action requires the exact Entra application role `CallRatingSystemAdmin` in the `roles` claim. Assign that app role to the appropriate users or groups in Entra; group membership alone must result in the app role being issued. Users without that role see the Access Denied page with HTTP 403, and the Help desk navigation link is hidden. Tests use a separate test authentication handler and do not contact Entra.

The production app always uses the actual data-access/EF/email registrations. It does not fall back to fake data if configuration is missing. The supplied `database/CallRatingSystemDemo.sql` documents the existing tables; no schema creation, migration or seed runs automatically. Apply/manage your development schema and its ADUsers/lookup data separately. EF maps nullable legacy columns and preserves the text CallDate/CallTime fields when saving. New ratings use status 1 = Open; closure uses 2 = Closed.

The SMTP credential fields are explicitly present but empty in `appsettings.json`; supply real credentials externally. The default help-desk destination is the legacy `hwvoip@rootsystem.co.za` address. `From` may be configured separately; if omitted, the authenticated SMTP username is used as the sender, matching the old application. Development overrides `Smtp:DebugEmail` to `2`, so email is disabled unless explicitly overridden. For debug mail use `Smtp__DebugEmail=1` and configure `Smtp__DebugEmailAddress`; both help-desk and user-feedback emails are redirected, with a `[DEBUG]` subject prefix. Missing debug configuration never falls back to live recipients. Saves report disabled/debug delivery separately from live notification success. Legacy mode 2 attempted to send without recipients; the new implementation skips delivery entirely.

The legacy migration comparison and remaining gaps are recorded in [docs/legacy-parity.md](docs/legacy-parity.md).

## Existing screen interaction

Open `/CallList` (also the default `/`). A card shows call information and its available download button. Selecting the card opens the help-desk popup for problems, pharmacy and other details. The download button does not select the card. Cancel closes the popup without saving. Server validation stays in the popup with entered values preserved. A successful submission saves the database row first, then sends email; delivery failure reports that the save succeeded. Duplicate submissions are checked in a serializable EF transaction.

`/Admin` shows rating cards, status filters and five-row paging. Updates persist status, reason, note and audit information and notify help desk; closing also emails the rating owner when an address exists. Excel export uses literal text cells for user-entered content. The original HW logo, CSS theme, colours and floating cards are reused locally.

| Route | Action |
| --- | --- |
| `GET /CallList?date=yyyy-MM-dd` | Calls for the current user's directory extension |
| `GET/POST /CallList/Rate` | Load/submit popup; full-page fallback without JavaScript |
| `GET /CallList/Recording?callId=…&date=…` | Resolve the selected call's recording |
| `GET /Admin?page=1&statusId=…` | Help-desk list/filter/page |
| `GET/POST /Admin/Edit` | Review/update a rating |
| `GET /Admin/Recording?id=…` | Resolve a saved rating's recording |
| `GET /Admin/Export?statusId=…` | Excel report |

The API data-access class uses the original endpoint contracts:

- `POST api/3cx/call-logs/by-extension` with JSON `{ extension, callType: "Both", date }`.
- `GET api/3cx/call-logs/call-url/{escapedFilename}` returning `{ "url": "https://…" }`.

## Run fake data only from the testing project

All demo data, simulated email, fake HTTP responses and the synthetic WAV are in **CRS.Tests**. Core/Web never reference the test project. To walk through screens without services, explicitly start the test-project host:

```sh
dotnet run --project tests/CRS.Tests --configuration Release -- 5174
```

This uses .NET's WebApplicationFactory/Kestrel test host to substitute the real registrations with test fixtures. It is a test executable, not a production runtime mode. Data resets when it restarts, and no real email is sent.

For automated Chromium checks, start a fresh test host, then with Node 22+:

```sh
npm ci --prefix tests/browser
cd tests/browser
npx playwright install chromium
cd ../..
npm test --prefix tests/browser
```

`CRS_BASE_URL` overrides local port 5174; `CHROMIUM_PATH` selects an installed browser. The script requires the test-host response header before performing writes. It checks cards/popups, validation, network-failure recovery, duplicate submission, independent downloads, keyboard/cancel, help-desk changes, Excel export and mobile navigation. CI runs Release tests and then this test-only host/browser walkthrough.

## Logging and validation

Serilog writes daily files under `C:\CallRatingSystem\Logs` on Windows or `Logs` below the Web content root on Linux. Seven-day retention runs on file open/roll; files also roll at 100 MB. The application needs folder write permission. Request monitoring is in `Middleware/ControllerLoggingMiddleware.cs`; function-level logging is in Core functions. Controller log messages omit raw paths, queries, headers and request bodies. No log cleanup runs while the process is stopped.

Tests use FakeItEasy for API/notification dependencies and SQLite **only in tests** to exercise actual EF queries, inserts, updates, null mapping and transactions. Live SQL Server, API bearer-token integration, real SMTP delivery, and real Entra sign-in still require deployment configuration and validation. The unused legacy task/patient controls are outside the active call-rating workflow. See `docs/testing.md` for coverage. Existing legacy configuration/history is not copied into the new projects.
