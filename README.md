# Task Tracker

## Prerequisites
Before you begin, ensure you have the following installed:
- .NET SDK (not required if you will start up app only from Docker)
- Docker

## Getting Started

1. Configure the Environment.<br>
   Make sure that you set environment variables in `.env`
```env
DB_USER="DB_USER"
DB_PASSWORD="DB_PASSWORD"
DB_NAME="DB_NAME"
AZURITE_CONNECTION_STRING="DefaultEndpointsProtocol=http;AccountName=ACCOUNT_NAME;AccountKey=ACCOUNT_KEY;BlobEndpoint=BLOB_ENDPOINT"
AZURE_CLIENT_ID="CLIENT_ID"
AZURE_TENANT_ID="TENANT_ID"
```
*Note: copy ACCOUNT_NAME and ACCOUNT_KEY from [official documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-connect-azurite?tabs=blob-storage#use-a-well-known-storage-account-and-key) as it is well-known constants for development*<br>
*Create BLOB_ENDPOINT from this template `http://CONTAINER_NAME:CONTAINER_PORT/ACCOUNT_NAME`*<br>
*If you are using my version of docker compose it will be `http://azurite:10000/ACCOUNT_NAME`*<br>
*For Azure AD integration, `AZURE_CLIENT_ID` and `AZURE_TENANT_ID` must match your Entra ID app registration. Set `AZURE_TENANT_ID` to `common` if you are using a multi-tenant setup.*

2. Start the Application via docker compose<br>

**Option A: Full Docker Deployment**<br>
In this project, I use a Docker-first approach for our infrastructure. The database migrations are not executed via EF Core Program.cs. Instead, the Database project is containerized and runs as an Init Container. File storage is handled locally via Azurite (Azure Blob Storage emulator).

Run the following command from the root directory where `docker-compose.yml` is located:

```bash
docker-compose up -d --build
```
What happens under the hood:
- Docker spins up the PostgreSQL container.
- The `db-migration` (DbUp) container starts, connects to Postgres, and automatically applies all pending SQL scripts to ensure your schema is up-to-date.
- The migration container safely exits.
- The Azurite container starts, providing a local Azure Blob Storage environment for file uploads (avatars, task attachments).
- The `api` container starts, connecting to the database, Azurite, and configuring Azure AD authentication.
- Finally, the `frontend` container starts, serving the client web application configured with your Azure AD scopes.

---
**Option B: Hybrid Mode (Local API + Docker Infrastructure)**<br>
You can run Docker only for the database, migrations, and blob storage, which is convenient for local debugging:
```bash
docker-compose up -d postgres-db db-migration azurite
```
Before running the API locally, ensure you have the correct connection strings in your `appsettings.Development.json` (or User Secrets) matching the variables in your `.env` file, with the host set to `localhost`, and the Azure Blob Storage should point to the local Azurite instance. You also need to configure the Azure AD section:

```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Port=5432;Database=DB_NAME;Username=DB_USER;Password=DB_PASSWORD;",
    "AzureBlobStorage": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=[http://127.0.0.1:10000/devstoreaccount1](http://127.0.0.1:10000/devstoreaccount1);"
  },
  "AzureAd": {
    "Instance": "[https://login.microsoftonline.com/](https://login.microsoftonline.com/)",
    "TenantId": "TENANT_ID",
    "ClientId": "CLIENT_ID",
    "Audience": "api://CLIENT_ID"
  }
}
```
*Note: The Azurite connection string uses the standard, publicly known Microsoft emulator account key.*

Then, run the API locally with these commands (from the root of the project):
```bash
cd ./src/Backend/Presentation
dotnet run
```
*(You can run the frontend locally in a similar way from `./src/Frontend/WebApp`)*

---
3. Verify the Deployment<br>
   If you are running up with docker compose, the application services will be available at:
- **Frontend (Web App):** `http://localhost:3000`
- **Backend (API):** `http://localhost:8080` (Swagger UI is usually available at `/swagger`)

*Note: The Frontend container exposes port `3000`. The API container exposes port `8080`. You can configure this directly in the `docker-compose.yml` or respective Dockerfiles if needed.*

## Useful Commands
If you need to completely reset your database and clear all volumes (e.g., to test fresh migrations):
```bash
docker-compose down -v
```