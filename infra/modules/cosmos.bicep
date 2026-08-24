param name string
param location string
param tags object

@description('Must match CosmosDB:DatabaseName.')
param databaseName string = 'TaskTrackerCosmosDB'

@description('Must match CosmosDB:Containers:BoardExport.')
param containerName string = 'BoardExports'

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: name
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    // Serverless: board-export metadata is low, bursty traffic. Provisioned has a 400 RU/s floor.
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    minimalTlsVersion: 'Tls12'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: containerName
  properties: {
    resource: {
      id: containerName
      // BoardExportDocument is partitioned by /boardId; changing this breaks every read.
      partitionKey: {
        paths: [
          '/boardId'
        ]
        kind: 'Hash'
      }
    }
  }
}

output name string = account.name
output databaseName string = database.name
output containerName string = container.name
