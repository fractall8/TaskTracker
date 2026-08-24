targetScope = 'resourceGroup'

// Dev environment for TaskTracker. Single command:
//   az deployment group create -g <rg> -f infra/main.bicep -p infra/main.dev.bicepparam
// See infra/README.md for the two-phase first run and the manual prerequisites.

@description('Base name used in every resource name.')
param appName string = 'tasktracker'

@description('Environment short name.')
param env string = 'dev'

param location string = resourceGroup().location

@description('Image tag to deploy. CI passes the git SHA.')
param imageTag string = 'latest'

@description('False on the very first deploy, before any image exists in ACR.')
param deployApps bool = true

@description('Postgres major version. Bump to 17 only if the region supports it.')
param postgresVersion string = '16'

@description('Optional client IP allowed through the Postgres firewall, for psql from your machine.')
param clientIpAddress string = ''

// --- Entra ID: app registrations are created manually, see README ---
param azureTenantId string
param azureClientId string

// --- external endpoints (resources you already own) ---
param openAiEndpoint string
param openAiChatDeployment string
param aiSearchEndpoint string
param aiSearchIndexName string

// Cosmos is an existing account, not created here: the subscription's one free-tier account
// is already claimed, so a second would be billed.
param cosmosDatabaseName string = 'TaskTrackerCosmosDB'
param cosmosContainerName string = 'BoardExports'

param businessCalendarTimeZoneId string = 'Europe/Kyiv'

// --- secrets, supplied as secure parameters ---
@secure()
param postgresAdminPassword string
@secure()
param stripeSecretKey string
@secure()
param stripeWebhookSecret string
@secure()
param internalApiKey string
@secure()
param openAiApiKey string
@secure()
param aiSearchApiKey string
@secure()
param cosmosConnectionString string

var suffix = take(uniqueString(resourceGroup().id), 6)
var tags = {
  application: appName
  environment: env
  managedBy: 'bicep'
}

// Container app names are baked into the FQDNs, so keep them short and stable.
var apiAppName = 'ca-api-${env}'
var webAppName = 'ca-web-${env}'
var funcAppName = 'ca-func-${env}'

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    name: 'log-${appName}-${env}'
    location: location
    tags: tags
  }
}

module registry 'modules/registry.bicep' = {
  name: 'registry'
  params: {
    name: 'cr${appName}${env}${suffix}'
    location: location
    tags: tags
  }
}

module identity 'modules/identity.bicep' = {
  name: 'identity'
  params: {
    name: 'id-${appName}-${env}'
    location: location
    tags: tags
    registryName: registry.outputs.name
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: 'st${appName}${env}${suffix}'
    location: location
    tags: tags
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    name: 'psql-${appName}-${env}-${suffix}'
    location: location
    tags: tags
    postgresVersion: postgresVersion
    administratorPassword: postgresAdminPassword
    clientIpAddress: clientIpAddress
  }
}

module serviceBus 'modules/servicebus.bicep' = {
  name: 'serviceBus'
  params: {
    name: 'sb-${appName}-${env}-${suffix}'
    location: location
    tags: tags
  }
}

module communication 'modules/communication.bicep' = {
  name: 'communication'
  params: {
    name: 'acs-${appName}-${env}-${suffix}'
    tags: tags
  }
}

module vault 'modules/keyvault.bicep' = {
  name: 'vault'
  params: {
    name: 'kv-tt-${env}-${suffix}'
    location: location
    tags: tags
    principalId: identity.outputs.principalId
    secrets: {
      'stripe-secret-key': stripeSecretKey
      'stripe-webhook-secret': stripeWebhookSecret
      'internal-api-key': internalApiKey
      'openai-api-key': openAiApiKey
      'aisearch-api-key': aiSearchApiKey
      'postgres-admin-password': postgresAdminPassword
    }
  }
}

module environment 'modules/environment.bicep' = {
  name: 'environment'
  params: {
    name: 'cae-${appName}-${env}'
    location: location
    tags: tags
    logAnalyticsName: monitoring.outputs.name
  }
}

// Both FQDNs are derived from the environment's default domain rather than read back off the
// apps, which is what lets the API and the frontend point at each other without a cycle.
var apiFqdn = '${apiAppName}.${environment.outputs.defaultDomain}'
var webFqdn = '${webAppName}.${environment.outputs.defaultDomain}'

// Deployed unconditionally: a job definition only pulls its image when started, so it can
// exist before any image does. This is what lets the pipeline migrate before the apps roll.
module migrateJob 'modules/job-migrate.bicep' = {
  name: 'migrateJob'
  params: {
    name: 'job-migrate-${env}'
    location: location
    tags: tags
    environmentId: environment.outputs.id
    identityId: identity.outputs.id
    registryLoginServer: registry.outputs.loginServer
    imageTag: imageTag
    postgresFqdn: postgres.outputs.fqdn
    postgresDatabase: postgres.outputs.databaseName
    postgresAdminUser: postgres.outputs.administratorLogin
    postgresAdminPassword: postgresAdminPassword
  }
}

module apps 'modules/apps.bicep' = if (deployApps) {
  name: 'apps'
  params: {
    location: location
    tags: tags
    environmentId: environment.outputs.id
    identityId: identity.outputs.id
    registryLoginServer: registry.outputs.loginServer
    imageTag: imageTag
    apiAppName: apiAppName
    webAppName: webAppName
    funcAppName: funcAppName
    apiFqdn: apiFqdn
    webFqdn: webFqdn
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    storageName: storage.outputs.name
    postgresFqdn: postgres.outputs.fqdn
    postgresDatabase: postgres.outputs.databaseName
    postgresAdminUser: postgres.outputs.administratorLogin
    postgresAdminPassword: postgresAdminPassword
    serviceBusName: serviceBus.outputs.name
    queueName: serviceBus.outputs.queueName
    cosmosConnectionString: cosmosConnectionString
    cosmosDatabaseName: cosmosDatabaseName
    cosmosContainerName: cosmosContainerName
    communicationName: communication.outputs.name
    azureTenantId: azureTenantId
    azureClientId: azureClientId
    openAiEndpoint: openAiEndpoint
    openAiChatDeployment: openAiChatDeployment
    openAiApiKey: openAiApiKey
    aiSearchEndpoint: aiSearchEndpoint
    aiSearchIndexName: aiSearchIndexName
    aiSearchApiKey: aiSearchApiKey
    stripeSecretKey: stripeSecretKey
    stripeWebhookSecret: stripeWebhookSecret
    internalApiKey: internalApiKey
    businessCalendarTimeZoneId: businessCalendarTimeZoneId
  }
}

output registryLoginServer string = registry.outputs.loginServer
output registryName string = registry.outputs.name
output keyVaultName string = vault.outputs.name
output frontendUrl string = 'https://${webFqdn}'
output apiUrl string = 'https://${apiFqdn}'
output stripeWebhookUrl string = 'https://${apiFqdn}/webhooks/stripe'
output spaRedirectUri string = 'https://${webFqdn}/authentication/login-callback'
output migrationJobName string = migrateJob.outputs.name
