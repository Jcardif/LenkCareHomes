// ============================================================================
// App Service Module
// ============================================================================
// Creates App Service Plan with API and Frontend Web Apps
// Includes system-assigned managed identity for API, CORS, and custom domains
// ============================================================================

@description('Name of the App Service Plan')
param appServicePlanName string

@description('Name of the API App Service')
param apiAppName string

@description('Name of the Frontend App Service')
param frontendAppName string

@description('Azure region for deployment')
param location string

@description('Resource tags')
param tags object = {}

@description('App Service Plan SKU name (e.g., B1, B2, S1, P1v3)')
param skuName string = 'B2'

@description('Key Vault URI for configuration')
param keyVaultUri string

@description('Frontend origin for CORS')
param frontendOrigin string

@description('API origin URL for frontend')
param apiOrigin string

@description('Azure Maps subscription key')
@secure()
param azureMapsKey string

@description('Custom domain for frontend (e.g., dev.homes.lenkcare.com)')
param frontendCustomDomain string

@description('Custom domain for API (e.g., dev.api.homes.lenkcare.com)')
param apiCustomDomain string

@description('Enable custom domain binding (set to false for initial deployment if DNS is not configured)')
param enableCustomDomains bool = true

// ============================================================================
// Variables
// ============================================================================

var skuTier = contains(skuName, 'P') ? 'PremiumV3' : (contains(skuName, 'S') ? 'Standard' : 'Basic')

// ============================================================================
// Resources
// ============================================================================

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
    perSiteScaling: false
    elasticScaleEnabled: false
    maximumElasticWorkerCount: 1
    isSpot: false
    isXenon: false
    hyperV: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
    zoneRedundant: false
  }
}

// API App Service (.NET 10)
resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    enabled: true
    serverFarmId: appServicePlan.id
    reserved: true
    isXenon: false
    hyperV: false
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'DOTNETCORE|10.0'
      acrUseManagedIdentityCreds: false
      alwaysOn: skuTier != 'Basic' // AlwaysOn requires Standard or higher
      http20Enabled: true
      functionAppScaleLimit: 0
      minimumElasticInstanceCount: 0
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
      cors: {
        allowedOrigins: [frontendOrigin]
        supportCredentials: true
      }
      appSettings: [
        {
          name: 'KeyVault__Uri'
          value: keyVaultUri
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
    clientAffinityEnabled: false
    clientCertEnabled: false
    clientCertMode: 'Required'
    hostNamesDisabled: false
    httpsOnly: true
    redundancyMode: 'None'
    publicNetworkAccess: 'Enabled'
    storageAccountRequired: false
    keyVaultReferenceIdentity: 'SystemAssigned'
  }
}

// API App Web Config
resource apiAppConfig 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: apiApp
  name: 'web'
  properties: {
    numberOfWorkers: 1
    linuxFxVersion: 'DOTNETCORE|10.0'
    requestTracingEnabled: false
    remoteDebuggingEnabled: false
    httpLoggingEnabled: true
    acrUseManagedIdentityCreds: false
    logsDirectorySizeLimit: 35
    detailedErrorLoggingEnabled: false
    use32BitWorkerProcess: false
    webSocketsEnabled: false
    alwaysOn: skuTier != 'Basic'
    managedPipelineMode: 'Integrated'
    loadBalancing: 'LeastRequests'
    autoHealEnabled: false
    vnetRouteAllEnabled: false
    publicNetworkAccess: 'Enabled'
    cors: {
      allowedOrigins: [frontendOrigin]
      supportCredentials: true
    }
    localMySqlEnabled: false
    http20Enabled: true
    minTlsVersion: '1.2'
    scmMinTlsVersion: '1.2'
    ftpsState: 'FtpsOnly'
  }
}

// Frontend App Service (Node.js)
resource frontendApp 'Microsoft.Web/sites@2023-12-01' = {
  name: frontendAppName
  location: location
  tags: tags
  kind: 'app,linux'
  properties: {
    enabled: true
    serverFarmId: appServicePlan.id
    reserved: true
    isXenon: false
    hyperV: false
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'NODE|22-lts'
      acrUseManagedIdentityCreds: false
      alwaysOn: skuTier != 'Basic'
      http20Enabled: true
      functionAppScaleLimit: 0
      minimumElasticInstanceCount: 0
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
      appCommandLine: 'node server.js'
      appSettings: [
        {
          name: 'NEXT_PUBLIC_API_URL'
          value: '${apiOrigin}/api'
        }
        {
          name: 'NEXT_PUBLIC_AZURE_MAPS_KEY'
          value: azureMapsKey
        }
        {
          name: 'NODE_ENV'
          value: 'production'
        }
      ]
    }
    clientAffinityEnabled: false
    clientCertEnabled: false
    clientCertMode: 'Required'
    hostNamesDisabled: false
    httpsOnly: true
    redundancyMode: 'None'
    publicNetworkAccess: 'Enabled'
    storageAccountRequired: false
  }
}

