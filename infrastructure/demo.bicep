targetScope = 'subscription'

param location string = deployment().location

resource targetResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'demo-tio'
  location: location
}

module storageAccount 'sa.bicep' = {
  scope: targetResourceGroup
  name: 'storageAccountModule'
  params: {
    location: targetResourceGroup.location
  }
}
