param name string
param tags object

@description('Where ACS stores data. Not the same as the resource location.')
param dataLocation string = 'europe'

// Identity + Rooms only, so no email domain to verify.
resource acs 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

output name string = acs.name
