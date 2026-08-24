param name string
param location string
param tags object

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    // Pulls go through the managed identity, so the admin account stays off.
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

output name string = registry.name
output loginServer string = registry.properties.loginServer
