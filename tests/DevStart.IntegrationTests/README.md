# DevStart.IntegrationTests

HTTP-level integration tests that exercise the real WebApi pipeline end-to-end:
routing → JWT auth → FluentValidation → CQRS handlers → EF Core → PostgreSQL.

## How it works

- **Real database.** Each test run starts a throwaway PostgreSQL container via
  [Testcontainers](https://dotnet.testcontainers.org/) (`postgres:17-alpine`). EF Core migrations and
  the startup seeders (consent documents, term-sheet templates) run exactly as in production.
- **Real HTTP.** Tests drive the app through an `HttpClient` over `WebApplicationFactory<Program>`
  (`Microsoft.AspNetCore.Mvc.Testing`), so the full middleware pipeline runs.
- **Faked externals.** Redis, MinIO, YooKassa, Centrifugo, SMTP, the Hangfire job scheduler and the
  OAuth providers are replaced with in-memory fakes (see `Fakes/`), so tests are deterministic,
  offline and free. Configuration that the host validates at startup (`Jwt`, `OAuth`, `YooKassa`, …)
  is supplied via environment variables in `IntegrationTestWebAppFactory.InitializeAsync` — it must be
  set before `WebApplication.CreateBuilder` reads it, which is too early for `ConfigureAppConfiguration`.
- **Isolation.** All tests share one container/host (collection `"Integration"`) and run sequentially.
  Before each test [Respawn](https://github.com/jbogard/Respawn) truncates the `public` schema (keeping
  the migrations history and seeded `consent_documents`) and the in-memory cache is flushed.
- **Rate-limit partitioning.** The TestServer has no real client IP, so a startup filter sets
  `RemoteIpAddress` from an `X-Test-Client-Ip` header. Each test gets a unique IP → its own rate-limit
  partition, except `AuthRateLimitingTests`, which reuses one IP on purpose to trip the 429.

## Running

Docker must be running.

```bash
dotnet test tests/DevStart.IntegrationTests
```

## Coverage

| Area | File |
|------|------|
| Auth — register / consents / verification email | `Auth/RegisterTests.cs` |
| Auth — login, enumeration-safety, verification gate | `Auth/LoginTests.cs` |
| Auth — refresh-token rotation | `Auth/RefreshTests.cs` |
| Auth — per-IP rate limiting (429) | `Auth/AuthRateLimitingTests.cs` |
| Startups — create (member + product), public read, auth, uniqueness | `Startups/StartupTests.cs` |
| Subscriptions — checkout redirect, free promo activation | `Subscriptions/CheckoutTests.cs` |
| Admin moderation — ban, grant subscription, promo codes, authz | `Admin/AdminModerationTests.cs` |

## Adding a test

Derive from `IntegrationTestBase`, annotate the class with
`[Collection(IntegrationTestCollection.Name)]`, and use the helpers: `SeedUserAsync(...)`,
`CreateAuthenticatedClient(user)`, `CreateClient()`, `ExecuteDbAsync(...)`. Configure or assert on the
fakes via `Factory` (e.g. `Factory.EmailSender`, `Factory.PaymentProvider`).
