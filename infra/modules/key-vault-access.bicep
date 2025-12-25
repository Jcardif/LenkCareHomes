// ============================================================================
// Key Vault Access Module
// ============================================================================
// Grants the API App Service managed identity access to Key Vault secrets
// Uses RBAC (Key Vault Secrets User role)
// ============================================================================

@description('Name of the Key Vault')
param keyVaultName string

@description('Principal ID of the API App Service managed identity')
param apiAppPrincipalId string

// ============================================================================
// Variables
// ============================================================================

// Key Vault Secrets User role - allows reading secrets
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// ============================================================================
// Resources
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Grant API App access to read secrets
resource apiAppSecretsAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    principalId: apiAppPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalType: 'ServicePrincipal'
  }
}
