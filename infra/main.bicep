// ============================================================================
// LenkCare Homes - Main Infrastructure Deployment
// ============================================================================
// Naming convention: <resource abbr>-<environment>-<project>-<location>
// ============================================================================

targetScope = 'resourceGroup'

// ============================================================================
// Parameters
// ============================================================================

@description('The environment to deploy to (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('The Azure region for deployment')
param location string = resourceGroup().location

@description('The project name used in resource naming')
param projectName string = 'lenkcare'

@description('Use free tier for Cosmos DB (only one free tier allowed per subscription)')
param useCosmosFreeTier bool = true

@description('Use free tier for Azure SQL (only one free tier allowed per subscription)')
param useSqlFreeTier bool = true

@description('SQL Server administrator login')
@secure()
param sqlAdminLogin string

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('JWT Secret Key for authentication')
@secure()
param jwtSecretKey string

@description('Azure Communication Services connection string (create manually in Azure Portal)')
@secure()
param emailConnectionString string

@description('Email sender address (e.g., DoNotReply@***.azurecomm.net)')
param emailSenderAddress string

@description('Custom domain for frontend (e.g., dev.homes.lenkcare.com or homes.lenkcare.com for prod)')
param frontendCustomDomain string = environment == 'prod' ? 'homes.lenkcare.com' : '${environment}.homes.lenkcare.com'

@description('Custom domain for API (e.g., dev.api.homes.lenkcare.com or api.homes.lenkcare.com for prod)')
param apiCustomDomain string = environment == 'prod' ? 'api.homes.lenkcare.com' : '${environment}.api.homes.lenkcare.com'

@description('Enable custom domain binding for App Services (set to false for initial deployment if DNS is not configured)')
param enableCustomDomains bool = true

@description('App Service Plan SKU for dev/staging/prod environments')
param appServiceSkuName string = environment == 'prod' ? 'B1' : 'B2'

// ============================================================================
// Variables
// ============================================================================

// Location abbreviation mapping
var locationAbbreviations = {
  eastus: 'eus'
  eastus2: 'eus2'
  westus: 'wus'
  westus2: 'wus2'
  westus3: 'wus3'
  westcentralus: 'wcus'
  centralus: 'cus'
  northcentralus: 'ncus'
  southcentralus: 'scus'
  northeurope: 'neu'
  westeurope: 'weu'
  uksouth: 'uks'
  ukwest: 'ukw'
}

var locationAbbr = locationAbbreviations[?location] ?? replace(location, ' ', '')

// Resource naming - follows pattern: <abbr>-<env>-<project>-<location>
var keyVaultName = 'kv-${environment}-${projectName}-${locationAbbr}'
var storageAccountName = 'st${environment}${projectName}${locationAbbr}'
var sqlServerName = 'sql-${environment}-${projectName}-${locationAbbr}'
var sqlDatabaseName = 'sqldb-${environment}-${projectName}-${locationAbbr}'
var cosmosAccountName = 'cosmos-${environment}-${projectName}-${locationAbbr}'
var appServicePlanName = 'asp-${environment}-${projectName}-${locationAbbr}'
var apiAppName = 'app-${environment}-api-${projectName}-${locationAbbr}'
var frontendAppName = 'app-${environment}-frontend-${projectName}-${locationAbbr}'
var mapsAccountName = 'map-${environment}-${projectName}-${locationAbbr}'

// Key Vault URI can be computed without creating the resource first
var keyVaultUri = 'https://${keyVaultName}${az.environment().suffixes.keyvaultDns}'

// Common tags
var commonTags = {
  Environment: environment
  Project: 'LenkCare Homes'
  ManagedBy: 'Bicep'
}

// ============================================================================
// Modules - Deployment Order:
// 1. Storage, SQL, Maps, App Service (parallel)
// 2. Cosmos DB (after step 1)
// 3. Key Vault (after Cosmos DB)
// 4. Key Vault Secrets and Access (last)
// Note: Communication Services (Email) is created manually via Azure Portal
// ============================================================================

// Storage Account
module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    storageAccountName: storageAccountName
    location: location
    tags: commonTags
    frontendOrigins: environment == 'dev' ? [
      'https://${frontendCustomDomain}'
      'http://localhost:3000'
    ] : [
      'https://${frontendCustomDomain}'
    ]
  }
}

