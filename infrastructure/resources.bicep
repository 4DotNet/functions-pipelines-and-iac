param projectName string
@allowed([
  'dev'
  'tst'
  'prd'
])
param environmentName string
param locationAbbreviation string
param location string = resourceGroup().location

var desiredTables = [
  'users'
]
var desiredContainers = [
  'upload'
]
var serviceBusQueueNames = [
  'validate'
  'error'
  'persist'
]

var defaultResourceName = toLower('${projectName}-${environmentName}-${locationAbbreviation}')

// Azure Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: '${defaultResourceName}-kv'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    accessPolicies: []
  }
}

// Configuration store
resource appConfig 'Microsoft.AppConfiguration/configurationStores@2022-05-01' = {
  name: '${defaultResourceName}-cfg'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Standard'
  }
}

// Add logging
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: '${defaultResourceName}-log'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${defaultResourceName}-ai'
  location: location
  kind: 'web'
  properties: {
    WorkspaceResourceId: logAnalyticsWorkspace.id
    Application_Type: 'web'
  }
}
resource applicationInsightsConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  name: 'ApplicationInsightsConnectionString'
  parent: keyVault
  properties: {
    contentType: 'text/plain'
    value: applicationInsights.properties.ConnectionString
  }
}
resource applicationInsightsConfigurationValue 'Microsoft.AppConfiguration/configurationStores/keyValues@2022-05-01' = {
  name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
  parent: appConfig
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
    value: '{"uri":"${applicationInsightsConnectionStringSecret.properties.secretUri}"}'
  }
}

// Messaging
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: '${defaultResourceName}-bus'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}
resource serviceBusQueues 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' = [for queue in serviceBusQueueNames: {
  name: queue
  parent: serviceBus
}]
resource serviceBusName 'Microsoft.AppConfiguration/configurationStores/keyValues@2022-05-01' = {
  name: 'Azure:ServiceBus'
  parent: appConfig
  properties: {
    contentType: 'text/plain'
    value: '${serviceBus.name}.servicebus.windows.net'
  }
}
resource serviceBusFqdn 'Microsoft.AppConfiguration/configurationStores/keyValues@2022-05-01' = {
  name: 'ServiceBusConnection:fullyQualifiedNamespace'
  parent: appConfig
  properties: {
    contentType: 'text/plain'
    value: '${serviceBus.name}.servicebus.windows.net'
  }
}

// Storage account
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: uniqueString('${defaultResourceName}-storage')
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}
resource storageAccountTableService 'Microsoft.Storage/storageAccounts/tableServices@2022-05-01' = {
  name: 'default'
  parent: storageAccount
}
resource storageAccountTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2022-05-01' = [for table in desiredTables: {
  name: table
  parent: storageAccountTableService
}]
resource storageAccountBlobService 'Microsoft.Storage/storageAccounts/blobServices@2022-05-01' = {
  name: 'default'
  parent: storageAccount
}
resource storageAccountBlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-05-01' = [for container in desiredContainers: {
  name: container
  parent: storageAccountBlobService
}]
resource storageAccountNameSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2022-05-01' = {
  name: 'AzureWebJobsStorage__accountName'
  parent: appConfig
  properties: {
    contentType: 'text/plain'
    value: storageAccount.name
  }
}
var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'

// Functions app
resource serverFarms 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${defaultResourceName}-plan'
  location: location
  kind: 'linux'
  sku: {
    tier: 'Dynamic'
    name: 'Y1'
  }
}
resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: '${defaultResourceName}-app'
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: serverFarms.id
    clientAffinityEnabled: false
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'AzureAppConfiguration'
          value: appConfig.properties.endpoint
        }
        {
          name: 'AzureWebJobsStorage'
          value: storageAccountConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageAccountConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: '${defaultResourceName}-app'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
      ]
    }
  }
}

module roleAssignments 'role-assignments.bicep' = {
  name: 'roleAssignmentsModule'
  params: {
    containerAppPrincipalId: functionApp.identity.principalId
  }
}

output functionsAppName string = functionApp.name
