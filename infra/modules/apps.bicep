param location string
param tags object
param environmentId string
param identityId string
param registryLoginServer string
param imageTag string

param apiAppName string
param webAppName string
param funcAppName string
param apiFqdn string
param webFqdn string

param appInsightsConnectionString string

param storageName string
param postgresFqdn string
param postgresDatabase string
param postgresAdminUser string
@secure()
param postgresAdminPassword string
param serviceBusName string
param queueName string
param cosmosDatabaseName string
param cosmosContainerName string
param communicationName string

param azureTenantId string
param azureClientId string
param openAiEndpoint string
param openAiChatDeployment string
param aiSearchEndpoint string
param aiSearchIndexName string
param businessCalendarTimeZoneId string

@secure()
param openAiApiKey string
@secure()
param aiSearchApiKey string
@secure()
param cosmosConnectionString string
@secure()
param stripeSecretKey string
@secure()
param stripeWebhookSecret string
@secure()
param internalApiKey string

@description('ASPNETCORE_ENVIRONMENT. Development keeps Swagger reachable on the dev box.')
param aspNetCoreEnvironment string = 'Development'

// --- connection strings resolved at deploy time, so no key is ever copied by hand ---

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageName
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusName
}

resource serviceBusRootRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2022-10-01-preview' existing = {
  parent: serviceBusNamespace
  name: 'RootManageSharedAccessKey'
}

