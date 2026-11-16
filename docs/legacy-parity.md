# Legacy call-rating parity

The ASP.NET Core branch now restores the active legacy Call List/Admin workflow while retaining the requested framework, folder, configuration, and access-rule changes. CentralConfig/API bearer-token integration is excluded at the owner's request; the owner will add it to the configured `HttpClient` in `Program.cs`. The application does not invent a replacement token protocol or fall back to demo services.

## Restored behavior

| Area | ASP.NET Core implementation |
| --- | --- |
| Login and sign-in errors | Entra configuration and middleware remain in `Program.cs`. `IUserSession` reads the logged-in ID, name, and email. Failed sign-in displays a generic retry page without exposing provider exception details. |
| Logout | The navigation has a Log Out form. `POST /account/logout` validates antiforgery, clears the application cookie, and performs Entra sign-out. `AzureAd:SignedOutCallbackPath` and `AzureAd:PostLogoutRedirectUri` configure the callback and final destination. The default destination is the public signed-out page. |
| SMTP settings | Explicit host/port, username/password, sender, help-desk recipient, TLS, and 300-second timeout settings are present. A blank sender falls back to the SMTP username, matching the old sender. The deployment template tokenizes every SMTP value. Development contains the legacy host/port and non-secret AE/debug recipient defaults, with blank username/password values for local secure configuration. |
| Debug email | `Smtp:DebugEmail` matches the old modes: `0` live, `1` debug address only, `2` no email. Mode 1 redirects both help-desk and feedback messages and adds a DEBUG subject prefix. Development restores legacy mode 1 and its debug address; deployment mode/address come from replacement tokens; missing debug settings never cause live-recipient fallback. |
| Original email templates | The original `EmailMessage.resx` HTML is copied into embedded templates under `CRS.Core/Notifications/Templates`. New reports, help-desk updates, and user feedback use their separate original bodies and subject formats. Help-desk messages include client/host IPs; blank Other rows are omitted. Inserted values are HTML encoded. |
| Notification sequence | Ratings/updates save first. On closure, feedback to the user is attempted even if help-desk delivery fails, as in the legacy workflow. Failures are reported without suggesting that the save should be repeated. |
| Feedback recipient | Rating author details come from user ID; feedback email comes from the directory record matching the telephone extension, matching `CSP_GetCallRatingPage`. Missing extension recipients do not fall back to the author's address. Conflicting addresses for one extension require directory cleanup rather than selecting an arbitrary person. |
| Client/host IPs | The connection's original remote IP is captured before forwarded headers are processed. Configured trusted proxies/networks can supply the client IP and HTTPS scheme. Ratings preserve the forwarded client IP and original peer/host IP. Arbitrary forwarding headers from untrusted clients are ignored. |
| Debug call lookup | `CallApi:Debug=1` overrides extension/date. `DebugExtension=330` and `DebugDate=2026-10-05` reproduce the old constants and can be configured. Other modes use the actual signed-in user's extension and selected date. Ownership checks use the explicitly configured effective extension/date. Debug lookup is off by default. |
| Optional selections and text | Pharmacy and Admin reason may be left unselected, as in the legacy forms. Supplied selections are still validated against the lookups. Legacy long Other/IT notes are preserved without the former arbitrary 4000-character cap. |
| Rating cards and saved markers | Call cards open the reporting popup; recording downloads remain independent. Saved calls are marked Reported. The marker uses stored ratings scoped to the current user/date and survives refreshes. Server validation preserves the popup's inputs. |
| Admin review popup | Review opens a popup with status, reason, IT note, and recording download, matching the old interaction. Validation stays in the popup; saving preserves the current filter/page. A complete page remains available when JavaScript is unavailable. |
| Recording permissions | Call List recording links and their endpoint use the legacy role-name convention containing PSP, IT, or Admin. Hiding the button is backed by server authorization. Every Admin endpoint still requires the exact `CallRatingSystemAdmin` role, per the current requirements. |
| Database workflow and reports | Existing rating/lookup tables and separate ADUsers database are used through EF Core. Rating inserts, status/reason/note updates, audit fields, filters, paging, and Excel/recording downloads remain available. No schema creation or seeding runs in production. |

