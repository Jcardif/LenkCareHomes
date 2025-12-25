// ============================================================================
// Azure SQL Server and Database Module
// ============================================================================
// Creates SQL Server with database, supporting free tier option
// ============================================================================

@description('Name of the SQL Server')
param sqlServerName string

@description('Name of the SQL Database')
param sqlDatabaseName string

@description('Azure region for deployment')
param location string

@description('Resource tags')
param tags object = {}

@description('SQL Server administrator login')
@secure()
param administratorLogin string

@description('SQL Server administrator password')
@secure()
param administratorPassword string

@description('Use free tier (one per subscription)')
param useFreeTier bool = true

@description('Environment (affects SKU selection for non-free tier)')
@allowed(['dev', 'staging', 'prod'])
param environment string

// ============================================================================
// Variables
// ============================================================================

// SKU selection based on free tier and environment
var databaseSku = useFreeTier ? {
  name: 'GP_S_Gen5'
  tier: 'GeneralPurpose'
  family: 'Gen5'
  capacity: 2
} : (environment == 'prod' ? {
  name: 'Basic'
  tier: 'Basic'
} : {
  name: 'Basic'
  tier: 'Basic'
})

// ============================================================================
// Resources
// ============================================================================

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: databaseSku
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: useFreeTier ? 34359738368 : 2147483648 // 32GB for free tier, 2GB for Basic tier
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    autoPauseDelay: useFreeTier ? 60 : -1 // Auto-pause after 60 min for free tier
    requestedBackupStorageRedundancy: 'Local'
    minCapacity: useFreeTier ? json('0.5') : null
    isLedgerOn: false
    useFreeLimit: useFreeTier
    freeLimitExhaustionBehavior: useFreeTier ? 'AutoPause' : null
    availabilityZone: 'NoPreference'
  }
}

// Enable Transparent Data Encryption
resource tde 'Microsoft.Sql/servers/databases/transparentDataEncryption@2023-08-01-preview' = {
  parent: sqlDatabase
  name: 'current'
  properties: {
    state: 'Enabled'
  }
}

// Allow Azure services firewall rule
resource firewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The SQL Server resource ID')
output serverId string = sqlServer.id

@description('The SQL Server name')
output serverName string = sqlServer.name

@description('The SQL Server fully qualified domain name')
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The SQL Database name')
output databaseName string = sqlDatabase.name

@description('The SQL Database connection string')
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${administratorLogin};Password=${administratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
