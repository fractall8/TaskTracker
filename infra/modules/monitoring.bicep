param name string
param location string
param tags object

@description('Daily ingestion cap in GB. Serilog across three apps adds up fast.')
param dailyQuotaGb int = 1

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: dailyQuotaGb
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: replace(name, 'log-', 'appi-')
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

output name string = workspace.name
output id string = workspace.id
output appInsightsConnectionString string = appInsights.properties.ConnectionString
