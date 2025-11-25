# Azure Services Architecture

This diagram shows the Azure services deployed by this solution and how they connect.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           Azure Resource Group                                   │
│                          (rg-expensemgmt-demo)                                  │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         User Assigned Managed Identity                    │   │
│  │                        (mid-AppModAssist-xxxxx)                          │   │
│  │                                                                          │   │
│  │   Used for secure, passwordless authentication between services          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                      │                                          │
│                                      │ Assigned to                              │
│                                      ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         Azure App Service                                │   │
│  │                    (app-expensemgmt-xxxxx)                               │   │
│  │                                                                          │   │
│  │   • .NET 8 ASP.NET Razor Pages Application                              │   │
│  │   • REST APIs with Swagger Documentation                                 │   │
│  │   • Chat UI with AI Integration                                          │   │
│  │   • Standard S1 SKU (UK South)                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                    │                                        │                    │
│                    │ Managed Identity Auth                  │ Managed Identity  │
│                    ▼                                        ▼ Auth              │
│  ┌──────────────────────────────┐    ┌──────────────────────────────────────┐  │
│  │      Azure SQL Database      │    │        Azure OpenAI Service          │  │
│  │   (sql-expensemgmt-xxxxx)    │    │      (oai-expensemgmt-xxxxx)         │  │
│  │                              │    │                                       │  │
│  │   • Northwind Database       │    │   • GPT-4o Model (Sweden Central)    │  │
│  │   • Entra ID Only Auth       │    │   • Function Calling for DB Ops      │  │
│  │   • Basic SKU                │    │   • S0 SKU                           │  │
│  │   • Stored Procedures        │    │                                       │  │
│  └──────────────────────────────┘    └──────────────────────────────────────┘  │
│                                                            │                    │
│                                                            │ Managed Identity   │
│                                                            ▼ Auth              │
│                                       ┌──────────────────────────────────────┐  │
│                                       │      Azure AI Search                 │  │
│                                       │    (srch-expensemgmt-xxxxx)          │  │
│                                       │                                       │  │
│                                       │   • RAG Pattern Support              │  │
│                                       │   • Basic SKU (UK South)             │  │
│                                       └──────────────────────────────────────┘  │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘

                                    │
                                    │ HTTPS
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                                   Users                                          │
│                                                                                  │
│   • Access application at: https://app-expensemgmt-xxxxx.azurewebsites.net/Index │
│   • View API docs at: /swagger                                                   │
│   • Use Chat UI at: /Chat                                                        │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## Security Features

- **Entra ID Only Authentication**: SQL Server uses Azure AD authentication exclusively (no SQL auth)
- **Managed Identity**: All service-to-service communication uses passwordless Managed Identity
- **HTTPS Only**: App Service configured for HTTPS-only traffic
- **Minimal TLS Version**: TLS 1.2 required for all connections

## Deployment Options

1. **deploy.sh**: Deploys App Service + SQL Database (Chat UI shows placeholder message)
2. **deploy-with-chat.sh**: Deploys everything including Azure OpenAI + AI Search (Full AI capabilities)

## Data Flow

1. User submits expense through web UI or Chat
2. App Service receives request and authenticates with Managed Identity
3. Stored procedures in SQL Database handle all data operations
4. For Chat: Azure OpenAI processes natural language and calls functions
5. Functions execute API calls that use stored procedures
6. Results returned to user through the UI
