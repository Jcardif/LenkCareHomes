// ============================================================================
// Key Vault Secrets Module
// ============================================================================
// Stores all application secrets in Key Vault
// Secret names use double-dash (--) format for ASP.NET Core configuration
// ============================================================================

@description('Name of the Key Vault')
param keyVaultName string

@description('SQL Database connection string')
@secure()
param sqlConnectionString string

@description('Cosmos DB connection string')
@secure()
param cosmosConnectionString string

@description('Cosmos DB database name')
param cosmosDatabaseName string

@description('Cosmos DB container name')
param cosmosContainerName string

@description('Storage Account connection string')
@secure()
param storageConnectionString string

@description('Storage container name for documents')
param storageContainerName string

@description('Email service connection string')
@secure()
param emailConnectionString string

@description('Email sender address')
param emailSenderAddress string

@description('Frontend base URL')
param frontendBaseUrl string

@description('JWT Secret Key')
@secure()
param jwtSecretKey string

@description('Auth token expiration in minutes')
param tokenExpirationMinutes int = 480

@description('Invitation link expiration in hours')
param invitationExpirationHours int = 48

@description('Max file size for uploads in bytes (default 50MB)')
param maxFileSizeBytes int = 52428800

@description('Fido2/WebAuthn server domain')
param fido2ServerDomain string

@description('Fido2/WebAuthn server name')
param fido2ServerName string = 'LenkCare Homes'

@description('Fido2/WebAuthn allowed origins')
param fido2Origins array

// ============================================================================
// Resources
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Connection Strings
resource secretSqlConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--SqlDatabase'
  properties: {
    value: sqlConnectionString
  }
}

// Cosmos DB Settings
resource secretCosmosConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'CosmosDb--ConnectionString'
  properties: {
    value: cosmosConnectionString
  }
}

resource secretCosmosDatabaseName 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'CosmosDb--DatabaseName'
  properties: {
    value: cosmosDatabaseName
  }
}

resource secretCosmosContainerName 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'CosmosDb--ContainerName'
  properties: {
    value: cosmosContainerName
  }
}

// Blob Storage Settings
resource secretBlobConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'BlobStorage--ConnectionString'
  properties: {
    value: storageConnectionString
  }
}

resource secretBlobContainerName 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'BlobStorage--ContainerName'
  properties: {
    value: storageContainerName
  }
}

resource secretMaxFileSize 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'BlobStorage--MaxFileSizeBytes'
  properties: {
    value: string(maxFileSizeBytes)
  }
}

// Email Settings
resource secretEmailConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Email--ConnectionString'
  properties: {
    value: emailConnectionString
  }
}

resource secretEmailSenderAddress 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Email--SenderAddress'
  properties: {
    value: emailSenderAddress
  }
}

// Auth Settings
resource secretFrontendBaseUrl 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Auth--FrontendBaseUrl'
  properties: {
    value: frontendBaseUrl
  }
}

resource secretJwtSecretKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Auth--JwtSecret'
  properties: {
    value: jwtSecretKey
  }
}

resource secretTokenExpiration 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Auth--TokenExpirationMinutes'
  properties: {
    value: string(tokenExpirationMinutes)
  }
}

resource secretInvitationExpiration 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Auth--InvitationExpirationHours'
  properties: {
    value: string(invitationExpirationHours)
  }
}


// Fido2/WebAuthn Settings
resource secretFido2ServerDomain 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Fido2--ServerDomain'
  properties: {
    value: fido2ServerDomain
  }
}

resource secretFido2ServerName 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Fido2--ServerName'
  properties: {
    value: fido2ServerName
  }
}

resource secretFido2Origin 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Fido2--Origin'
  properties: {
    value: frontendBaseUrl
  }
}

// Store origins as indexed secrets for ASP.NET Core array configuration binding
// Key Vault doesn't support JSON arrays, so we use Fido2--Origins--0, Fido2--Origins--1, etc.
resource secretFido2Origins 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [for (origin, i) in fido2Origins: {
  parent: keyVault
  name: 'Fido2--Origins--${i}'
  properties: {
    value: origin
  }
}]
