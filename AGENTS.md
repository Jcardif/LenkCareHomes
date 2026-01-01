# AGENTS.md

LenkCare Homes is a HIPAA-compliant web application for managing adult family homes. It tracks residents (clients), caregivers, daily care activities, incidents, and documents. This is an **internal-only** application—no public access or self-registration. The guidance below follows the [AGENTS.md](https://agents.md/) format so every coding agent sees the same constraints, build steps, and security rules.

---

## Project Overview

| Component | Technology | Location |
|-----------|------------|----------|
| Frontend | Next.js 16 + React 19 | `src/frontend/` |
| Backend | .NET 10 Web API + Aspire | `src/backend/LenkCareHomes.Server/` |
| AppHost | Aspire orchestrator | `LenkCareHomes.AppHost/` |
| ServiceDefaults | Shared Aspire config | `LenkCareHomes.ServiceDefaults/` |
| Target DB (PHI) | Azure SQL | — |
| Target DB (Audit) | Azure Cosmos DB | — |
| Target Storage | Azure Blob Storage | — |
| Documentation | Architecture + implementation phases | `docs/` |

**Current State:** Project is scaffolded with basic Aspire setup. Frontend currently uses Tailwind but should migrate to **Ant Design** per architecture spec.

---

## Prerequisites

- .NET SDK **10.0** (check via `dotnet --version`).
- **Aspire CLI 13.0+** (install via `curl -sSL https://aspire.dev/install.sh | bash` or see [aspire.dev](https://aspire.dev)).
- Node.js **20 LTS** + npm 10 (Next.js 16 requirement). Use `nvm use 20` if needed.
- **Docker Desktop** (required for local development containers).
- PowerShell 7+ or Bash for scripts.
- Ensure certificates and secrets are injected via environment variables or Azure Key Vault; never commit them.

> **Windows ARM64 (Copilot+ PCs):** The SQL Server Docker image doesn't support ARM64. Install SQL Server locally instead—the AppHost auto-detects ARM64 and connects via Windows Authentication. Define the connection string in `appsettings.Development.json` under `ConnectionStrings:SqlDatabase`.

---

## Local Development with Docker

The project uses **Aspire** to orchestrate Docker containers for local development, eliminating the need for Azure resources during development:

| Service | Container Image | Port(s) | Purpose |
|---------|----------------|---------|---------|
| **SQL Server** | `mcr.microsoft.com/mssql/server:2025-latest` | 1433 | PHI data storage |
| **Azurite** | `mcr.microsoft.com/azure-storage/azurite` | 10000 (blob), 10001 (queue), 10002 (table) | Document storage |
| **Cosmos DB Emulator** | `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` | 8081 (gateway), 1234 (explorer) | Audit logging |

### Windows ARM64 (Copilot+ PCs)

The SQL Server Docker image is **not available for ARM64**. On Windows ARM64 machines, the AppHost automatically:
1. Detects the ARM64 architecture at runtime
2. Skips the SQL Server container
3. Uses locally installed SQL Server via Windows Authentication

The connection string is configured in `LenkCareHomes.AppHost/appsettings.Development.json`:
```json
"ConnectionStrings": {
  "SqlDatabase": "Server=localhost;Database=LenkCareHomes;Integrated Security=True;TrustServerCertificate=True"
}
```

Azurite and Cosmos DB Emulator containers work normally on ARM64.

### User Secrets (Development Only)

The only Azure service required in development is **Azure Communication Services** for email. Configure it via user secrets:

```powershell
cd src/backend/LenkCareHomes.Server/LenkCareHomes.Api
dotnet user-secrets set "Email:ConnectionString" "<your-acs-connection-string>"
dotnet user-secrets set "Email:SenderAddress" "<your-verified-sender-email>"
```

If email is not configured, emails will be logged to console instead of sent.

---

## Setup Commands

```powershell
# Backend - run from src/backend/LenkCareHomes.Server/
dotnet run --project LenkCareHomes.AppHost  # Starts Aspire dashboard + Docker containers + API

# Or use Aspire CLI (Aspire 13+)
aspire run                                      # Same as above but with enhanced CLI features

# Frontend - run from src/frontend/
npm install
npm run dev      # Next.js dev server at http://localhost:3000
npm run build    # Production build
npm run lint     # ESLint checks
```

### Backend tips

- `dotnet restore` at the solution root (`src/backend/LenkCareHomes.Server/`) to prime packages.
- `dotnet run --project LenkCareHomes.AppHost` or `aspire run` boots the Aspire dashboard, Docker containers (SQL Server, Azurite, Cosmos DB Emulator), API, and frontend.
- Aspire 13 automatically handles npm package installation for the frontend via `AddJavaScriptApp()`.
- Aspire ensures containers are running and healthy before starting the API.
- Data volumes (`lenkcare-sql-data`, `lenkcare-azurite-data`) persist data between restarts.
- Use `aspire update` to keep Aspire packages up to date.
- Use `dotnet test` at the solution root once backend tests exist.

### Frontend tips

- Stick to npm (no pnpm/yarn) unless package.json changes.
- `npm run dev -- --turbo` is available once Next.js caching is configured.
- `npm run lint` must be clean before committing; it enforces strict mode and Ant Design migration rules.

---

## Testing Instructions

- Backend: `dotnet test` from solution folder (when tests exist)
- Frontend: `npm test` from `src/frontend/` (when tests exist)
- API health check endpoints (dev only): `/health` (readiness), `/alive` (liveness)
- Run all linting before committing: `npm run lint` and verify no errors
- Prefer small, focused test runs: backend projects should add xUnit; frontend should add Vitest/RTL per docs.
- Add or update tests for every feature touching PHI or authorization.
- Capture Aspire dashboard screenshots for regressions affecting distributed tracing.

---

## Code Style

### Backend (.NET)

- Use `builder.AddServiceDefaults()` in Program.cs for consistent Aspire integration
- Follow existing pattern in `Extensions.cs` for service configuration
- Health checks must be dev-only (already configured in ServiceDefaults)
- OpenTelemetry tracing is enabled via ServiceDefaults—don't disable it
- Target: ASP.NET Core Identity with TOTP MFA for authentication
- All PHI operations **must** log to Cosmos DB audit trail
- Embrace dependency injection and minimal APIs; keep controllers slim and push logic into services.
- Async all the way—every I/O-bound method takes a `CancellationToken` and ends with `Async`.
- Guard arguments with `ArgumentNullException.ThrowIfNull` / `string.IsNullOrWhiteSpace`.
- Prefer records for DTOs, configure EF Core schemas explicitly, and keep secrets in configuration providers (Key Vault or environment variables).

### Frontend (Next.js/React)

- App Router pattern in `src/app/` directory
- Target UI library: **Ant Design** (migration from Tailwind needed)
- TypeScript strict mode is enabled—do not disable
- ESLint config in `eslint.config.mjs`—follow existing rules
- Use functional components with hooks
- No `any` types without justification
- Use Ant Design tokens/components; delete Tailwind classes as the migration progresses.
- Organize server actions under `src/frontend/src/app/(routes)/actions` once created; all fetches go through a typed API client.
- Keep shared UI in `src/frontend/src/components`; co-locate tests with components using `.test.tsx`.
- Never store PHI in client-side caches longer than necessary; prefer server-side rendering + React Server Components.

### General Principles

- **DRY:** Extract shared logic into reusable services, components, or utilities. If logic exists elsewhere, reference it.
- **KISS:** Prefer straightforward solutions. Avoid over-engineering or premature abstractions.
- **Security First:** This is a HIPAA application. Every feature touching PHI must consider audit logging, access control, and encryption.
- Favor feature toggles (appsettings or env) over long-lived branches.
- Write infrastructure-as-code friendly changes; keep environment variables documented.
- Document architectural decisions in `docs/Architecture and Design.md` when introducing new patterns.

---

## Security Requirements (HIPAA)

⚠️ **Critical:** All code must comply with HIPAA requirements.

| Requirement | Implementation |
|-------------|----------------|
| Encryption at rest | Azure TDE for SQL, AES-256 for Blob Storage |
| Encryption in transit | TLS 1.2+ required on all connections |
| MFA | Required for all users (TOTP via ASP.NET Core Identity) |
| RBAC | Admin, Caregiver (home-scoped), Developer/Sysadmin |
| Home-scoped access | Caregivers can only access clients in their assigned homes |
| Document access | Per-document permissions, view-only for caregivers (no downloads) |
| Audit logging | Log every PHI access and modification to Cosmos DB |
| Data retention | Minimum 6 years, no auto-purge |

### Additional guidance**

- Use TLS 1.2+ everywhere (Aspire already configures HTTPS redirection).
- Restrict health endpoints to dev/test; production requires auth + IP allow listing.
- Rotate keys/secrets quarterly and store rotation notes in the private runbook.
- Cosmos DB audit container must never be truncated—append-only semantics.

---

## User Roles

| Role | Permissions |
|------|-------------|
| **Admin** | Full access: manage homes, beds, clients, caregivers, documents, reports, audit logs |
| **Caregiver** | Limited to assigned home(s): view clients, log ADLs/vitals/notes, view permitted documents (no download) |
| **Developer/Sysadmin** | System maintenance only—**cannot access or modify PHI** |

---

## Key Documentation

- [Architecture and Design](docs/Architecture%20and%20Design.md) — Full system spec with all functional requirements
- [Implementation Phases](docs/implementation/README.md) — Detailed phase-by-phase breakdown
- [C# Development Guidelines](.github/instructions/csharp-guide.instructions.md) — .NET coding standards, patterns, and best practices
- [Next.js Development Guidelines](.github/instructions/nextjs-guide.instructions.md) — React 19 and Next.js 16 patterns and conventions
- [AGENTS.md reference site](https://agents.md/) — Shared context for agents working in this repo

---

## Notes

- This is an **internal-only** system; never expose endpoints publicly or allow self-registration.
- Data containing PHI must never leave approved Azure resources. Local fixtures should be synthetic and anonymized.
- When in doubt, ask the security officer—compliance requirements take precedence over delivery speed.
