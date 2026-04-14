# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Coding Standards & Conventions

Read @.maister/docs/INDEX.md before starting any task. It indexes the project's coding standards and conventions:
- Coding standards organized by domain (frontend, backend, testing, etc.)
- Project vision, tech stack, and architecture decisions

Follow standards in `.maister/docs/standards/` when writing code — they represent team decisions. If standards conflict with the task, ask the user.

### Standards Evolution

When you notice recurring patterns, fixes, or conventions during implementation that aren't yet captured in standards — suggest adding them. Examples:
- A bug fix reveals a pattern that should be standardized (e.g., "always validate X before Y")
- PR review feedback identifies a convention the team wants enforced
- The same type of fix is needed across multiple files
- A new library/pattern is adopted that should be documented

When this happens, briefly suggest the standard to the user. If approved, invoke `/maister:standards-update` with the identified pattern.

## Maister Workflows

This project uses the maister plugin for structured development workflows. When any `/maister:*` command is invoked, execute it via the Skill tool immediately — do not skip workflows for "straightforward" tasks. The user chose the workflow intentionally; complexity assessment is the workflow's job.

## Project Overview

IntegrationHub is a .NET 8 REST API serving as a shared integration platform. It proxies requests to external systems (SRP, CEP, KSIP, ANPRS, ZW) for internal clients (currently PIESP). The target environment is **offline-first** — no external internet dependencies at runtime.

## Build & Run

```bash
dotnet restore IntegrationHub.sln
dotnet build IntegrationHub.sln
dotnet test IntegrationHub.sln -c Release
```

The API entry point is `src/Core/Api/Program.cs`. Run with `dotnet run --project src/Core/Api`.

## Architecture

Three-tier modular structure with strict boundaries:

- **`src/Core/`** — Platform core (Api, Application, Domain, Infrastructure, Common). Client-agnostic.
- **`src/Clients/PIESP/`** — Client-specific module (auth, config, EF Core DbContext). Must not leak into Core.
- **`src/Sources/{SRP,CEP,KSIP,ANPRS,ZW}/`** — External system adapters. Each is an independent project. Must not couple with Clients.
- **`shared/`** — Reusable libraries targeting netstandard2.0 (CSV, Excel, Dapper extensions, BPS SDK extensions).
- **`tools/`** — Standalone tools (not part of runtime).

Request flow: **Controller → Application Service/Mapper → Domain Interface → Infrastructure/Source**

### Core Patterns

**`Result<T, Error>`** (`IntegrationHub.Common.Primitives`) — Functional error handling. Services return `Result` instead of throwing exceptions. Supports `Match()` for pattern matching, implicit conversions.

**`ProxyResponse<T>`** (`IntegrationHub.Common.Contracts`) — Unified API response envelope. Fields: `Status` (Success/BusinessError/TechnicalError), `Data`, `Message`, `Source`, `SourceStatusCode`, `RequestId`. All controllers return this type via `ProxyResponseMapper`.

**Source integration pattern** — Each source follows: Config class → HttpClient with resilience (Polly) + mTLS certs → Service interface (in Domain) → Service implementation (in Sources) → Test double for dev/test mode. Registration via `Add{Source}()` extension methods.

**Validation** — `IRequestValidator<T>` interface. Validate early in application layer, return errors as `Result<T, ValidationError>`.

### Middleware Pipeline

ForwardedHeaders → ApiAuditMiddleware (request/response audit to SQL) → ErrorLoggingMiddleware (Problem Details) → Serilog → JWT Authentication → Role Authorization

### Authentication

JWT Bearer tokens (HMAC-SHA256) with Active Directory (LDAP) backend. JTI blacklist for token revocation. Roles: User, Supervisor, PowerUser.

## Conventions

- **Code comments in Polish** — maintain this convention for inline comments.
- NuGet packages managed centrally via `Directory.Packages.props`. Do not add packages without justification.
- Versioning in `Directory.Build.props`.
- Business logic belongs in services/handlers, never in controllers.
- Each source is isolated — no cross-source or source-to-client dependencies.
- PII redaction in audit logs (PESEL, VIN patterns masked).
- DI registration: ensure new types are registered; use existing DI extension methods per module.
- Prefer Dapper over EF Core for new data access. EF Core is used for PIESP context.

## CI/CD

GitHub Actions (`dotnet_ci.yml`): restore → build Release → test on push to main and PRs.
`openapi_artifact.yml`: generates swagger.json artifact from SwaggerGenOnly mode.
