// ============================================================================
// Azure Cosmos DB Module
// ============================================================================
// Creates Cosmos DB account with database and container for audit logging
// Supports free tier option
// ============================================================================

@description('Name of the Cosmos DB account')
param cosmosAccountName string

@description('Azure region for deployment')
param location string

@description('Resource tags')
param tags object = {}

@description('Use free tier (one per subscription)')
param useFreeTier bool = true

@description('Environment (affects throughput limits)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Database name')
param databaseName string = 'LenkCareHomes'

@description('Container name for audit logs')
param containerName string = 'AuditLogs'

// ============================================================================
// Variables
// ============================================================================

// Throughput limits based on environment
var totalThroughputLimit = useFreeTier ? 1000 : (environment == 'prod' ? 10000 : 4000)

// Backup policy based on environment
var backupPolicy = environment == 'prod' ? {
  type: 'Continuous'
  continuousModeProperties: {
    tier: 'Continuous7Days'
  }
} : {
  type: 'Periodic'
  periodicModeProperties: {
    backupIntervalInMinutes: 240
    backupRetentionIntervalInHours: 8
    backupStorageRedundancy: 'Local'
  }
}

// ============================================================================
// Resources
// ============================================================================

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosAccountName
  location: location
  tags: union(tags, {
    defaultExperience: 'Core (SQL)'
    'hidden-workload-type': environment == 'prod' ? 'Production' : 'Development/Testing'
  })
  kind: 'GlobalDocumentDB'
  properties: {
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: true
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    disableKeyBasedMetadataWriteAccess: false
    enableFreeTier: useFreeTier
    enableAnalyticalStorage: false
    analyticalStorageConfiguration: {
      schemaType: 'WellDefined'
    }
    databaseAccountOfferType: 'Standard'
    defaultIdentity: 'FirstPartyIdentity'
    networkAclBypass: 'None'
    disableLocalAuth: false
    enablePartitionMerge: false
    enableBurstCapacity: false
    minimalTlsVersion: 'Tls12'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    cors: []
    capabilities: []
    ipRules: []
    backupPolicy: backupPolicy
    capacity: {
      totalThroughputLimit: totalThroughputLimit
    }
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: database
  name: containerName
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: ['/partitionKey']
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      defaultTtl: -1 // No TTL for audit logs (HIPAA requires 6-year retention)
    }
    options: {
      throughput: useFreeTier ? 400 : 1000
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The Cosmos DB account resource ID')
output accountId string = cosmosAccount.id

@description('The Cosmos DB account name')
output accountName string = cosmosAccount.name

@description('The Cosmos DB endpoint')
output endpoint string = cosmosAccount.properties.documentEndpoint

#disable-next-line outputs-should-not-contain-secrets
@description('The Cosmos DB connection string')
output connectionString string = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString

@description('The database name')
output databaseName string = databaseName

@description('The container name')
output containerName string = containerName
