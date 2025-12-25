# LenkCare Homes Infrastructure

This folder contains Bicep templates for deploying the LenkCare Homes application to Azure.

## Architecture Overview

The deployment creates the following Azure resources:

| Resource | Purpose |
|----------|---------|
| **Key Vault** | Stores application secrets and configuration |
| **Storage Account** | Document storage (blob container with CORS) |
| **Azure SQL Server** | Primary database for PHI data |
| **Cosmos DB** | Audit logging (HIPAA compliance) |
| **App Service Plan** | Hosts API and Frontend apps (Linux) |
| **API App Service** | .NET 10 backend API with managed identity |
| **Frontend App Service** | Next.js 16 frontend (Node.js 22) |
| **Communication Services** | Email sending (created manually via Azure Portal) |
| **Azure Maps** | Location and geocoding services |

## Naming Convention

Resources follow the pattern: `<abbr>-<environment>-<project>-<location>`

Examples:

- `kv-dev-lenkcare-wus2` (Key Vault)
- `sql-staging-lenkcare-wus2` (SQL Server)
- `app-prod-api-lenkcare-wus2` (API App)

## Prerequisites

1. **Azure CLI** installed and logged in
2. **Bicep CLI** (installed with Azure CLI)
3. A resource group created for the deployment

## Deployment

### 1. Create a Resource Group

**Bash/Unix:**

```bash
az group create \
  --name rg-dev-lenkcare-westus2 \
  --location westus2
```

**PowerShell:**

```powershell
az group create `
  --name rg-dev-lenkcare-westus2 `
  --location westus2
```

### 2. Generate JWT Secret Key

The JWT secret key must be at least 32 characters. Generate a secure random key:

**Bash/Unix:**

```bash
# Generate a 64-character random key
openssl rand -base64 48
```

**PowerShell:**

```powershell
# Generate a 64-character random key
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

### 3. Set Environment Variables

**Bash/Unix:**

```bash
export SQL_ADMIN_LOGIN="lenkcare"
export SQL_ADMIN_PASSWORD="<your-secure-password>"
export JWT_SECRET_KEY="<paste-generated-key-here>"
export EMAIL_CONNECTION_STRING="<your-acs-connection-string>"
export EMAIL_SENDER_ADDRESS="DoNotReply@<your-domain>.azurecomm.net"
```

**PowerShell:**

```powershell
$env:SQL_ADMIN_LOGIN = "lenkcare"
$env:SQL_ADMIN_PASSWORD = "<your-secure-password>"
$env:JWT_SECRET_KEY = "<paste-generated-key-here>"
$env:EMAIL_CONNECTION_STRING = "<your-acs-connection-string>"
$env:EMAIL_SENDER_ADDRESS = "DoNotReply@<your-domain>.azurecomm.net"
```

### 4. Create Communication Services (Manual Step)

Azure Communication Services must be created manually via Azure Portal before deploying infrastructure:

1. Go to **Azure Portal** → **Create a resource** → **Communication Services**
2. Create a new Communication Service (e.g., `acs-dev-lenkcare-wus2`)
3. Go to the created resource → **Email** → **Provision domains**
4. Click **1-click add** to add an Azure subdomain (free)
5. Copy the **Connection String** from **Keys** blade
6. Copy the **Sender Address** from **Email** → **Domains** (e.g., `DoNotReply@***.unitedstates.communication.azure.com`)

Set these values as environment variables (see step 3).

### 5. Deploy the Infrastructure

**Bash/Unix:**

```bash
# Deploy to dev environment (with free tiers)
az deployment group create \
  --resource-group rg-dev-lenkcare-westus2 \
  --template-file main.bicep \
  --parameters main.bicepparam \
  --parameters environment=dev

# Deploy to staging (disable free tiers if already used)
az deployment group create \
  --resource-group rg-staging-lenkcare-westus2 \
  --template-file main.bicep \
  --parameters main.bicepparam \
  --parameters environment=staging useCosmosFreeTier=false useSqlFreeTier=false

