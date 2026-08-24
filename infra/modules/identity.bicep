param name string
param location string
param tags object
param registryName string

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
  tags: tags
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: registryName
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, uai.id, acrPullRoleId)
  scope: registry
  properties: {
    principalId: uai.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
  }
}

output id string = uai.id
output principalId string = uai.properties.principalId
output clientId string = uai.properties.clientId
