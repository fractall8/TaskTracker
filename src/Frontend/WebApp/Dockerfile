FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Frontend/WebApp/WebApp.csproj", "src/Frontend/WebApp/"]
COPY ["src/Frontend/Services/Services.csproj", "src/Frontend/Services/"]
COPY ["src/Frontend/Services.Abstractions/Services.Abstractions.csproj", "src/Frontend/Services.Abstractions/"]
COPY ["src/Frontend/Domain/Domain.csproj", "src/Frontend/Domain/"]
COPY ["src/Shared/Contracts/Contracts.csproj", "src/Shared/Contracts/"]

RUN dotnet restore "src/Frontend/WebApp/WebApp.csproj"

COPY . .
WORKDIR "/src/src/Frontend/WebApp"
RUN dotnet publish "WebApp.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

COPY --from=build /app/publish/wwwroot .

COPY src/Frontend/WebApp/nginx.conf /etc/nginx/nginx.conf

COPY src/Frontend/WebApp/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]

CMD ["nginx", "-g", "daemon off;"]

EXPOSE 80