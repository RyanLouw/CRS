# Call Rating System — ASP.NET Core MVC

Open the root **CRS.slnx** in a .NET 10-compatible IDE. It contains:

```text
CallRatingSystem/             Existing Framework 4.8 project (default build excluded)
src/CRS.Web/                  MVC views, two controllers, middleware, Program.cs
src/CRS.Core/                 Models, managers, data access, EF Core contexts
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
| `Database__CallRating` | Existing CallRatingSystem SQL Server database |
| `Database__Users` | Existing ADUsers SQL Server database |
| `CurrentUser__Id` | Temporary server-configured directory user GUID until you wire sign-in |
| `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__HelpDesk` | Email delivery settings; default port 587 with TLS |
| `Smtp__Username`, `Smtp__Password` | SMTP credentials if needed |
| `Logging__Directory` | Absolute log folder override |

Authentication and role enforcement are deliberately left for you to connect later. There is no sign-in controller, Security folder, role-policy scaffolding, or demo branch in production code. `CurrentUser:Id` is a temporary server-side identity hookup, not authentication. Both controllers currently have no access policy. Replace that lookup with your signed-in user's directory ID and add your user/IT/admin permissions when wiring authentication. API authorization headers can be added to the configured HttpClient when ready; the data-access class does not require any authentication setup now.

The production app always uses the actual data-access/EF/email registrations. It does not fall back to fake data if configuration is missing. The supplied `database/CallRatingSystemDemo.sql` documents the existing tables; no schema creation, migration or seed runs automatically. Apply/manage your development schema and its ADUsers/lookup data separately. EF maps nullable legacy columns and preserves the text CallDate/CallTime fields when saving. New ratings use status 1 = Open; closure uses 2 = Closed.

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

Tests use FakeItEasy for API/notification dependencies and SQLite **only in tests** to exercise actual EF queries, inserts, updates, null mapping and transactions. SQL Server service connectivity, live API, SMTP and future authentication setup remain for your configuration. The unused legacy task/patient controls are outside the active call-rating workflow. See `docs/testing.md` for coverage. Existing legacy configuration/history is not copied into the new projects.
