// ============================================================================
// Storage Account Module
// ============================================================================
// Creates a Storage Account with blob container and CORS configuration
// ============================================================================

@description('Name of the Storage Account (must be globally unique, 3-24 chars, lowercase alphanumeric)')
param storageAccountName string

@description('Azure region for deployment')
param location string

@description('Resource tags')
param tags object = {}

@description('Allowed origins for CORS (frontend URLs)')
param frontendOrigins array = []

@description('Name of the blob container for documents')
param containerName string = 'documents'

@description('Name of the blob container for incident photos')
param incidentPhotosContainerName string = 'incident-photos'

// ============================================================================
// Resources
// ============================================================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  properties: {
    dnsEndpointType: 'Standard'
    defaultToOAuthAuthentication: false
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    largeFileSharesState: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
    encryption: {
      requireInfrastructureEncryption: false
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    deleteRetentionPolicy: {
      allowPermanentDelete: false
      enabled: true
      days: 7
    }
    cors: {
      corsRules: [
        for origin in frontendOrigins: {
          allowedOrigins: [origin]
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          maxAgeInSeconds: 3600
          exposedHeaders: ['*']
          allowedHeaders: ['*']
        }
      ]
    }
  }
}

resource documentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: containerName
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

resource incidentPhotosContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: incidentPhotosContainerName
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
    publicAccess: 'None'
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The Storage Account resource ID')
output storageAccountId string = storageAccount.id

@description('The Storage Account name')
output storageAccountName string = storageAccount.name

@description('The Storage Account blob endpoint')
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob

#disable-next-line outputs-should-not-contain-secrets
@description('The Storage Account connection string')
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'

@description('The blob container name')
output containerName string = containerName