# Deploy to prod
az deployment group create \
  --resource-group rg-prod-lenkcare-westus2 \
  --template-file main.bicep \
  --parameters main.bicepparam \
  --parameters environment=prod useCosmosFreeTier=false useSqlFreeTier=false
```

**PowerShell:**

```powershell
# Deploy to dev environment (with free tiers)
az deployment group create `
  --resource-group rg-dev-lenkcare-westus2 `
  --template-file main.bicep `
  --parameters main.bicepparam `
  --parameters environment=dev

# Deploy to staging (disable free tiers if already used)
az deployment group create `
  --resource-group rg-staging-lenkcare-westus2 `
  --template-file main.bicep `
  --parameters main.bicepparam `
  --parameters environment=staging useCosmosFreeTier=false useSqlFreeTier=false

# Deploy to prod
az deployment group create `
  --resource-group rg-prod-lenkcare-westus2 `
  --template-file main.bicep `
  --parameters main.bicepparam `
  --parameters environment=prod useCosmosFreeTier=false useSqlFreeTier=false
```

### 5. Alternative: Using Parameter Overrides Directly

```bash
az deployment group create \
  --resource-group rg-dev-lenkcare-westus2 \
  --template-file main.bicep \
  --parameters \
    environment=dev \
    sqlAdminLogin=lenkcare \
    sqlAdminPassword='<password>' \
    jwtSecretKey='<jwt-key>'
```

## Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `environment` | Yes | - | Environment: `dev`, `staging`, `prod` |
| `location` | No | Resource group location | Azure region |
| `projectName` | No | `lenkcare` | Project name for resource naming |
| `useCosmosFreeTier` | No | `true` | Use Cosmos DB free tier |
| `useSqlFreeTier` | No | `true` | Use Azure SQL free tier |
| `sqlAdminLogin` | Yes | - | SQL Server admin username |
| `sqlAdminPassword` | Yes | - | SQL Server admin password |
| `jwtSecretKey` | Yes | - | JWT signing key (min 32 chars) |
| `emailConnectionString` | Yes | - | Azure Communication Services connection string |
| `emailSenderAddress` | Yes | - | Email sender address (e.g., `DoNotReply@xxx.azurecomm.net`) |
| `frontendCustomDomain` | No | `<env>.homes.lenkcare.com` | Frontend custom domain |
| `apiCustomDomain` | No | `<env>.api.homes.lenkcare.com` | API custom domain |
| `enableCustomDomains` | No | `true` | Enable custom domains (set false if DNS not ready) |
| `appServiceSkuName` | No | `B2` (dev/staging), `B1` (prod) | App Service Plan SKU |

## Free Tier Limitations

> ⚠️ **Important**: Azure allows only ONE free tier per subscription for:
>
> - **Azure SQL**: Set `useSqlFreeTier=false` if you already have a free SQL database
> - **Cosmos DB**: Set `useCosmosFreeTier=false` if you already have a free Cosmos account

Recommended approach:

- Use free tiers for **dev** environment
- Set `false` for **staging** and **prod** environments

## Custom Domains Setup

The deployment supports custom domains with free managed SSL certificates. Custom domains are **enabled by default**.

### Option A: DNS Already Configured (Recommended)

If your DNS CNAME records are already set up, deploy normally:

```powershell
az deployment group create `
  --resource-group rg-dev-lenkcare-westus2 `
  --template-file main.bicep `
  --parameters main.bicepparam `
  --parameters environment=dev
```

### Option B: DNS Not Yet Configured

If DNS is not ready, deploy with custom domains disabled first:

**Step 1: Deploy without custom domains**

```powershell
az deployment group create `
  --resource-group rg-dev-lenkcare-westus2 `
  --template-file main.bicep `
  --parameters main.bicepparam `
  --parameters environment=dev enableCustomDomains=false
