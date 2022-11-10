param location string
var defaultResourceName = 'demo'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: uniqueString('${defaultResourceName}-storageaccount')
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
