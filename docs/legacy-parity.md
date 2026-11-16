# Legacy call-rating comparison

This review compares the active legacy Call List/Admin pages, mail sender, master page, API/database helpers, configuration names, email resources, and supplied SQL script against the ASP.NET Core branch. It checks source behavior; it does not establish production service connectivity. Credential values were not copied from legacy configuration.

## Email corrections in this change

- Added explicit `Smtp:Username` and `Smtp:Password` entries. The sender already supported them, but the JSON omitted them. Supply the values through deployment configuration, environment variables, or user secrets.
- Restored the legacy help-desk destination, `hwvoip@rootsystem.co.za`, as the configured default. It remains overridable.
- Restored `DebugEmail` modes under `Smtp`: `0` live, `1` debug address only, `2` disabled. `DebugEmailAddress` is required in mode 1. Every message, including closed-call user feedback, is redirected in debug mode; no live-recipient fallback occurs.
- Development defaults to mode 2. Disabled delivery skips SMTP entirely, including when SMTP/recipient configuration is absent. The old helper attempted a send with no recipients in mode 2; that behavior is corrected.
- Added explicit TLS and timeout settings, defaulting to TLS enabled and 300 seconds. Both credentials must be provided together for authenticated SMTP. If `From` is blank, the username is used as the sender, matching the legacy behavior.
- Save/update results distinguish live notification success, debug-only delivery, disabled delivery, and delivery failures. Saving a rating still happens before attempting notifications.

## Remaining gaps and behavior differences

| Area | Legacy evidence | Current branch and impact |
| --- | --- | --- |
| Call API authentication | `CallRatingSystem/Classes/SqlConnections.cs`, `SetHttpClientTokenAsync`, obtains a token using `HW.CentralConfig.Package.Framework.SecurityTokenProvider` and attaches `Authorization: Bearer`. | `CallApiDataAccess` sends no bearer token. The old `CentralConfig:BaseUrl`, `CentralConfig:ClientId`, and `CentralConfig:AuthSalt` integration has not been migrated. Configuring only `CallApi:BaseUrl` will not satisfy an API that requires this authentication. This is the main remaining service integration gap. |
| Logout | `MasterPages/Main.Master.cs`, `btnLogout_Click`, clears authentication cookies and redirects through Entra logout using `ida:PostLogoutRedirectUri`. | Login is configured, but there is no application logout action or navigation button. The old post-logout destination has not been migrated. |
| Original email templates and subjects | `EmailMessage.resx` and the three send functions in the Call List/Admin controls use distinct new-rating, help-desk update, and user-feedback templates and subjects containing extension/call details. | `SmtpRatingNotifier` currently builds a simpler shared HTML body and generic subjects. Original layout and client/host IP fields in help-desk emails are missing. The data remains stored in ratings. |
| Closed-call feedback recipient | `CSP_GetCallRatingPage` in `database/CallRatingSystemDemo.sql` joins `ADUser` by telephone extension to obtain `Mail`, used by `SendHelpDeskCallRatingStatusEmailToUser`. | `CallRatingDataAccess.Describe` resolves email by the rating's user ID. If directory ownership and extension ownership differ, the recipient can differ. The intended recipient rule needs confirmation before changing delivery. |
| Client/host IP meaning behind a proxy | The old Call List reads raw `HTTP_X_FORWARDED_FOR` for `ClientIP` and `Request.UserHostAddress` for `HostIP`. | The controller uses server-observed remote/local addresses. Trusted forwarded-header/proxy configuration is not registered, so a reverse-proxy deployment will not reproduce those old IP meanings. Raw forwarded headers are not trusted automatically. |
| Debug call lookup | `SqlConnections.GetDataCallStatsAsync` checks `Debug == "1"` and replaces extension/date with a hardcoded extension and date. | That override was not ported. The test project supplies fake API data instead, and production always uses the actual user's extension/date. This differs from the legacy switch but preserves user ownership checks. |
| Paging and query implementation | Legacy pages execute stored procedures; the old page query subtracts IDs from the maximum ID. | EF Core replaces those procedures and pages the filtered ordered results. This intentionally avoids skipping rows when IDs have gaps. Existing SQLite tests cover the replacement; live SQL Server behavior still needs validation. |
| Access rules | Legacy code redirects IT users to Admin and checks role-name substrings containing `IT`/`Admin`. | Per the current requirements, all authenticated application users can use Call List and every Admin action requires the exact `CallRatingSystemAdmin` app role. This is an intended change. |

## Old settings that do not need blind copying

- `MsSqlConnection` and `MsSqlConnectionADUsers` correspond to the new `ConnectionStrings:CallRating` and `ConnectionStrings:Users` entries. Real values remain deployment requirements.
- `StatsApiBaseUrl` corresponds to `CallApi:BaseUrl`; the current localhost default must be replaced for the real API.
- `ida:*` login settings are replaced by `AzureAd`; the supplied ClientId/TenantId/ClientSecret placeholders still require deployment values and an Entra redirect registration.
- `AE_Email_Send_To`, `DocFolder`, `DebugNumber`, `ADService`, and `NpgSqlConnection` have no active references in the reviewed call-rating code. The active help-desk email functions hardcode their recipient instead of reading `AE_Email_Send_To`.
- SMS calls and email-history persistence are commented out in the legacy implementation. Generic mail CC/attachment support exists, but the reviewed call-rating callers always pass empty lists. They are not active call-rating behavior that was lost.
- `UN_Pabx_Connection` is read by the old `GetDataCallStatsSQL` helper, but no caller of that helper was found. Active calls/recordings use the HTTP API.
- Legacy task/patient helpers are outside the active Call List/Admin workflow and have not been migrated.

## Validation and outstanding deployment values

The automated suite covers local SMTP protocol/authentication, routing and suppression, managers, EF queries with SQLite, MVC workflows, Entra challenge construction with fixed local discovery metadata, and role enforcement. It does not send live email or validate a real Entra sign-in. SMTP host/credentials/sender, a debug address for mode 1, real database connections, API URL/authentication, and real AzureAd values remain necessary for the corresponding live services. Configure secrets externally; do not place them in tracked JSON.
