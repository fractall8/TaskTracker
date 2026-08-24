param name string
param location string
param tags object
param postgresVersion string
param administratorLogin string = 'ttadmin'
@secure()
param administratorPassword string
param databaseName string = 'TaskTrackerDb'
param clientIpAddress string = ''

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: postgresVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// The DbUp job runs inside Azure, so this rule is what lets the migration reach the server.
resource allowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: server
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource allowClient 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = if (!empty(clientIpAddress)) {
  parent: server
  name: 'AllowClient'
  properties: {
    startIpAddress: clientIpAddress
    endIpAddress: clientIpAddress
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
  dependsOn: [
    allowAzure
  ]
}

output name string = server.name
output fqdn string = server.properties.fullyQualifiedDomainName
output administratorLogin string = administratorLogin
output databaseName string = database.name
