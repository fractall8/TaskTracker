param name string
param location string
param tags object

@description('Must match ServiceBus:QueueNames:BoardArchivingQueueName.')
param queueName string = 'board-archiving-queue'

// Basic supports queues only, which is all the board-archiving pipeline needs.
resource namespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: namespace
  name: queueName
  properties: {
    maxDeliveryCount: 5
    lockDuration: 'PT5M'
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
  }
}

output name string = namespace.name
output queueName string = queue.name
