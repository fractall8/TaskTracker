using 'main.bicep'

param appName = 'tasktracker'
param env = 'dev'
param imageTag = readEnvironmentVariable('IMAGE_TAG', 'latest')

// Leave false for the very first deploy, before any image is pushed to ACR.
param deployApps = bool(readEnvironmentVariable('DEPLOY_APPS', 'true'))

param postgresVersion = '16'
param clientIpAddress = readEnvironmentVariable('CLIENT_IP', '')

// From the Entra app registrations, created manually. See README.
param azureTenantId = readEnvironmentVariable('AZURE_TENANT_ID')
param azureClientId = readEnvironmentVariable('AZURE_CLIENT_ID')

// Resources you already own.
param openAiEndpoint = readEnvironmentVariable('AZURE_OPENAI_ENDPOINT')
param openAiChatDeployment = readEnvironmentVariable('AZURE_OPENAI_DEPLOYMENT', 'gpt-4o-mini')
param aiSearchEndpoint = readEnvironmentVariable('AZURE_AI_SEARCH_ENDPOINT')
param aiSearchIndexName = readEnvironmentVariable('AZURE_AI_SEARCH_INDEX', 'faq-index')

param businessCalendarTimeZoneId = 'Europe/Kyiv'

param postgresAdminPassword = readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD')
param stripeSecretKey = readEnvironmentVariable('STRIPE_SECRET_KEY')
param stripeWebhookSecret = readEnvironmentVariable('STRIPE_WEBHOOK_SECRET')
param internalApiKey = readEnvironmentVariable('INTERNAL_API_KEY')
param openAiApiKey = readEnvironmentVariable('AZURE_OPENAI_API_KEY')
param aiSearchApiKey = readEnvironmentVariable('AZURE_AI_SEARCH_API_KEY')