## Intended differences retained

- Call List is available to all authenticated application users; Admin requires the exact `CallRatingSystemAdmin` Entra app role. The old IT redirect and broad Admin role-substring check are replaced by the explicitly requested rules.
- EF Core replaces the stored-procedure data access. Filtered paging does not reproduce the old ID-subtraction bug, and repeated rating submissions do not create duplicate rows.
- Email mode 2 skips SMTP instead of attempting a send without recipients. Saved/debug/disabled outcomes are distinguished from live notification success. HTML and spreadsheet values remain encoded as data.
- Forwarded headers require trusted proxy configuration. The old raw-header parsing and IPv6 truncation are not reproduced.
- Legacy SMTP/API/database/AzureAd secret values are not copied into tracked configuration. Real values must be supplied externally.

## Configuration mapping

| Old setting | New setting |
| --- | --- |
| `MsSqlConnection`, `MsSqlConnectionADUsers` | `ConnectionStrings:CallRating`, `ConnectionStrings:Users` |
| `StatsApiBaseUrl` | `CallApi:BaseUrl` |
| `Debug == 1` with hardcoded extension/date | `CallApi:Debug=1`, `CallApi:DebugExtension`, `CallApi:DebugDate` |
| `system.net/mailSettings/smtp/network` | `Smtp:Host`, `Port`, `Username`, `Password`, `EnableSsl` |
| SMTP username as From | `Smtp:From`, falling back to `Smtp:Username` |
| `DebugEmail`, `DebugEmailAddress` | `Smtp:DebugEmail`, `Smtp:DebugEmailAddress` |
| `AE_Email_Send_To` | `Smtp:AE_Email_Send_To`; retained separately from call-rating recipients |
| Hardcoded help-desk destination | `Smtp:HelpDesk`, defaulting to `hwvoip@rootsystem.co.za` |
| `ida:*` login settings | `AzureAd:*`; register the app's HTTPS origin plus `/signin-oidc` in Entra |
| `ida:PostLogoutRedirectUri` | `AzureAd:PostLogoutRedirectUri`; middleware uses `/signout-callback-oidc` as its callback |
| Raw forwarded headers | `ReverseProxy:TrustedProxies`, `TrustedNetworks`, and `ForwardLimit` |
| `CentralConfig:*` | Owner-provided token integration on the CallApi HttpClient; not implemented in this change |

## Legacy code outside the active workflow

`DocFolder`, `DebugNumber`, `ADService`, and `NpgSqlConnection` have no active references in the reviewed call-rating code. The legacy mail functions hardcode the help-desk recipient instead of reading `AE_Email_Send_To`. That AE address list is nevertheless restored explicitly in `Smtp:AE_Email_Send_To` for configuration parity; it is not automatically added to call-rating notifications. SMS and email-history persistence are commented out. Call-rating mail callers pass empty CC/attachment lists. The old direct-PABX SQL helper has no callers; active calls and recordings use the HTTP API. Unused task/patient helpers have not been added to the new application.

## Validation limits

Automated tests exercise templates, recipient lookup, SMTP authentication/routing against a local server, notifications, EF queries using SQLite, proxy handling, debug overrides, MVC workflows, role enforcement, logout/callback, and the Entra challenge with fixed local discovery metadata. Chromium checks exercise cards, both popups, validation, downloads, filters/paging, saved markers, and mobile navigation.

Live SQL Server, real SMTP, real Entra sign-in, and the owner's pending CentralConfig bearer-token integration still need deployment values and service validation. Source/workflow parity is not a claim that those live integrations have been tested.
