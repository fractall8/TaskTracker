param name string
param location string
param tags object

@description('Managed identity granted read access, for future Key Vault referenced secrets.')
param principalId string

@secure()
param secrets object

var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

// Seeded so rotation has a home. The container apps read plain secrets rather than Key Vault
// references, because an RBAC assignment made in the same deployment has not propagated yet.
resource vaultSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [
  for item in items(secrets): {
    parent: vault
    name: item.key
    properties: {
      value: item.value
    }
  }
]

resource secretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, principalId, secretsUserRoleId)
  scope: vault
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
  }
}

output name string = vault.name
output uri string = vault.properties.vaultUri
