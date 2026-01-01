# LenkCare Homes

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-16-000000?logo=nextdotjs)](https://nextjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![HIPAA](https://img.shields.io/badge/HIPAA-Compliant-success)](https://www.hhs.gov/hipaa/)

> **📋 TODO:** Integrate [Seq](https://datalust.co/seq) for centralized structured logging. This will enable system logs, app usage logs, and tour analytics to be collected, searched, and analyzed in a single location. See [Serilog.Sinks.Seq](https://github.com/datalust/serilog-sinks-seq) for .NET integration.

A **HIPAA-compliant** web application for managing adult family homes. LenkCare Homes provides a comprehensive solution for tracking residents, caregivers, daily care activities, and maintaining regulatory compliance.

> **⚠️ Internal Use Only** — This application is designed for authorized personnel only. No self-registration.

---

## 📋 Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Quick Start](#-quick-start)
- [Configuration](#-configuration)
- [Project Structure](#-project-structure)
- [Security & Compliance](#-security--compliance)
- [Documentation](#-documentation)
- [License](#-license)

---

## ✨ Features

### Core Functionality

| Module | Description |
|--------|-------------|
| **Home & Bed Management** | Track facilities, bed assignments, and occupancy |
| **Client Management** | Resident profiles, admissions, discharges, transfers |
| **Caregiver Management** | Staff accounts, home assignments, invitations |
| **Daily Care Logging** | ADLs, vitals, medications, ROM exercises, behavior notes |
| **Activity Tracking** | Individual and group recreational activities |
| **Unified Timeline** | Chronological view of all care activities |
| **Incident Reporting** | Report and track incidents with review workflow |
| **Document Management** | Secure document storage with per-document access control |
| **Reporting Module** | PDF reports for client and home summaries with data aggregation |
| **Developer Tools** | Synthetic data generation for testing (dev environment only) |

### Security & Compliance

- 🔐 **Passkey Authentication** — Phishing-resistant WebAuthn/FIDO2 passkeys with biometric support (Face ID, Touch ID, Windows Hello)
- 🛡️ **Role-Based Access Control** — Admin, Caregiver, and Sysadmin roles
- 📝 **Comprehensive Audit Logging** — All PHI access tracked in immutable logs
- 🔒 **Encryption** — At-rest and in-transit encryption for all data
- 🏠 **Home-Scoped Access** — Caregivers only see clients in their assigned homes
- 🛡️ **Security Headers** — CSP, X-Frame-Options, HSTS for defense-in-depth
- ⏱️ **Rate Limiting** — Protection against brute force attacks on auth endpoints
- ♿ **Accessibility** — WCAG 2.1 AA compliant with keyboard navigation support

---

## 🏗️ Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│   Next.js 16    │────▶│   .NET 10 API   │────▶│   Azure SQL     │
│   (Frontend)    │     │   (Backend)     │     │   (PHI Data)    │
│                 │     │                 │     │                 │
└─────────────────┘     └────────┬────────┘     └─────────────────┘
                                 │
                    ┌────────────┼────────────┬────────────┐
                    ▼            ▼            ▼            ▼
            ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐
            │ Cosmos DB │ │   Azure   │ │   Azure   │ │   Azure   │
            │  (Audit)  │ │   Blob    │ │   Email   │ │   Maps    │
            └───────────┘ └───────────┘ └───────────┘ └───────────┘
```

| Component | Technology | Purpose |
|-----------|------------|---------|
| Frontend | Next.js 16, React 19, Ant Design | User interface |
| Backend | .NET 10, ASP.NET Core, EF Core | REST API, business logic |
| Database | Azure SQL | PHI and transactional data |
| Audit Logs | Azure Cosmos DB | Immutable audit trail |
| Documents | Azure Blob Storage | Secure document storage |
| Email | Azure Communication Services | Invitations, notifications |
| Address Lookup | Azure Maps | Address autocomplete |

---

## 📦 Prerequisites

| Requirement | Version | Purpose |
|-------------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ | Backend development |
| [Aspire CLI](https://aspire.dev/get-started/install-cli/) | 13.0+ | Orchestration and tooling |
| [Node.js](https://nodejs.org/) | 20 LTS | Frontend development |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | Required for containers (see ARM64 note below) |
| [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) | Latest | Azure resource management (production) |

> **🖥️ Windows ARM64 (Copilot+ PCs):** The SQL Server Docker image doesn't support ARM64. Install [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) locally instead—the AppHost auto-detects ARM64 and uses the local instance via Windows Authentication.

---

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/Jcardif/LenkCareHomes.git
cd LenkCareHomes
```

### 2. Install Aspire CLI

```bash
# Windows (PowerShell)
winget install Microsoft.DotNet.Aspire.Cli

# macOS/Linux
curl -sSL https://aspire.dev/install.sh | bash

# Verify installation
aspire --version
```

### 3. Configure Email (Optional)

The only Azure service needed for development is **Azure Communication Services** for email. If not configured, emails are logged to console instead.

```bash
cd src/backend/LenkCareHomes.Server/LenkCareHomes.Api

# Set email secrets (optional)
dotnet user-secrets set "Email:ConnectionString" "<your-acs-connection-string>"
dotnet user-secrets set "Email:SenderAddress" "<your-verified-sender-email>"
```

### 4. Start the Application

```bash
cd src/backend/LenkCareHomes.Server

# Start with Aspire (recommended)
aspire run

# Or use dotnet directly
dotnet run --project LenkCareHomes.AppHost
```

Aspire will automatically:

- 🐳 Start **SQL Server 2025** container for PHI data
- 🐳 Start **Azurite** container for blob storage emulation
- 🐳 Start **Cosmos DB Emulator** for audit logs
- 🚀 Start the **.NET API** (waits for containers to be healthy)
- ⚛️ Start the **Next.js frontend** (auto-installs npm packages)

The **Aspire Dashboard** opens automatically at `https://localhost:15138`:

| Service | URL | Description |
|---------|-----|-------------|
| Aspire Dashboard | `https://localhost:15138` | Monitor all services |
| Frontend | `http://localhost:3000` | Next.js application |
| API (Scalar Docs) | `https://localhost:7141/scalar` | API documentation |
| Cosmos DB Explorer | `http://localhost:1234` | Audit log data explorer |

### 5. First Login

Use the default development credentials:

| Field | Value |
|-------|-------|
| **Email** | `admin@lenkcarehomes.local` |
| **Password** | `Admin@123456!` |

> **Note:** You'll be prompted to register a passkey on first login. We recommend registering passkeys on multiple devices for backup access.

---

## 🔮 Future Enhancements

### Push Notification Authentication

Push notification-based authentication is planned for when the mobile application is developed. This feature will allow users to approve login attempts by tapping a notification on their registered mobile device, providing an alternative to passkeys that is particularly convenient for users who frequently log in from shared or new devices. The implementation will include number matching, where the user must select the correct number displayed on the login screen to prevent approval of fraudulent requests.

---

## 🐳 Local Development with Docker

The project uses **Aspire 13** to orchestrate Docker containers, eliminating the need for Azure resources during development:

| Container | Image | Port(s) | Purpose |
|-----------|-------|---------|---------|
| `lenkcare-sql` | `mcr.microsoft.com/mssql/server:2025-latest` | 1433 | PHI data storage |
| `lenkcare-storage` | `mcr.microsoft.com/azure-storage/azurite:latest` | 10000, 10001, 10002 | Document storage |
| `lenkcare-cosmosdb` | `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` | 8081, 1234 | Audit logging |

> **🖥️ Windows ARM64 (Copilot+ PCs):** SQL Server Docker image is not available for ARM64. The AppHost automatically detects this and uses locally installed SQL Server with Windows Authentication instead. Azurite and Cosmos DB emulators work normally on ARM64.

### Container Data Persistence

Data volumes persist between restarts:

- `lenkcare-sql-data` — SQL Server database files
- `lenkcare-azurite-data` — Blob storage data

### Useful Commands

```bash
# Update Aspire packages
aspire update

# Clean up old containers
docker container prune

# View container logs
docker logs lenkcare-sql
```

---

## ⚙️ Configuration

### Development Configuration (Docker-based)

For local development, **Aspire handles most configuration automatically** using Docker containers. The only optional configuration is for email.

#### Email Configuration (Optional)

```bash
cd src/backend/LenkCareHomes.Server/LenkCareHomes.Api
dotnet user-secrets init

# Set email secrets (if you want actual emails instead of console logging)
dotnet user-secrets set "Email:ConnectionString" "endpoint=https://<resource>.communication.azure.com/;accesskey=<key>"
dotnet user-secrets set "Email:SenderAddress" "DoNotReply@<guid>.azurecomm.net"
```

#### Development Defaults

These are pre-configured in `appsettings.Development.json`:

| Setting | Value | Source |
|---------|-------|--------|
| SQL Connection | Auto-injected by Aspire | Docker: `lenkcare-sql` |
| Cosmos DB | `https://localhost:8081` (emulator key) | Docker: `lenkcare-cosmosdb` |
| Blob Storage | Azurite well-known key | Docker: `lenkcare-storage` |
| Admin Email | `admin@lenkcarehomes.local` | appsettings.Development.json |
| Admin Password | `Admin@123456!` | appsettings.Development.json |

### Production Configuration

For production, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or Azure Key Vault. **Never commit secrets to source control.**

#### Required Secrets for Production

```json
{
  "ConnectionStrings:SqlDatabase": "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;...",
  
  "CosmosDb:ConnectionString": "AccountEndpoint=https://<account>.documents.azure.com:443/;AccountKey=<key>;",
  "CosmosDb:DatabaseName": "LenkCareAudit",
  "CosmosDb:ContainerName": "AuditLogs",
  
  "BlobStorage:ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;...",
  "BlobStorage:ContainerName": "documents",
  
  "Email:ConnectionString": "endpoint=https://<resource>.communication.azure.com/;accesskey=<key>",
  "Email:SenderAddress": "DoNotReply@<guid>.azurecomm.net",
  
  "Auth:JwtSecret": "<base64-encoded-secret-key>",
  "Auth:FrontendBaseUrl": "https://your-production-domain.com",
  
  "SeedAdmin:Email": "admin@yourdomain.com",
  "SeedAdmin:Password": "<strong-password>"
}
```

#### Generate a JWT Secret Key

```powershell
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])

# Or use OpenSSL
openssl rand -base64 32
```

### Frontend (`.env.local`)

```bash
NEXT_PUBLIC_API_URL=https://localhost:7141/api
NEXT_PUBLIC_AZURE_MAPS_KEY=<your-azure-maps-subscription-key>
```

> **Note:** Azure Maps key is required for address autocomplete functionality. Get one from the [Azure Portal](https://portal.azure.com/).

---

## 📁 Project Structure

```
LenkCareHomes/
├── 📄 AGENTS.md                    # AI agent guidelines
├── 📄 README.md                    # This file
│
├── 📂 docs/
│   ├── Architecture and Design.md # System architecture
│   └── implementation/             # Phase implementation guides
│
├── 📂 src/
│   ├── 📂 backend/
│   │   └── LenkCareHomes.Server/
│   │       ├── LenkCareHomes.Api/           # Main API
│   │       │   ├── Controllers/             # REST endpoints
│   │       │   ├── Domain/                  # Entities, enums
│   │       │   ├── Models/                  # DTOs
│   │       │   ├── Services/                # Business logic
│   │       │   └── SyntheticData/           # Test data (dev only)
│   │       ├── LenkCareHomes.AppHost/       # Aspire orchestrator
│   │       └── LenkCareHomes.ServiceDefaults/
│   │
│   ├── 📂 datagen/                 # Synthetic data generator
│   │   ├── generate.py             # Main generation script
│   │   ├── config.py               # Data configuration
│   │   ├── generators.py           # Entity generators
│   │   └── README.md               # Generator documentation
│   │
│   └── 📂 frontend/
│       └── src/
│           ├── app/                # Next.js pages
│           ├── components/         # React components
│           │   ├── auth/           # Authentication
│           │   ├── care-log/       # Care logging tabs
│           │   └── layout/         # Layout components
│           ├── contexts/           # React contexts
│           ├── lib/                # Utilities, API client
│           └── types/              # TypeScript definitions
│
└── 📂 infra/                       # Infrastructure as Code
```

---

## 🔒 Security & Compliance

### HIPAA Requirements Met

| Requirement | Implementation |
|-------------|----------------|
| **Encryption at Rest** | Azure SQL TDE, Cosmos DB encryption |
| **Encryption in Transit** | TLS 1.2+ on all connections |
| **Access Controls** | Role-based access with home scoping |
| **Audit Trail** | Immutable Cosmos DB logs (6-year retention) |
| **Authentication** | Passkey (WebAuthn/FIDO2) with biometric support |
| **Minimum Necessary** | Caregivers access only assigned homes |
| **Security Headers** | CSP, X-Frame-Options, X-Content-Type-Options, HSTS |
| **Rate Limiting** | Brute force protection on auth endpoints |

### Accessibility (WCAG 2.1 AA)

LenkCare Homes is designed with accessibility in mind:

- **Keyboard Navigation** — Full application accessible via keyboard
- **Skip Links** — Bypass navigation to reach main content quickly
- **ARIA Labels** — Screen reader compatible with semantic landmarks
- **Color Contrast** — Minimum 4.5:1 contrast ratio for text
- **Focus Indicators** — Visible focus states for all interactive elements
- **Form Labels** — Properly associated labels and error messages

Visit `/help#accessibility` for our full accessibility statement.

### User Roles

| Role | Permissions |
|------|-------------|
| **Admin** | Full system access: manage homes, users, clients, view audit logs |
| **Caregiver** | Limited to assigned homes: view/log care for assigned clients |
| **Sysadmin** | System maintenance only: **no PHI access** |

### Password Policy

- Minimum 8 characters
- At least 1 uppercase, 1 lowercase, 1 digit, 1 special character
- No account lockout (HIPAA consideration for emergency access)

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Architecture and Design](docs/Architecture%20and%20Design.md) | Complete system specification |
| [Phase 1: Foundation](docs/implementation/Phase%201%20-%20Foundation%20and%20Authentication.md) | Auth, infrastructure setup |
| [Phase 2: Core Data](docs/implementation/Phase%202%20-%20Core%20Data%20Management.md) | Homes, clients, caregivers |
| [Phase 3: Care Logging](docs/implementation/Phase%203%20-%20Daily%20Care%20Logging.md) | ADLs, vitals, activities |
| [Phase 4: Incidents & Documents](docs/implementation/Phase%204%20-%20Incident%20Reporting%20and%20Document%20Management.md) | Incident reporting, document storage |
| [Phase 5: Reporting](docs/implementation/Phase%205%20-%20Reporting%20Module.md) | Reports, PDF generation |
| [Phase 6: Production](docs/implementation/Phase%206%20-%20Finalization%20and%20Production%20Readiness.md) | Deployment, optimization |
| [AGENTS.md](AGENTS.md) | AI coding agent guidelines |

---

## 🛠️ Development

### Running Tests

```bash
# Backend tests
cd src/backend/LenkCareHomes.Server
dotnet test

# Frontend tests
cd src/frontend
npm test
```

### Database Migrations

```bash
cd src/backend/LenkCareHomes.Server/LenkCareHomes.Api

# Create migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update
```

### Synthetic Data (Development Only)

Generate realistic test data for development and testing. The Python data generator is integrated with **Aspire** and can be started from the dashboard:

1. Open the **Aspire Dashboard** at `https://localhost:15138`
2. Find `lenkcare-datagen` in the service list
3. Click **Start** to run the generator

Alternatively, run manually:

```bash
cd src/datagen
pip install -r requirements.txt
python generate.py
```

This creates 2 years of synthetic data including:
- 6 homes opening organically over time
- Multiple caregivers per home
- Clients with realistic health profiles and discharges
- ~80,000 care log entries (ADLs, vitals, medications, etc.)
- Activities and incidents
- **PDF documents** for each client (care plans, consent forms, medical reports, etc.)

Load the data via **Settings → Developer Tools** (Sysadmin only, Development environment only). The UI shows **real-time progress** with step-by-step updates as data is loaded or cleared.

> ⚠️ The synthetic data feature is protected by multiple security layers and is completely disabled in production.

### Linting

```bash
# Frontend
cd src/frontend
npm run lint
```

---

## 📄 License

**Proprietary** — All rights reserved.

This software is confidential and intended for authorized use only. Unauthorized copying, distribution, or use is strictly prohibited.

---

<div align="center">

**Built with ❤️ for better healthcare management**

[Report Bug](https://github.com/Jcardif/LenkCareHomes/issues) · [Request Feature](https://github.com/Jcardif/LenkCareHomes/issues)

</div>
