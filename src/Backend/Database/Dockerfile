FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Backend/Database/Database.csproj", "src/Backend/Database/"]
COPY ["src/Backend/Domain/Domain.csproj", "src/Backend/Domain/"]
RUN dotnet restore "src/Backend/Database/Database.csproj"
COPY . .
WORKDIR "/src/src/Backend/Database"
RUN dotnet build "./Database.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Database.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Database.dll"]
