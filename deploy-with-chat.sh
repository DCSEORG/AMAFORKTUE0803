#!/bin/bash

# ============================================
# Expense Management System - Full Deployment with GenAI
# Deploys App Service, Azure SQL, Azure OpenAI, and AI Search
# ============================================

set -e

echo "=========================================="
echo "Expense Management System - Full Deployment"
echo "Including GenAI Services (Azure OpenAI + AI Search)"
echo "=========================================="

# Configuration
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
BASE_NAME="expensemgmt"

# Get current user info for SQL Admin
echo ""
echo "Step 1: Getting Azure AD user info..."
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo "  Admin Login: $ADMIN_LOGIN"
echo "  Admin Object ID: $ADMIN_OBJECT_ID"

# Create Resource Group
echo ""
echo "Step 2: Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo "  ✓ Resource group created: $RESOURCE_GROUP"

# Deploy Infrastructure (App Service + SQL + GenAI)
echo ""
echo "Step 3: Deploying infrastructure (App Service + Azure SQL + GenAI)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infra/main.bicep \
    --parameters \
        baseName=$BASE_NAME \
        adminObjectId=$ADMIN_OBJECT_ID \
        adminLogin=$ADMIN_LOGIN \
        deployGenAI=true \
    --query "properties.outputs" \
    --output json)

echo "  ✓ Infrastructure deployed"

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_HOST_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceHostName.value')
MANAGED_IDENTITY_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityId.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')

echo ""
echo "Deployment Outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  App URL: https://$APP_HOST_NAME/Index"
echo "  SQL Server: $SQL_SERVER_NAME"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"

# Configure App Service settings
echo ""
echo "Step 4: Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:$SQL_SERVER_FQDN;Database=$DATABASE_NAME;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;"

az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
        "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
        "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
    --output none

echo "  ✓ App settings configured (including OpenAI)"

# Wait for SQL Server to be ready
echo ""
echo "Step 5: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo ""
echo "Step 6: Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "DeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none
echo "  ✓ Firewall rule added for IP: $MY_IP"

# Update Python scripts with actual values
echo ""
echo "Step 7: Updating Python scripts with connection info..."

# Cross-platform sed (works on Mac and Linux)
sed -i.bak "s/sql-expensemgmt-UNIQUEID.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-UNIQUEID.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-UNIQUEID.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

echo "  ✓ Python scripts updated"

# Install Python dependencies
echo ""
echo "Step 8: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Import database schema
echo ""
echo "Step 9: Importing database schema..."
python3 run-sql.py

# Configure managed identity roles
echo ""
echo "Step 10: Configuring managed identity database roles..."
python3 run-sql-dbrole.py

# Create stored procedures
echo ""
echo "Step 11: Creating stored procedures..."
python3 run-sql-stored-procs.py

# Build and package the application
echo ""
echo "Step 12: Building and packaging application..."
cd src/ExpenseManagement
dotnet restore
dotnet publish -c Release -o ./publish

# Create zip file (ensure files are at root level)
cd publish
zip -r ../../../app.zip ./*
cd ../../..

echo "  ✓ Application packaged: app.zip"

# Deploy to Azure
echo ""
echo "Step 13: Deploying application to Azure..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip

echo "  ✓ Application deployed"

# Summary
echo ""
echo "=========================================="
echo "Full Deployment Complete!"
echo "=========================================="
echo ""
echo "Application URL: https://$APP_HOST_NAME/Index"
echo "Swagger API Docs: https://$APP_HOST_NAME/swagger"
echo "Chat UI: https://$APP_HOST_NAME/Chat"
echo ""
echo "GenAI Services:"
echo "  Azure OpenAI: $OPENAI_ENDPOINT"
echo "  Model: $OPENAI_MODEL_NAME (GPT-4o)"
echo "  Search: $SEARCH_ENDPOINT"
echo ""
echo "The Chat UI is now fully functional with AI capabilities!"
echo ""
echo "To run locally:"
echo "1. Change connection string in appsettings.json to use:"
echo "   Authentication=Active Directory Default"
echo "2. Add OpenAI settings to appsettings.json"
echo "3. Run: az login"
echo "4. Run: cd src/ExpenseManagement && dotnet run"
echo ""