```

**Step 2: Configure DNS CNAME Records**

Add these CNAME records in your DNS provider (e.g., Cloudflare, GoDaddy, Namecheap):

| Type | Name | Target | TTL |
|------|------|--------|-----|
| CNAME | `dev.homes` | `app-dev-frontend-lenkcare-wus2.azurewebsites.net` | 3600 |
| CNAME | `dev.api.homes` | `app-dev-api-lenkcare-wus2.azurewebsites.net` | 3600 |
| CNAME | `staging.homes` | `app-staging-frontend-lenkcare-wus2.azurewebsites.net` | 3600 |
| CNAME | `staging.api.homes` | `app-staging-api-lenkcare-wus2.azurewebsites.net` | 3600 |
| CNAME | `prod.homes` | `app-prod-frontend-lenkcare-wus2.azurewebsites.net` | 3600 |
| CNAME | `prod.api.homes` | `app-prod-api-lenkcare-wus2.azurewebsites.net` | 3600 |

> **Note**: Replace `wus2` with your actual location abbreviation. Get the exact hostnames from deployment outputs.

### Step 3: Verify DNS Propagation

Wait for DNS propagation (usually 5-30 minutes), then verify:

**Bash/Unix:**

```bash
nslookup dev.homes.lenkcare.com
nslookup dev.api.homes.lenkcare.com
```

**PowerShell:**

```powershell
Resolve-DnsName dev.homes.lenkcare.com
Resolve-DnsName dev.api.homes.lenkcare.com
```

### Step 4: Enable Custom Domains with SSL

Redeploy without the override (or explicitly set `enableCustomDomains=true`):

```powershell
az deployment group create `
  --resource-group rg-dev-lenkcare-westus2 `
  --template-file main.bicep `
  --parameters main.bicepparam `
  --parameters environment=dev
```

This will:
- Bind custom domains to App Services
- Provision free managed SSL certificates
- Enable SNI-based HTTPS

### Custom Domain Summary

| Environment | Frontend URL | API URL |
|-------------|-------------|--------|
| dev | `https://dev.homes.lenkcare.com` | `https://dev.api.homes.lenkcare.com` |
| staging | `https://staging.homes.lenkcare.com` | `https://staging.api.homes.lenkcare.com` |
| prod | `https://prod.homes.lenkcare.com` | `https://prod.api.homes.lenkcare.com` |

## Email Setup

Azure Communication Services is **not deployed via Bicep** due to API compatibility issues. Create it manually:

1. **Create Communication Service** in Azure Portal
2. **Add email domain** via the Email blade (1-click Azure subdomain)
3. **Get connection string** from Keys blade
4. **Get sender address** from Email → Domains
5. **Set environment variables** before deploying:

```powershell
$env:EMAIL_CONNECTION_STRING = "endpoint=https://...;accesskey=..."
$env:EMAIL_SENDER_ADDRESS = "DoNotReply@xxx.unitedstates.communication.azure.com"
```

The deployment stores these values in Key Vault for the application to use.

## Key Vault Secrets

The deployment creates these secrets in Key Vault:

### Connection Strings

| Secret Name | Description | Source |
|-------------|-------------|--------|
| `ConnectionStrings--SqlDatabase` | SQL Server connection string | SQL module |
| `CosmosDb--ConnectionString` | Cosmos DB connection string | Cosmos module |
| `BlobStorage--ConnectionString` | Storage account connection string | Storage module |
| `Email--ConnectionString` | Azure Communication Services connection | Communication module |

### Cosmos DB Settings

| Secret Name | Description | Default |
|-------------|-------------|---------|
| `CosmosDb--DatabaseName` | Database name | `LenkCareHomes` |
| `CosmosDb--ContainerName` | Container for audit logs | `AuditLogs` |

### Blob Storage Settings

| Secret Name | Description | Default |
|-------------|-------------|---------|
| `BlobStorage--ContainerName` | Container for documents | `documents` |
| `BlobStorage--MaxFileSizeBytes` | Max upload size | `52428800` (50MB) |

### Email Settings

| Secret Name | Description | Source |
|-------------|-------------|--------|
| `Email--SenderAddress` | Sender email address | Communication module |

### Auth Settings

