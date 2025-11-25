// Main Bicep template for Expense Management System
// Orchestrates deployment of all Azure resources

@description('Location for resources')
param location string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Azure AD Admin Object ID for SQL Server')
param adminObjectId string

@description('Azure AD Admin Login (UPN) for SQL Server')
param adminLogin string

@description('Deploy GenAI resources (Azure OpenAI, Cognitive Search)')
param deployGenAI bool = false

// Use uniqueString for consistent naming
var uniqueSuffix = uniqueString(resourceGroup().id)

// Deploy App Service with Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
  }
}

// Deploy Azure SQL Database
module azureSql 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Conditionally deploy GenAI resources
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: 'swedencentral' // GPT-4o available in Sweden
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceHostName string = appService.outputs.appServiceHostName
output managedIdentityId string = appService.outputs.managedIdentityId
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerName string = azureSql.outputs.sqlServerName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output databaseName string = azureSql.outputs.databaseName

// GenAI outputs (conditional with null-safe operators)
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genAI.outputs.searchName : ''
