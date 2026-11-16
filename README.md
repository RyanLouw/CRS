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
- **AdminController**: help-desk review popup, filters, paging, updates, recordings and Excel export.
- **CallApiDataAccess**: one ordinary class receiving an `HttpClient`. Calls the existing call-list and recording endpoints. `Program.cs` sets its base URL. The owner will supply the CentralConfig bearer-token integration on this HttpClient registration. CallApiOptions contains the optional legacy debug extension/date override.
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

`src/CRS.Web/appsettings.json` is the deployment template: all configuration values use `#{Section.Key}#` replacement tokens, including email addresses, credentials, connections, URLs, debug flags, logging and proxy settings. Replace these tokens for the target environment before running outside Development, and set `ASPNETCORE_ENVIRONMENT=Production` in live deployments so local Development overrides are not selected. Numeric/boolean token values are quoted in JSON; .NET configuration binds their replaced text to the correct types.

`src/CRS.Web/appsettings.Development.json` supplies the same complete set of settings as editable local values. The launch profiles select Development automatically. Local database strings are examples using localhost/Windows integrated authentication; change them for your database setup. SMTP username/password and AzureAd credentials are blank for you to supply. Use user secrets or environment variables for real local secrets so they do not get committed. Development values override the base template; user secrets/environment variables can override Development values.

Proxy token values can be a comma-separated list of IPs/CIDRs, or an empty string when no extra proxies/networks should be trusted. Each array entry can also contain one value. For a local override that clears a base array token, use `[""]` instead of `[]` because configuration arrays merge by index. Deployment token replacement must preserve JSON escaping for passwords/connection strings.

Set connection values externally using environment variables, user secrets (`dotnet user-secrets --project src/CRS.Web …`) or deployment configuration:

| Setting | Purpose |
| --- | --- |
| `CallApi__BaseUrl` | Base URL, configured on `AddHttpClient<CallApiDataAccess>` in Program.cs |
| `ConnectionStrings__CallRating` | Existing CallRatingSystem SQL Server database |
| `ConnectionStrings__Users` | Existing ADUsers SQL Server database |
| `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__HelpDesk` | Email delivery settings; default port 587 with TLS |
| `Smtp__Username`, `Smtp__Password` | Supply both for authenticated SMTP; leave both empty only for an unauthenticated relay |
| `Smtp__EnableSsl`, `Smtp__TimeoutSeconds` | TLS enabled by default; delivery timeout defaults to 300 seconds |
| `Smtp__AE_Email_Send_To` | Restored AE recipient list; separate from call-rating help-desk notifications |
| `Smtp__DebugEmail`, `Smtp__DebugEmailAddress` | `0` = live; `1` = redirect all mail to the debug address; `2` = no email |
| `Logging__Directory` | Absolute log folder override |
| `CallApi__Debug`, `CallApi__DebugExtension`, `CallApi__DebugDate` | Mode `1` enables the old debug lookup; defaults are extension 330/date 2026-10-05; mode `0` is live |
| `AzureAd__SignedOutCallbackPath`, `AzureAd__PostLogoutRedirectUri` | Sign-out callback and final destination; defaults `/signout-callback-oidc` and `/Home/SignedOut` |
| `ReverseProxy__TrustedProxies__0`, `ReverseProxy__TrustedNetworks__0`, `ReverseProxy__ForwardLimit` | Trusted proxy IP/CIDR entries and maximum forwarded hops; configure for the real deployment |

Entra ID sign-in is configured entirely in `Program.cs` with Microsoft.Identity.Web and the `AzureAd` configuration section. Replace the `#{AzureAd.ClientId}#`, `#{AzureAd.TenantId}#`, and `#{AzureAd.ClientSecret}#` placeholders through deployment substitution or external configuration (`AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__ClientSecret`). Do not commit a real client secret. Register the application's HTTPS origin plus `/signin-oidc` as a Web redirect URI in Entra. The supplied domain is `healthwindow.co.za`.

