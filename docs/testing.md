# Test coverage

`dotnet test CRS.slnx --configuration Release` runs:

- Call manager validation, ownership/date checks, missing users/extensions/calls, input constraints, duplicate saves, API/database/email failures, cancellation and recording resolution.
- Help-desk paging/filtering, missing records, validation, persisted update orchestration, notification failure and spreadsheet generation/formula-like text.
- `CallApiDataAccess` URL/method/body contract, no built-in authentication, JSON parsing, HTTP errors, connection failures, timeout/cancellation and recording URLs.
- Real EF Core queries and SaveChanges against test-only SQLite databases: active lookups, directory reads, insert/update/audit fields, duplicate detection, nullable legacy columns, filters, untrusted text, failure translation and cancellation. SQL Server-specific transaction/concurrency behaviour still needs live database validation.
- Email recipients, closure feedback, HTML encoding and delivery/configuration errors.
- Workflow and access-denied controllers, call-card/popup fragments, validation, antiforgery, persistence orchestration, recording downloads, admin updates/export and sanitized error responses.
- Middleware monitoring only controller actions (not static files/unmatched requests), success/error events, and omission of query data. Serilog file output, folder errors and actual seven-day retention.
- The test-only data fixtures remain outside Core/Web. The optional test executable hosts fake dependencies for browser checks; production has no demo switch.

Authentication tests cover the Entra authorization-code login redirect with local discovery metadata, UserSession claim mapping, anonymous/invalid identities, Call List access without admin roles, exact CallRatingSystemAdmin role checks for every Admin action, navigation visibility, and the HTTP 403 Access Denied page and home link. Real Entra sign-in/token validation and directory app-role assignments require configured AzureAd values and are not exercised by these fixture tests. No test suite guarantees every possible failure. Live SQL Server/API/SMTP and Windows file-permission checks are not claimed by mocked/SQLite tests.
