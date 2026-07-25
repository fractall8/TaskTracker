---
project_name: 'TaskTracker'
user_name: 'Maksym'
date: '2026-07-25'
sections_completed: ['technology_stack', 'language_specific_rules', 'domain_exceptions', 'framework_specific_rules', 'testing_rules', 'code_quality_rules', 'development_workflow_rules', 'critical_dont_miss_rules']
status: 'complete'
rule_count: 30
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

| Layer | Stack |
|---|---|
| Backend | .NET 10 (`net10.0`), ASP.NET Core Web API, EF Core 10.0.8 + Npgsql 10.0.2 (query only — schema via DbUp, not EF migrations), MediatR 14.1.0, FluentValidation 12.1.1, Serilog 4.3.0 / Serilog.AspNetCore 10.0.0, Hangfire 1.8.23 + Hangfire.PostgreSql 1.21.1, Microsoft.Identity.Web 4.10.0 |
| Frontend | Blazor WebAssembly (.NET 10), MudBlazor 9.5.0, Microsoft.Authentication.WebAssembly.Msal 10.0.8 |
| Microservice | Isolated Azure Functions Worker V4 (2.52.0), Azure.Messaging.ServiceBus 7.20.1, Microsoft.Azure.Cosmos 3.61.0, OpenTelemetry |
| Shared | `src/Shared/Contracts` — DTOs/enums/SignalR payloads referenced by both Backend and Frontend |
| Infra | Postgres 17-alpine, Azurite (Blob emulator), Stripe.net 52.1.1, Docker Compose (`develop.watch` hot-reloads on `Shared/Contracts` changes too) |

## Critical Implementation Rules

### Language-Specific Rules (C#)
- File-scoped namespaces mandatory; usings placed outside the namespace (build error otherwise)
- Braces required on all control flow statements
- Private fields: `_camelCase`; parameters: `camelCase` (enforced as build errors, not suggestions)
- Prefer primary constructors and `record` types for DTOs/Contracts; no expression-bodied constructors/operators/local functions elsewhere
- Nullable reference types + ImplicitUsings enabled everywhere — don't opt out per file
- Don't hand-format precisely — Husky's pre-commit `dotnet format style` pass handles staged `.cs` style, but won't fix naming/brace violations that fail the build

### Domain Exceptions & Error Handling
- Business-rule violations throw a subtype of `Domain.Exceptions.AppException` (abstract, carries `StatusCode` + `Title`), using a primary constructor — never throw raw `Exception`/`InvalidOperationException` for a domain rule
- `KeyNotFoundException`/`UnauthorizedAccessException`/`InvalidOperationException` are legacy-only fallbacks still mapped by `GlobalExceptionHandler` — don't add new usages, add a new `AppException` subtype instead
- All exceptions are caught centrally by `Presentation/Infrastructure/GlobalExceptionHandler.cs` and rendered as `ProblemDetails` — don't add per-controller try/catch for these
- `WorkspaceFeatureGuardBehavior` throws `SubscriptionFeatureRequiredException` (403) when a plan lacks a feature — this supersedes the `UnauthorizedAccessException` description in CLAUDE.md

### Framework-Specific Rules
- CQRS: one file per command/query (record + handler + validator together) under `Application/Features/<Feature>/{Commands,Queries}`
- Pipeline order is fixed: ValidationBehavior → LoggingBehavior → WorkspaceFeatureGuardBehavior
- Plan-gated commands/queries implement `IRequireWorkspaceFeature`; never hand-check entitlements in the handler
- Dependency direction: Presentation → Infrastructure/Persistence → Application → Domain (one-way, no exceptions)
- Controllers only dispatch MediatR requests — no logic/validation in controller actions
- Schema changes require BOTH an `IEntityTypeConfiguration` update AND a new numbered `Database/Scripts/000N_*.sql` — EF Core migrations are not used
- Frontend call chain is fixed: Refit API interface → `*ApiService` (unwraps via `response.HandleResponseAsync()` from `Services/Extensions/ApiResponseExtensions.cs`) → `Store` → components/pages. Components/pages must inject the Store only — never a Refit interface or `*ApiService` directly
- `HandleResponseAsync` throws a raw `Exception` (not a typed exception) with a message extracted from the backend's `ProblemDetails` (`Errors` → `Detail` → `Title` → raw content → generic fallback) — don't expect a typed/structured error on the frontend even though the backend throws typed `AppException` subtypes
- Real-time updates via SignalR hub services mirroring backend hubs (`BoardActionsHub`, `BoardExportStatusHub`)
- New gated feature must update both backend (`IRequireWorkspaceFeature`/`PlanCatalog`) and frontend (`WorkspaceSubscriptionsStore`) together
- `Shared/Contracts` DTO changes ripple to both Backend handlers and Frontend Refit interfaces/Stores — grep both before renaming

### Testing Rules
- No test project exists in this solution — do not invent test commands, assume a test framework (xUnit/NUnit/bUnit/etc.), or add test files unless explicitly asked to set one up first

### Code Quality & Style Rules
- One file per logical unit, organized in feature folders (Application/Features/<Feature>, Persistence/Configurations, Persistence/Repositories, Infrastructure/DI/Modules, Presentation/Controllers — one file each, matching the existing per-entity/per-feature/per-concern split)
- `GenerateDocumentationFile=true` but `CS1591` is suppressed — XML doc comments are not required; don't add them just to satisfy a warning that's already silenced
- Match existing comment density: sparse, only for genuinely non-obvious behavior (e.g. legacy-fallback notes) — no narrative/explanatory comments

### Development Workflow Rules
- Branches: `feature/<name>` merged to `main` via PR
- Commits: conventional-commit prefixes (`feat:`, `fix:`) — match this style
- Husky.Net pre-commit auto-runs `dotnet format style` on staged `.cs` and re-stages — never bypass with `--no-verify`
- Local dev has two modes: full `docker-compose up -d --build`, or hybrid (`docker-compose up -d postgres-db db-migration azurite` + native `dotnet run` for API/frontend)
- `.env` (copied from `.env.example`) is required before any `docker-compose up` — missing values fail container startup

### Critical Don't-Miss Rules
- Serilog redaction (`SensitiveDataDestructuringPolicy`) matches property names exactly (`Password`, `Token`, `RefreshToken`, `ClientSecret`, `AccessToken`) — add any new secret-bearing property name to this list, don't assume it's covered. Known gap as of this writing: names like `ApiKey`, `Secret`, `Pin` are NOT covered and will log in the clear — flagged for a future fix, not yet resolved
- `InternalApiKeyMiddleware` only guards paths under `/api/internal` — new internal/service-to-service endpoints must live under that prefix to get the `X-Internal-Api-Key` check

---

## Usage Guidelines

**For AI Agents:**
- Read this file before implementing any code in this project
- Follow ALL rules exactly as documented; when in doubt, prefer the more restrictive option
- Update this file when a new pattern emerges or an existing rule turns out to be wrong

**For Humans:**
- Keep this file lean and focused on agent needs — don't let it re-derive what's obvious from `CLAUDE.md` or the code itself
- Update when the technology stack or a cross-layer convention changes
- Revisit the "Known gap" note under Critical Don't-Miss Rules once the Serilog redaction list is fixed

Last Updated: 2026-07-25
