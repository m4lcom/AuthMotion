# Etapa 1: Base (Runtime .NET 9)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Etapa 2: Build (SDK .NET 9)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restauración en capa separada para optimizar caché
COPY ["AuthMotion.API/AuthMotion.API.csproj", "AuthMotion.API/"]
COPY ["AuthMotion.Application/AuthMotion.Application.csproj", "AuthMotion.Application/"]
COPY ["AuthMotion.Domain/AuthMotion.Domain.csproj", "AuthMotion.Domain/"]
COPY ["AuthMotion.Infrastructure/AuthMotion.Infrastructure.csproj", "AuthMotion.Infrastructure/"]

RUN dotnet restore "AuthMotion.API/AuthMotion.API.csproj"

# Copiamos el resto del código y compilamos
COPY . .
WORKDIR "/src/AuthMotion.API"
RUN dotnet build "AuthMotion.API.csproj" -c Release -o /app/build

# Etapa 3: Publish
FROM build AS publish
RUN dotnet publish "AuthMotion.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthMotion.API.dll"]