| Secret Name | Description | Default/Source |
|-------------|-------------|----------------|
| `Auth--FrontendBaseUrl` | Frontend URL for CORS/redirects | From `frontendCustomDomain` |
| `Auth--JwtSecret` | JWT signing key | Parameter `jwtSecretKey` |
| `Auth--TokenExpirationMinutes` | Token validity | `480` (8 hours) |
| `Auth--InvitationExpirationHours` | Invitation link validity | `48` hours |

### FIDO2/WebAuthn Settings

| Secret Name | Description | Default/Source |
|-------------|-------------|----------------|
| `Fido2--ServerDomain` | WebAuthn domain | From `frontendCustomDomain` |
| `Fido2--ServerName` | Display name | `LenkCare Homes` |
| `Fido2--Origin` | Allowed origin | `https://<frontendCustomDomain>` |
| `Fido2--Origins` | Allowed origins array | `["https://<frontendCustomDomain>"]` |

## Module Structure

```text
infra/
├── main.bicep                          # Main orchestration
├── main.bicepparam                     # Parameters file
├── README.md                           # This file
└── modules/
    ├── key-vault.bicep                 # Key Vault resource
    ├── key-vault-secrets.bicep         # All application secrets
    ├── key-vault-access.bicep          # RBAC for API app managed identity
    ├── storage.bicep                   # Storage account with CORS
    ├── sql-server.bicep                # SQL Server and database
    ├── cosmos-db.bicep                 # Cosmos DB account
    ├── app-service.bicep               # App Service Plan and web apps
    └── azure-maps.bicep                # Azure Maps for location services
```

## Security Features

- ✅ **TLS 1.2** enforced on all resources
- ✅ **RBAC** for Key Vault access (no access policies)
- ✅ **Managed Identity** for API app to access Key Vault secrets
- ✅ **CORS** configured for Storage blob service and API App Service
- ✅ **HTTPS Only** for all web apps
- ✅ **FTP Disabled** for deployments
- ✅ **Soft Delete** enabled for Key Vault (90 days retention)
- ✅ **TDE** enabled for SQL Database
- ✅ **Blob soft delete** enabled (7 days retention)

## Outputs

After deployment, these values are output:

| Output | Description |
|--------|-------------|
| `keyVaultUri` | Key Vault URI for app configuration |
| `apiAppDefaultHostname` | API app hostname (`.azurewebsites.net`) |
| `frontendAppDefaultHostname` | Frontend app hostname |
| `sqlServerFqdn` | SQL Server fully qualified domain name |
| `cosmosEndpoint` | Cosmos DB endpoint |
| `storageBlobEndpoint` | Storage blob endpoint |

## Post-Deployment Steps

1. **Configure custom domains** with SSL certificates in Azure Portal
2. **Deploy application code** via GitHub Actions or Azure DevOps
3. **Run database migrations** against the SQL database
4. **Verify Key Vault access** - API app should read secrets automatically

## Cleanup / Delete Resources

To completely remove all resources including soft-deleted Key Vaults:

**Bash/Unix:**

```bash
# Set variables
RESOURCE_GROUP="rg-dev-lenkcare-westus2"
KEY_VAULT_NAME="kv-dev-lenkcare-wus2"

# Delete the resource group (this deletes all resources)
az group delete --name $RESOURCE_GROUP --yes --no-wait

# Purge soft-deleted Key Vault (required to reuse the same name)
az keyvault purge --name $KEY_VAULT_NAME --no-wait
```

**PowerShell:**

```powershell
# Set variables
$ResourceGroup = "rg-dev-lenkcare-westus2"
$KeyVaultName = "kv-dev-lenkcare-wus2"

# Delete the resource group (this deletes all resources)
az group delete --name $ResourceGroup --yes --no-wait

# Purge soft-deleted Key Vault (required to reuse the same name)
az keyvault purge --name $KeyVaultName --no-wait
```

> ⚠️ **Warning**: This permanently deletes all data. The `--no-wait` flag runs deletion in the background. Remove it to wait for completion.
