// Azure OpenAI and Cognitive Services for GenAI Chat UI
// Uses GPT-4o model in Sweden region

@description('Location for GenAI resources - Sweden for GPT-4o')
param location string = 'swedencentral'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Managed Identity Principal ID for role assignments')
param managedIdentityPrincipalId string

// Generate unique names
var openAIName = 'oai-${baseName}-${uniqueSuffix}'
var searchName = 'srch-${baseName}-${uniqueSuffix}'
var modelDeploymentName = 'gpt-4o'

// Azure OpenAI Service
resource openAI 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: openAIName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// GPT-4o Model Deployment
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAI
  name: modelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
    raiPolicyName: 'Microsoft.Default'
  }
}

// Azure AI Search (Cognitive Search) for RAG
resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: 'uksouth' // Search in same region as other resources
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Role Assignment: Cognitive Services OpenAI User for Managed Identity
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, 'Cognitive Services OpenAI User')
  scope: openAI
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services OpenAI User
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment: Search Index Data Contributor for Managed Identity
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, managedIdentityPrincipalId, 'Search Index Data Contributor')
  scope: search
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7') // Search Index Data Contributor
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output openAIEndpoint string = openAI.properties.endpoint
output openAIName string = openAI.name
output openAIModelName string = modelDeploymentName
output searchEndpoint string = 'https://${search.name}.search.windows.net'
output searchName string = search.name