// Frontend App Web Config
resource frontendAppConfig 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: frontendApp
  name: 'web'
  properties: {
    numberOfWorkers: 1
    linuxFxVersion: 'NODE|22-lts'
    requestTracingEnabled: false
    remoteDebuggingEnabled: false
    httpLoggingEnabled: true
    acrUseManagedIdentityCreds: false
    logsDirectorySizeLimit: 35
    detailedErrorLoggingEnabled: false
    use32BitWorkerProcess: false
    webSocketsEnabled: false
    alwaysOn: skuTier != 'Basic'
    managedPipelineMode: 'Integrated'
    loadBalancing: 'LeastRequests'
    autoHealEnabled: false
    vnetRouteAllEnabled: false
    publicNetworkAccess: 'Enabled'
    localMySqlEnabled: false
    http20Enabled: true
    minTlsVersion: '1.2'
    scmMinTlsVersion: '1.2'
    ftpsState: 'FtpsOnly'
    appCommandLine: 'node server.js'
  }
}

// Disable FTP publishing for API
resource apiAppFtp 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: apiApp
  name: 'ftp'
  properties: {
    allow: false
  }
}

// Disable FTP publishing for Frontend
resource frontendAppFtp 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: frontendApp
  name: 'ftp'
  properties: {
    allow: false
  }
}

// ============================================================================
// Custom Domain Bindings with Managed SSL Certificates
// ============================================================================
// NOTE: Custom domains require two-step deployment:
// 1. First deploy with enableCustomDomains=false
// 2. Configure DNS CNAME records pointing to the default hostnames
// 3. Then deploy with enableCustomDomains=true
//
// The managed certificate requires the hostname binding to exist first,
// but the binding can be created with SSL in a single resource if the
// certificate already exists. For initial deployment, we use sslState='Disabled'
// and a separate deployment step is needed to enable SSL.
//
// For simplicity, this template creates the binding WITHOUT SSL initially.
// After the certificate is provisioned, run the deployment again to apply SSL.

// API Custom Domain Hostname Binding (initial binding, SSL added on subsequent deployment)
resource apiCustomDomainBinding 'Microsoft.Web/sites/hostNameBindings@2023-12-01' = if (enableCustomDomains) {
  parent: apiApp
  name: apiCustomDomain
  properties: {
    siteName: apiApp.name
    hostNameType: 'Verified'
    sslState: 'Disabled'
  }
}

// Frontend Custom Domain Hostname Binding (initial binding, SSL added on subsequent deployment)
resource frontendCustomDomainBinding 'Microsoft.Web/sites/hostNameBindings@2023-12-01' = if (enableCustomDomains) {
  parent: frontendApp
  name: frontendCustomDomain
  properties: {
    siteName: frontendApp.name
    hostNameType: 'Verified'
    sslState: 'Disabled'
  }
}

// API Managed SSL Certificate (Free) - created after binding exists
resource apiManagedCertificate 'Microsoft.Web/certificates@2023-12-01' = if (enableCustomDomains) {
  name: 'cert-${apiAppName}-${replace(apiCustomDomain, '.', '-')}'
  location: location
  tags: tags
  properties: {
    serverFarmId: appServicePlan.id
    canonicalName: apiCustomDomain
  }
  dependsOn: [
    apiCustomDomainBinding
  ]
}

// Frontend Managed SSL Certificate (Free) - created after binding exists
resource frontendManagedCertificate 'Microsoft.Web/certificates@2023-12-01' = if (enableCustomDomains) {
  name: 'cert-${frontendAppName}-${replace(frontendCustomDomain, '.', '-')}'
  location: location
  tags: tags
  properties: {
    serverFarmId: appServicePlan.id
    canonicalName: frontendCustomDomain
  }
  dependsOn: [
    frontendCustomDomainBinding
  ]
}

// ============================================================================
// Outputs
// ============================================================================

@description('The App Service Plan resource ID')
output appServicePlanId string = appServicePlan.id

@description('The API App resource ID')
output apiAppId string = apiApp.id

@description('The API App name')
output apiAppName string = apiApp.name

@description('The API App default hostname')
output apiAppDefaultHostname string = apiApp.properties.defaultHostName

@description('The API App principal ID (managed identity)')
output apiAppPrincipalId string = apiApp.identity.principalId

@description('The Frontend App resource ID')
output frontendAppId string = frontendApp.id

@description('The Frontend App name')
output frontendAppName string = frontendApp.name

@description('The Frontend App default hostname')
output frontendAppDefaultHostname string = frontendApp.properties.defaultHostName
