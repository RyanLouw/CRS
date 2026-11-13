# Migration validation map

Run `dotnet test CRS.slnx --configuration Release`. Tests isolate dependencies with FakeItEasy and HTTP handlers; no real database, email, caller recordings, or credentials are needed.

| Area | Tested cases |
| --- | --- |
| Call manager | Own extension/date filtering, empty calls, missing user/extension, missing identity, future/invalid dates, forged call IDs, valid/inactive problems/pharmacies, duplicate problem selection, required Other details, length limits, trusted save fields, duplicates, API/database/email failures, cancellation, recording ownership/missing recordings |
| Help-desk manager | Empty/boundary pages, gaps in IDs, deterministic sort, status filters, missing ratings, invalid status/reason/notes, updates, concurrent deletion, failed database/email, stored recording key, valid/empty XLSX export, formula-like user input |
| API client | Exact request URL/method/body/bearer contract, empty/null response, parsing, malformed/incomplete JSON, HTTP 400/401/403/404/429/500/503, connection failure, timeout vs caller cancellation, missing token, filename escaping, allowed/unsafe/missing recording URLs |
| SQL adapter | Correct database/procedure selection, parameterized GUID/filter/ID/value binding, null/missing/aliased fields, duplicate/update result interpretation, transactional locking contract, missing configuration and propagated failures. SQL Server execution itself needs a live development database |
| Notifications | Initial/update/closure recipients, absent user email, HTML encoding, SMTP errors, absent recipient/configuration. No mail is sent by tests |
| Identity/demo | Supported directory ID claims, missing/malformed IDs, model validation, concurrent duplicate requests, demo isolation, demo rejection outside Development |
| HTTP integration | Anonymous challenges, exact-role restrictions, CSRF on sign-in/rating, operator save/duplicate/validation, admin filtering/paging/update/export, PSP WAV download, not-found/bad-input responses, sanitized dependency/unexpected errors, sign-out |
| Logging | Folder override/default, relative/occupied folder failure, actual log output, actual old/recent file retention, request-query/path privacy |
| Browser | Real Chromium operator/admin walkthrough, form validation, save/duplicate, paging/filter/update, Excel/WAV downloads, legacy theme/mobile navigation, no JavaScript errors |

Required live follow-up, owned by deployment configuration: SQL schema/permissions and directory/lookup data, API token acquisition and service responses, SMTP delivery, Entra tenant/roles/callbacks, and Windows log-folder write access. These were not run and must not be inferred from mocked tests. No automated test can cover every possible failure.
