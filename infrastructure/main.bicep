targetScope = 'subscription'

param projectName string
@allowed([
  'dev'
  'tst'
  'prd'
])
param environmentName string
param locationAbbreviation string
param location string = deployment().location

var resourceGroupName = toLower('${projectName}-${environmentName}-${locationAbbreviation}')

resource targetResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

module resourceModule 'resources.bicep' = {
  name: 'resourceModule'
  scope: targetResourceGroup
  params: {
    environmentName: environmentName
    locationAbbreviation: locationAbbreviation
    projectName: projectName
    location: location
  }
}

output functionsAppName string = resourceModule.outputs.functionsAppName