Both controllers require an authenticated user with a nonempty directory GUID in the Entra `oid`/objectidentifier claim (or `ClaimTypes.NameIdentifier` for other identities). Anonymous requests challenge Entra sign-in; invalid directory identities receive 403. Controllers use the scoped `IUserSession`/`UserSession` service for the current user's directory ID, display name, and email. The object ID is an identifier, not an access token. Call List is available to authenticated application users. Every Admin action requires the exact Entra application role `CallRatingSystemAdmin` in the `roles` claim. Assign that app role to the appropriate users or groups in Entra; group membership alone must result in the app role being issued. Users without that role see the Access Denied page with HTTP 403, and the Help desk navigation link is hidden. Tests use a separate test authentication handler and do not contact Entra.

The Log Out form signs out both the application cookie and Entra session through middleware configured in Program.cs. Sign-in errors display a generic retry page. Configure trusted proxy IPs/networks before using forwarded client addresses or HTTPS scheme; untrusted forwarding headers are ignored.

The production app always uses the actual data-access/EF/email registrations. It does not fall back to fake data if configuration is missing. The supplied `database/CallRatingSystemDemo.sql` documents the existing tables; no schema creation, migration or seed runs automatically. Apply/manage your development schema and its ADUsers/lookup data separately. EF maps nullable legacy columns and preserves the text CallDate/CallTime fields when saving. New ratings use status 1 = Open; closure uses 2 = Closed.

All SMTP fields in the base template use deployment tokens, including `#{Smtp.Username}#` and `#{Smtp.Password}#`. Editable local SMTP values are in `appsettings.Development.json`. Supply real credentials externally using `Smtp__Username` and `Smtp__Password`, user secrets, or deployment substitution. Unresolved placeholders fail before any SMTP connection. The default help-desk destination is the legacy `hwvoip@rootsystem.co.za` address. `From` may be configured separately; if omitted, the authenticated SMTP username is used as the sender, matching the old application. Development restores the legacy debug mode `1` and the legacy `Smtp:DebugEmailAddress`; the deployment template takes these values from tokens. Real credentials are still required before debug mail can be delivered. Use `Smtp__DebugEmail=2` to disable email. For debug mail use `Smtp__DebugEmail=1` and configure `Smtp__DebugEmailAddress`; both help-desk and user-feedback emails are redirected, with a `[DEBUG]` subject prefix. Missing debug configuration never falls back to live recipients. Saves report disabled/debug delivery separately from live notification success. Legacy mode 2 attempted to send without recipients; the new implementation skips delivery entirely.

The legacy migration comparison, completed work, and remaining deployment requirements are recorded in [docs/legacy-parity.md](docs/legacy-parity.md).

## Existing screen interaction

Open `/CallList` (also the default `/`). A card shows call information and its available download button. Selecting the card opens the help-desk popup for problems, pharmacy and other details. The download button does not select the card. Cancel closes the popup without saving. Server validation stays in the popup with entered values preserved. Pharmacy is optional, matching the old form, and legacy long comments remain supported. A successful submission saves the database row first, then sends email; delivery failure reports that the save succeeded. Duplicate submissions are checked in a serializable EF transaction. Previously reported calls display a Reported marker, scoped to the current user/date. Recording links and their endpoint preserve the legacy PSP/IT/Admin role-name convention.

`/Admin` shows rating cards, status filters and five-row paging. Review opens a popup, keeping validation inside it and preserving the selected filter/page after saving. Reason is optional, matching the old form. Updates persist status, reason, note and audit information and notify help desk; closing also emails the rating owner when an address exists. Excel export uses literal text cells for user-entered content. The original HW logo, CSS theme, colours and floating cards are reused locally.

| Route | Action |
| --- | --- |
| `GET /CallList?date=yyyy-MM-dd` | Calls for the current user's directory extension |
| `GET/POST /CallList/Rate` | Load/submit popup; full-page fallback without JavaScript |
| `GET /CallList/Recording?callId=…&date=…` | Resolve the selected call's recording |
| `GET /Admin?page=1&statusId=…` | Help-desk list/filter/page |
| `GET/POST /Admin/Edit` | Review/update popup, with full-page fallback |
| `POST /account/logout` | Antiforgery-protected cookie and Entra sign-out |
| `GET /Home/SignedOut` | Public sign-out confirmation |
| `GET /Home/AuthenticationError` | Generic sign-in retry page |
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
