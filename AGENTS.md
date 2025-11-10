# Repository Guidelines

## Project Structure & Module Organization
- `HabitFlow.sln` — solution root grouping Backend and Frontend.
- `HabitFlow.Api/` — ASP.NET Core Minimal API (OpenAPI in Development).
- `HabitFlow.Blazor/` — Blazor Server app (`Components/`, `wwwroot/`).
- `appsettings.json` and `appsettings.Development.json` exist in both projects for configuration.

## Build, Test, and Development Commands
- `dotnet restore` — restore dependencies for the solution.
- `dotnet build` — build all projects in Debug.
- `dotnet run --project HabitFlow.Api` — start the API (e.g., `/weatherforecast`).
- `dotnet run --project HabitFlow.Blazor` — start the Blazor Server app.
- `dotnet watch run --project <ProjectDir>` — hot-reload during development.

## Coding Style & Naming Conventions
- Language: C# (net9.0), `Nullable` and `ImplicitUsings` enabled.
- Indentation: 4 spaces; file-scoped namespaces; expression-bodied members when clear.
- Naming: `PascalCase` for types/methods/properties; `camelCase` for locals/params; `_camelCase` for private fields.
- Formatting: run `dotnet format` before committing.

## Backend

### Architecture

- Follow Clean Architecture: Domain → Application → Infrastructure → Api.
- Apply CQS: separate Command and Query paths.
- No MediatR. Use a lightweight in-house dispatcher and contracts:
  - ICommand, ICommandHandler<TCommand, TResult>
  - IQuery<TResult>, IQueryHandler<TQuery, TResult>
  - ICommandDispatcher, IQueryDispatcher (registered in DI).
- Validation should run before handler execution (e.g., pipeline behavior in the dispatchers).
- All handlers support CancellationToken.

### Entity Framework Core (SQL Server)

- Code First with migrations as the source of truth.
- Prevent N+1 with eager loading (Include/ThenInclude) only where necessary.
- Queries: always use AsNoTracking and project with Select into DTOs/records to limit columns.
- Optimize hot paths with compiled queries and ensure appropriate DB indexing.
- Use value converters for custom value objects and specialized identifiers.

### Repository and Unit of Work

- Repositories are used only for Commands (state changes) in Infrastructure; DbContext acts as the Unit of Work.
- Query handlers do not use repositories:
  - Option A: inject DbContext directly into query handlers (read-only policies, AsNoTracking).
  - Option B: define read-side interfaces in Application (e.g., IFooReadStore) implemented in Infrastructure over EF Core to avoid EF types leaking into Application.
  - Option C: expose specialized read services in Infrastructure that return pre-shaped DTOs or IAsyncEnumerable for streaming scenarios.
- Transactions: rely on DbContext.SaveChanges/SaveChangesAsync; use explicit transactions/TransactionScope only when a single unit of work spans multiple aggregates/resources.

### ASP.NET API

- Use Minimal APIs for endpoints.
- Endpoint mapping conventions:
  - POST/PUT/PATCH/DELETE → send Command via ICommandDispatcher.
  - GET → send Query via IQueryDispatcher.
- Errors and results:
  - Prefer Result/Result<T> for domain/validation/expected errors; do not use custom exceptions for control flow.
  - Handlers return Result<T>; endpoints translate Result to HTTP responses.
  - A global ProblemDetails mapper converts failures to RFC 7807 ProblemDetails with consistent error codes, titles, and details.
  - Reserve exceptions only for unexpected faults; the global exception handler maps them to 500 ProblemDetails.
- DI: DbContext is scoped; register dispatchers and scan/register handlers (or register them explicitly).

### Database and SQL Server

- Parameterized queries (default in EF Core) to prevent SQL injection.
- Index according to read/write patterns and common filters/sorts.
- Consider views or precomputed read models for heavy reporting if needed.

## Testing Guidelines
- No test projects yet. Prefer xUnit:
  - Create `HabitFlow.Api.Tests/` and `HabitFlow.Blazor.Tests/`.
  - Name files `*Tests.cs`; one class per unit under test.
  - Run with `dotnet test` (add to solution once created).

## Commit & Pull Request Guidelines
- Commits: concise, imperative. Conventional Commits style is encouraged:
  - Examples: `feat(api): add habit endpoints`, `fix(blazor): correct nav styling`.
- PRs: include purpose, linked issues (e.g., `Closes #123`), testing steps, and screenshots for UI.
- Keep changes focused; update docs/config when behavior changes.

## Security & Configuration Tips
- Do not commit secrets. Prefer environment variables or `dotnet user-secrets` in development.
- Use `ASPNETCORE_ENVIRONMENT=Development` locally. Trust HTTPS certs with `dotnet dev-certs https --trust`.
- Place non-sensitive settings in `appsettings.Development.json`; production values via environment or secret store.

## Agent-Specific Instructions
- Keep edits scoped to the touched project (`Api` or `Blazor`).
- Favor minimal APIs and records in the API; Razor Components with server render mode in Blazor.
- When adding new projects, nest under existing solution folders (Backend/Frontend) in `HabitFlow.sln`.

## Product Spec (PRD)
- Główny dokument PRD: `.ai/prd.md`.
- Aktualizuj przy zmianach zakresu/priorytetów i linkuj powiązane issue/PR.
