# CallRatingSystem ASP.NET Core migration

The new .NET 10 solution is `CRS.slnx`:

- `src/CRS.Core`: models, with managers, business rules, and data/API access to be migrated here.
- `src/CRS.Web`: MVC controllers, Razor views, assets, and application composition.
- `tests/CRS.Tests`: xUnit tests, FakeItEasy for dependencies, and MVC integration tests.

The existing Framework 4.8 applications are retained as migration references. Call-list/rating business functionality and authentication have not yet been migrated. The current new application is the MVC scaffold with logging.

```sh
dotnet restore CRS.slnx
dotnet build CRS.slnx --no-restore
dotnet test CRS.slnx --no-build
dotnet run --project src/CRS.Web --no-launch-profile
```

## Logging

Serilog writes startup, shutdown, request completion/failure events and application errors to console and daily rolling files. Windows defaults to `C:\CallRatingSystem\Logs\crs-YYYYMMDD.log`. Linux defaults to `Logs` under the Web content root. Override with the absolute `Logging__Directory` environment variable. The application identity needs write access to that folder.

Files are retained for seven days, with retention applied when the sink opens/rolls a file (startup, daily roll, or size roll). No background deletion happens while the application is stopped. Files also roll at 100 MB. Logs and local secret files are ignored by Git. HTTP event logs omit raw paths, query strings, headers, request/response bodies and caller details. Do not include credentials or sensitive data in application log messages or exception text. Operation-specific logging and failure tests must accompany each migrated feature; no finite test suite covers every possible failure.

## Secrets

Use environment variables, a deployment secret store, or .NET user secrets for development; do not put passwords/tokens into tracked configuration. The new projects contain no database/API credentials. The legacy tracked `Web.config` files already contain password-bearing connection strings: rotate those credentials and coordinate repository-history cleanup separately. No legacy configuration values were copied into the new application.

## Tests

Tests exercise log path validation, inaccessible path failure, actual file writing and seven-day retention, MVC view rendering, and safe logging of a 404 request with a sensitive query parameter. Live SQL/API/AD checks await secure development configuration.
