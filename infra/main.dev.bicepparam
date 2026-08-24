using 'main.bicep'

// GitHub Actions sets every one of these, so an unset repository variable arrives as an empty
// string rather than as an absent variable -- and readEnvironmentVariable only falls back to its
// default when the variable is absent. Anything with a meaningful default is therefore read into
// a var first and emptiness-checked, so a blank variable does not silently win.

var envLocation = readEnvironmentVariable('AZURE_LOCATION', '')
var envImageTag = readEnvironmentVariable('IMAGE_TAG', '')
var envDeployApps = readEnvironmentVariable('DEPLOY_APPS', '')
var envDeployJob = readEnvironmentVariable('DEPLOY_JOB', '')
var envOpenAiDeployment = readEnvironmentVariable('AZURE_OPENAI_DEPLOYMENT', '')
var envSearchIndex = readEnvironmentVariable('AZURE_AI_SEARCH_INDEX', '')
var envPostgresSku = readEnvironmentVariable('POSTGRES_SKU', '')
var envPostgresTier = readEnvironmentVariable('POSTGRES_TIER', '')
var envPostgresVersion = readEnvironmentVariable('POSTGRES_VERSION', '')

param appName = 'tasktracker'
param env = 'dev'

// Azure for Students restricts which regions a subscription may deploy to; Poland Central is
// known good for this one. Set AZURE_LOCATION to move it.
param location = empty(envLocation) ? 'polandcentral' : envLocation

param imageTag = empty(envImageTag) ? 'latest' : envImageTag

// Left false on the very first deploy, before any image is pushed to the registry.
param deployApps = empty(envDeployApps) ? true : bool(envDeployApps)
param deployJob = empty(envDeployJob) ? true : bool(envDeployJob)

param postgresVersion = empty(envPostgresVersion) ? '16' : envPostgresVersion
param postgresSkuName = empty(envPostgresSku) ? 'Standard_B1ms' : envPostgresSku
param postgresSkuTier = empty(envPostgresTier) ? 'Burstable' : envPostgresTier
param clientIpAddress = readEnvironmentVariable('CLIENT_IP', '')

// From the Entra app registration for the app itself. See README section 1.
param azureTenantId = readEnvironmentVariable('AZURE_TENANT_ID')
param azureClientId = readEnvironmentVariable('AZURE_CLIENT_ID')

// Existing resources, referenced rather than created.
param openAiEndpoint = readEnvironmentVariable('AZURE_OPENAI_ENDPOINT')
param openAiChatDeployment = empty(envOpenAiDeployment) ? 'gpt-5-mini' : envOpenAiDeployment
param aiSearchEndpoint = readEnvironmentVariable('AZURE_AI_SEARCH_ENDPOINT')
param aiSearchIndexName = empty(envSearchIndex) ? 'faq-index' : envSearchIndex

param businessCalendarTimeZoneId = 'Europe/Kyiv'

param postgresAdminPassword = readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD')
param stripeSecretKey = readEnvironmentVariable('STRIPE_SECRET_KEY')
param stripeWebhookSecret = readEnvironmentVariable('STRIPE_WEBHOOK_SECRET')
param internalApiKey = readEnvironmentVariable('INTERNAL_API_KEY')
param openAiApiKey = readEnvironmentVariable('AZURE_OPENAI_API_KEY')
param aiSearchApiKey = readEnvironmentVariable('AZURE_AI_SEARCH_API_KEY')
param cosmosConnectionString = readEnvironmentVariable('COSMOS_CONNECTION_STRING')