// Azure SQL Server and Database
module sql 'modules/sql-server.bicep' = {
  name: 'sql'
  params: {
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    location: location
    tags: commonTags
    administratorLogin: sqlAdminLogin
    administratorPassword: sqlAdminPassword
    useFreeTier: useSqlFreeTier
    environment: environment
  }
}

// Azure Maps
module maps 'modules/azure-maps.bicep' = {
  name: 'maps'
  params: {
    mapsAccountName: mapsAccountName
    location: location
    tags: commonTags
  }
}

// App Service Plan and Web Apps (uses computed keyVaultUri, no dependency on Key Vault resource)
module appService 'modules/app-service.bicep' = {
  name: 'appService'
  params: {
    appServicePlanName: appServicePlanName
    apiAppName: apiAppName
    frontendAppName: frontendAppName
    location: location
    tags: commonTags
    skuName: appServiceSkuName
    keyVaultUri: keyVaultUri
    frontendOrigin: 'https://${frontendCustomDomain}'
    apiOrigin: 'https://${apiCustomDomain}'
    azureMapsKey: maps.outputs.primaryKey
    frontendCustomDomain: frontendCustomDomain
    apiCustomDomain: apiCustomDomain
    enableCustomDomains: enableCustomDomains
  }
}

// Cosmos DB Account - deploys after Storage, SQL, Communication, Maps, App Service
module cosmos 'modules/cosmos-db.bicep' = {
  name: 'cosmos'
  params: {
    cosmosAccountName: cosmosAccountName
    location: location
    tags: commonTags
    useFreeTier: useCosmosFreeTier
    environment: environment
  }
  dependsOn: [
    storage
    sql
    maps
    appService
  ]
}

// Key Vault - deploys after Cosmos DB
module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  params: {
    keyVaultName: keyVaultName
    location: location
    tags: commonTags
  }
  dependsOn: [
    cosmos
  ]
}

// Key Vault Secrets - deployed after Key Vault and all resources that provide secrets
module keyVaultSecrets 'modules/key-vault-secrets.bicep' = {
  name: 'keyVaultSecrets'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    sqlConnectionString: sql.outputs.connectionString
    cosmosConnectionString: cosmos.outputs.connectionString
    cosmosDatabaseName: cosmos.outputs.databaseName
    cosmosContainerName: cosmos.outputs.containerName
    storageConnectionString: storage.outputs.connectionString
    storageContainerName: storage.outputs.containerName
    emailConnectionString: emailConnectionString
    emailSenderAddress: emailSenderAddress
    frontendBaseUrl: 'https://${frontendCustomDomain}'
    jwtSecretKey: jwtSecretKey
    fido2ServerDomain: frontendCustomDomain
    fido2Origins: ['https://${frontendCustomDomain}']
  }
}

// Key Vault Access Policy for API App - deployed last
module keyVaultAccess 'modules/key-vault-access.bicep' = {
  name: 'keyVaultAccess'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    apiAppPrincipalId: appService.outputs.apiAppPrincipalId
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The Key Vault URI for configuration')
output keyVaultUri string = keyVault.outputs.keyVaultUri

@description('The API App default hostname')
output apiAppDefaultHostname string = appService.outputs.apiAppDefaultHostname

@description('The Frontend App default hostname')
output frontendAppDefaultHostname string = appService.outputs.frontendAppDefaultHostname

@description('The SQL Server fully qualified domain name')
output sqlServerFqdn string = sql.outputs.serverFqdn

@description('The Cosmos DB endpoint')
output cosmosEndpoint string = cosmos.outputs.endpoint

@description('The Storage Account blob endpoint')
output storageBlobEndpoint string = storage.outputs.blobEndpoint
