// ============================================================================
// Azure Maps Module
// ============================================================================
// Creates Azure Maps account for location services
// ============================================================================

@description('Name of the Azure Maps account')
param mapsAccountName string

@description('Azure region for deployment (ignored - Azure Maps only supports global)')
param location string

@description('Resource tags')
param tags object = {}

@description('SKU for Azure Maps (G2 for Gen2 features)')
@allowed(['G2', 'S0', 'S1'])
param skuName string = 'G2'

// ============================================================================
// Resources
// ============================================================================

resource mapsAccount 'Microsoft.Maps/accounts@2024-01-01-preview' = {
  name: mapsAccountName
  location: 'global' // Azure Maps only supports global location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'Gen2'
  properties: {
    disableLocalAuth: false
    cors: {
      corsRules: [
        {
          allowedOrigins: ['*']
        }
      ]
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The Azure Maps account resource ID')
output mapsAccountId string = mapsAccount.id

@description('The Azure Maps account name')
output mapsAccountName string = mapsAccount.name

#disable-next-line outputs-should-not-contain-secrets
@description('The Azure Maps primary key')
output primaryKey string = mapsAccount.listKeys().primaryKey
