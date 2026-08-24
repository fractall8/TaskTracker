param name string
param location string
param tags object
param environmentId string
param identityId string
param registryLoginServer string
param imageTag string

param postgresFqdn string
param postgresDatabase string
param postgresAdminUser string
@secure()
param postgresAdminPassword string

var postgresConnectionString = 'Host=${postgresFqdn};Port=5432;Database=${postgresDatabase};Username=${postgresAdminUser};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true;'

// DbUp applies Scripts/000N_*.sql and exits. Manual trigger so the pipeline can run it between
// pushing images and rolling the apps.
resource migrateJob 'Microsoft.App/jobs@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    environmentId: environmentId
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 900
      replicaRetryLimit: 1
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
      registries: [
        {
          server: registryLoginServer
          identity: identityId
        }
      ]
      secrets: [
        {
          name: 'postgres-connection'
          value: postgresConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'migrate'
          image: '${registryLoginServer}/tasktracker-migration:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__PostgresConnection'
              secretRef: 'postgres-connection'
            }
          ]
        }
      ]
    }
  }
}

output name string = migrateJob.name
