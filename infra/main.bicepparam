// ============================================================================
// LenkCare Homes - Parameters File
// ============================================================================
// Usage (Bash/Unix):
//   az deployment group create \
//     --resource-group rg-<env>-lenkcare-<location> \
//     --template-file main.bicep \
//     --parameters main.bicepparam \
//     --parameters environment=dev
//
// Usage (PowerShell):
//   az deployment group create `
//     --resource-group rg-<env>-lenkcare-<location> `
//     --template-file main.bicep `
//     --parameters main.bicepparam `
//     --parameters environment=dev
//
// Environment variables required:
//   $env:SQL_ADMIN_PASSWORD = "<password>"    # PowerShell
//   $env:JWT_SECRET_KEY = "<key>"             # PowerShell
//   export SQL_ADMIN_PASSWORD="<password>"    # Bash
//   export JWT_SECRET_KEY="<key>"             # Bash
// ============================================================================

using 'main.bicep'

// Required: Set the environment (dev, staging, prod)
param environment = 'dev'

// Optional: Override location (defaults to resource group location)
// param location = 'westus2'

// Optional: Override project name
// param projectName = 'lenkcare'

// Free tier options (only one per subscription)
// Set to false if you already have a free tier resource
param useCosmosFreeTier = true
param useSqlFreeTier = true

// SQL Admin credentials (use Key Vault or pipeline secrets in production)
// These should be provided via --parameters or environment variables
param sqlAdminLogin = readEnvironmentVariable('SQL_ADMIN_LOGIN', 'lenkcare')
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')

// JWT Secret Key for authentication (required)
param jwtSecretKey = readEnvironmentVariable('JWT_SECRET_KEY', '')

// Email settings (create Communication Services manually in Azure Portal first)
// Get connection string from: Azure Portal → Communication Services → Keys
// Get sender address from: Azure Portal → Communication Services → Email → Domains
param emailConnectionString = readEnvironmentVariable('EMAIL_CONNECTION_STRING', '')
param emailSenderAddress = readEnvironmentVariable('EMAIL_SENDER_ADDRESS', '')

// Optional: Custom domain for frontend (defaults based on environment)
// param frontendCustomDomain = 'staging.homes.lenkcare.com'
// Note: API custom domain follows pattern: <env>.api.homes.lenkcare.com

// Optional: Custom email domain (default: lenkcare.com)
// param customEmailDomain = 'lenkcare.com'

// Custom domains are enabled by default
// Set to false for initial deployment if DNS CNAME records are not yet configured
// Step 1: If DNS not ready, deploy with enableCustomDomains = false
// Step 2: Add CNAME records in your DNS provider (see README)
// Step 3: Redeploy with enableCustomDomains = true (or remove override)
param enableCustomDomains = true

// Optional: App Service SKU (auto-selected based on environment)
// param appServiceSkuName = 'P1v3'
