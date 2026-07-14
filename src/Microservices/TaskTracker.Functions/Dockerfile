FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Microservices/TaskTracker.Functions/TaskTracker.Functions.csproj", "src/Microservices/TaskTracker.Functions/"]
COPY ["src/Shared/Contracts/Contracts.csproj", "src/Shared/Contracts/"]
RUN dotnet restore "src/Microservices/TaskTracker.Functions/TaskTracker.Functions.csproj"
COPY . .
WORKDIR "/src/src/Microservices/TaskTracker.Functions"
RUN dotnet build "./TaskTracker.Functions.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TaskTracker.Functions.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
