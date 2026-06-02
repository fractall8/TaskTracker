# Task Tracker
## Prerequisites
Before you begin, ensure you have the following installed:
- .NET SDK 10.0 (not required if you will start up app only from Docker)
- Docker
## Getting Started
1. Configure the Environment.<br>Make sure that you set environment variables in .env
```.env
DB_USER="DB_USER"
DB_PASSWORD="DB_PASSWORD"
DB_NAME="DB_NAME"
```

2. Start the Application via docker compose<br>
**Option A: Full Docker Deployment**<br>
In this project, I use a Docker-first approach for our infrastructure. The database migrations are not executed via EF Core Program.cs. Instead, the Database project is containerized and runs as an Init Container. File storage is handled locally via Azurite (Azure Blob Storage emulator).
Run the following command from the root directory where docker-compose.yml is located:

```Bash
docker-compose up -d --build
```
What happens under the hood:
- Docker spins up the PostgreSQL container.
- The db-migration (DbUp) container starts, connects to Postgres, and automatically applies all pending SQL scripts to ensure your schema is up-to-date.
- The migration container safely exits.
- The Azurite container starts, providing a local Azure Blob Storage environment for file uploads (avatars, task attachments).
- Then api container started and you can access application
---
**Option B: Hybrid Mode (Local API + Docker Infrastructure)**<br>
You can run Docker only for the database, migrations, and blob storage, which is convenient for local debugging:
```bash
docker-compose up -d postgres-db db-migration azurite
```
Before running the API locally, ensure you have the correct ```PostgresConnection``` in your ```appsettings.Development.json``` (or User Secrets) matching the variables in your ```.env``` file, with the host set to ```localhost```, and the Azure Blob Storage should point to the local Azurite instance:
```json
"ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Port=5432;Database=DB_NAME;Username=DB_USER;Password=DB_PASSWORD;",
    "AzureBlobStorageConnection": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=[http://127.0.0.1:10000/devstoreaccount1](http://127.0.0.1:10000/devstoreaccount1);"
}
```
*Note: The Azurite connection string uses the standard, publicly known Microsoft emulator account key.*
Then, run the API locally with these commands (from the root of the project):
```
cd ./Presentation
dotnet run
```
---
3. Verify the Deployment<br>
If you are running up with docker compose, api will be available on
```bash
http://localhost:8080
```
The default port is ```8080```. You can change it in docker-compose.yml. Here:
```docker-compose.yml
    api:
    image: tasktracker-api
    container_name: tasktracker-api
    build:
      context: .
      dockerfile: ./Presentation/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__PostgresConnection=Host=postgres-db;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};
      # It is hardcoded development credentials for the Azurite Emulator (Not the real azure, only local emulator)
      - ConnectionStrings__AzureBlobStorage=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;
    depends_on:
      - postgres-db
      - azurite
```
*Note: The API container exposes ports ```8080``` and ```8081```. You can configure this directly in the API's ```Dockerfile``` if needed.*

## Useful Commands
If you need to completely reset your database and clear all volumes (e.g., to test fresh migrations):
```bash
docker-compose down -v
```
