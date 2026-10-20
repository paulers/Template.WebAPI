# Integration tests

The suite sends HTTP requests through the real API pipeline. It covers all 29 business and identity endpoints, `/health`, and the development-only OpenAPI document.

## Running the tests

From the repository root, with the .NET 10 SDK installed:

```sh
dotnet test Template.WebAPI.sln
```

Run a single group:

```sh
dotnet test tests/Template.WebAPI.IntegrationTests --filter FullyQualifiedName~InventoryTests
```

No running API, Docker, PostgreSQL instance, SMTP server, or local secrets are required.

## Endpoint coverage

All business and identity routes use the `/api/v1` prefix.

- `HomeTests`: `POST /homes`, `GET /homes`, and `GET`, `PUT`, `DELETE /homes/{id}`. Covers CRUD, sorted pagination, ownership, request validation, and deleting a home with rooms and inventory.
- `RoomTests`: `GET`, `POST /homes/{homeId}/rooms` and `PUT`, `DELETE /homes/{homeId}/rooms/{roomId}`. Covers CRUD, pagination, ownership, mismatched home routes, and room deletion preserving inventory with a null room reference.
- `InventoryTests`: `GET`, `POST /inventory` and `GET`, `PUT`, `DELETE /inventory/{id}`. Covers CRUD, home/room filters, pagination, optional room assignment, moving inventory, ownership, invalid assignments, and validation.
- `ProfessionalTests`: `GET`, `POST /pros`, `PUT`, `DELETE /pros/{id}`, and `GET /pros/types`. Covers CRUD, pagination, ownership, seeded types, unknown types, and validation.
- `IdentityTests`: `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET /auth/confirmEmail`, `POST /auth/resendConfirmationEmail`, `POST /auth/forgotPassword`, `POST /auth/resetPassword`, `GET`, `POST /auth/manage/info`, and `POST /auth/manage/2fa`. Covers email confirmation, bearer tokens, refresh, password reset/change, email changes, two-factor configuration, and rejected credentials/tokens.
- `HealthTests`: `GET /health`, development `GET /openapi/v1.json`, and OpenAPI being unavailable outside development.
- `AuthorizationTests`: unauthenticated requests to every protected endpoint, plus an invalid bearer token.
- `RequestValidationTests`: pagination boundaries for every paged endpoint, missing resources, and malformed JSON.

Endpoint coverage means each route and HTTP method is exercised. It does not imply complete branch or line coverage.

## Hosting and isolation

`ApiTest` creates and disposes an `ApiFactory` for each test case. Each factory holds one SQLite connection open for the host's lifetime, with foreign keys enabled. `EnsureCreatedAsync` creates the schema and the model's seeded professional types. Tests create their own data through HTTP and observe results through HTTP rather than querying the database to verify outcomes.

Authentication, authorization, validation filters, endpoint handlers, and EF Core are real. `TestEmailSender` replaces the external email boundary and captures confirmation links and password reset codes. The SMTP background worker is removed from the test host. Accounts are registered, confirmed, and logged in through the same endpoints used by clients.

Clients use HTTPS and disable automatic redirects and cookies, so authentication failures cannot be hidden by redirects or an earlier cookie session. The default environment is `Testing`; the OpenAPI test also starts a `Development` host.

Tests run sequentially because application hosts share the static Serilog logger. Data isolation comes from each test's separate database.

## SQLite limitations

SQLite exercises relational queries, `ExecuteDeleteAsync`, and foreign-key delete actions. It does not verify PostgreSQL migrations, PostgreSQL-specific SQL translation, data types, or database-enforced string lengths. Schema creation uses the current EF model, not the PostgreSQL migrations. HTTP validation tests cover the application's declared length limits.

SMTP delivery and the email background worker are outside this suite's coverage. A future PostgreSQL suite can verify migrations and provider-specific behavior without changing the HTTP test boundary.

## Adding tests

Derive from `ApiTest`. Use `AuthenticateAsync` for a real authenticated account and `Factory.CreateApiClient()` for another independent user. `CreateHomeAsync`, `CreateRoomAsync`, and `CreateInventoryAsync` create prerequisites through HTTP. Use `ReadAsync` or `ExpectAsync` to check responses; failures include the status and response body.

When adding an endpoint, add a successful request flow to its domain test class and add protected routes to `AuthorizationTests`. Add ownership and validation cases where they affect the endpoint's behavior.
