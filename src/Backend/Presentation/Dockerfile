FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Backend/Presentation/Presentation.csproj", "src/Backend/Presentation/"]
COPY ["src/Backend/Application/Application.csproj", "src/Backend/Application/"]
COPY ["src/Backend/Domain/Domain.csproj", "src/Backend/Domain/"]
COPY ["src/Shared/Contracts/Contracts.csproj", "src/Shared/Contracts/"]
COPY ["src/Backend/Infrastructure/Infrastructure.csproj", "src/Backend/Infrastructure/"]
COPY ["src/Backend/Persistence/Persistence.csproj", "src/Backend/Persistence/"]
RUN dotnet restore "src/Backend/Presentation/Presentation.csproj"
COPY . .
WORKDIR "/src/src/Backend/Presentation"
RUN dotnet build "./Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Presentation.dll"]