resource acs 'Microsoft.Communication/communicationServices@2023-04-01' existing = {
  name: communicationName
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var serviceBusConnectionString = serviceBusRootRule.listKeys().primaryConnectionString
var acsConnectionString = acs.listKeys().primaryConnectionString
var postgresConnectionString = 'Host=${postgresFqdn};Port=5432;Database=${postgresDatabase};Username=${postgresAdminUser};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true;'

var registryConfig = [
  {
    server: registryLoginServer
    identity: identityId
  }
]

var userIdentity = {
  type: 'UserAssigned'
  userAssignedIdentities: {
    '${identityId}': {}
  }
}

// --- API ---

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  tags: tags
  identity: userIdentity
  properties: {
    environmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        // 'auto' keeps HTTP/2 and websockets available, which the SignalR hubs need.
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: registryConfig
      secrets: [
        { name: 'postgres-connection', value: postgresConnectionString }
        { name: 'storage-connection', value: storageConnectionString }
        { name: 'servicebus-connection', value: serviceBusConnectionString }
        { name: 'cosmos-connection', value: cosmosConnectionString }
        { name: 'acs-connection', value: acsConnectionString }
        { name: 'stripe-secret-key', value: stripeSecretKey }
        { name: 'stripe-webhook-secret', value: stripeWebhookSecret }
        { name: 'openai-api-key', value: openAiApiKey }
        { name: 'aisearch-api-key', value: aiSearchApiKey }
        { name: 'internal-api-key', value: internalApiKey }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${registryLoginServer}/tasktracker-api:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: aspNetCoreEnvironment }
            // Ingress terminates TLS and forwards plain HTTP; without this UseHttpsRedirection loops.
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'ASPNETCORE_HTTP_PORTS', value: '8080' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
            { name: 'ConnectionStrings__PostgresConnection', secretRef: 'postgres-connection' }
            { name: 'ConnectionStrings__AzureBlobStorage', secretRef: 'storage-connection' }
            { name: 'ConnectionStrings__ServiceBus', secretRef: 'servicebus-connection' }
            { name: 'ConnectionStrings__CosmosDB', secretRef: 'cosmos-connection' }
            { name: 'ConnectionStrings__AzureCommunicationServices', secretRef: 'acs-connection' }
            { name: 'AzureAd__TenantId', value: azureTenantId }
            { name: 'AzureAd__ClientId', value: azureClientId }
            { name: 'AzureAd__Audience', value: 'api://${azureClientId}' }
            { name: 'Frontend__AllowedOrigins__0', value: 'https://${webFqdn}' }
            { name: 'Stripe__SecretKey', secretRef: 'stripe-secret-key' }
            { name: 'Stripe__WebhookSecret', secretRef: 'stripe-webhook-secret' }
            { name: 'Stripe__SuccessUrl', value: 'https://${webFqdn}/workspaces/{workspaceId}/subscriptions/success?planId={planId}' }
            { name: 'Stripe__CancelUrl', value: 'https://${webFqdn}/workspaces/{workspaceId}' }
            { name: 'AzureOpenAi__Endpoint', value: openAiEndpoint }
            { name: 'AzureOpenAi__ApiKey', secretRef: 'openai-api-key' }
            { name: 'AzureOpenAi__ChatDeploymentName', value: openAiChatDeployment }
            { name: 'AzureAiSearch__Endpoint', value: aiSearchEndpoint }
            { name: 'AzureAiSearch__ApiKey', secretRef: 'aisearch-api-key' }
            { name: 'AzureAiSearch__IndexName', value: aiSearchIndexName }
            { name: 'InternalApi__ApiKey', secretRef: 'internal-api-key' }
            { name: 'CosmosDB__DatabaseName', value: cosmosDatabaseName }
            { name: 'CosmosDB__Containers__BoardExport', value: cosmosContainerName }
            { name: 'ServiceBus__QueueNames__BoardArchivingQueueName', value: queueName }
            { name: 'BusinessCalendar__TimeZoneId', value: businessCalendarTimeZoneId }
          ]
        }
      ]
      scale: {
        // Pinned at 1: the SignalR hubs hold per-instance state and Hangfire recurring jobs
        // would double-fire. Raise only after adding a backplane.
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// --- frontend ---

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: webAppName
  location: location
  tags: tags
  identity: userIdentity
  properties: {
    environmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: registryConfig
    }
    template: {
      containers: [
        {
          name: 'web'
          image: '${registryLoginServer}/tasktracker-frontend:${imageTag}'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          // entrypoint.sh runs envsubst over appsettings.template.json, so one image
          // serves every environment.
          env: [
            { name: 'AZURE_TENANT_ID', value: azureTenantId }
            { name: 'AZURE_CLIENT_ID', value: azureClientId }
            { name: 'API_CLIENT_SCOPES', value: 'api://${azureClientId}/access_as_user' }
            { name: 'API_BASE_URL', value: 'https://${apiFqdn}' }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

// --- Functions worker ---

resource funcApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: funcAppName
  location: location
  tags: tags
  identity: userIdentity
  properties: {
    environmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      // No ingress: this is a Service Bus worker, woken by the KEDA scaler below.
      registries: registryConfig
      secrets: [
        { name: 'storage-connection', value: storageConnectionString }
        { name: 'servicebus-connection', value: serviceBusConnectionString }
        { name: 'cosmos-connection', value: cosmosConnectionString }
        { name: 'internal-api-key', value: internalApiKey }
      ]
    }
    template: {
      containers: [
        {
          name: 'functions'
          image: '${registryLoginServer}/tasktracker-functions:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'AzureWebJobsStorage', secretRef: 'storage-connection' }
            { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
            { name: 'ConnectionStrings__ServiceBus', secretRef: 'servicebus-connection' }
            { name: 'ConnectionStrings__CosmosDB', secretRef: 'cosmos-connection' }
            { name: 'ConnectionStrings__BlobStorage', secretRef: 'storage-connection' }
            { name: 'CosmosDB__DatabaseName', value: cosmosDatabaseName }
            { name: 'CosmosDB__Containers__BoardExport', value: cosmosContainerName }
            { name: 'BlobStorage__ArchivesContainerName', value: 'board-archives' }
            { name: 'BlobStorage__TaskAttachmentsContainerName', value: 'attachments' }
            { name: 'BoardExportApi__BaseUrl', value: 'https://${apiFqdn}/api/' }
            { name: 'BoardExportApi__ApiKeyHeaderName', value: 'X-Internal-Api-Key' }
            { name: 'BoardExportApi__ApiKey', secretRef: 'internal-api-key' }
            { name: 'BoardExportApi__RequestTimeoutMinutes', value: '10' }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
        rules: [
          {
            name: 'board-archiving-queue'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: queueName
                messageCount: '5'
              }
              auth: [
                {
                  secretRef: 'servicebus-connection'
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

output apiFqdn string = apiApp.properties.configuration.ingress.fqdn
output webFqdn string = webApp.properties.configuration.ingress.fqdn
output functionsAppName string = funcApp.name
