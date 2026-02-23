# STAGE 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# STAGE 2: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for optimized layer caching
COPY ["AuthMotion.API/AuthMotion.API.csproj", "AuthMotion.API/"]
COPY ["AuthMotion.Application/AuthMotion.Application.csproj", "AuthMotion.Application/"]
COPY ["AuthMotion.Domain/AuthMotion.Domain.csproj", "AuthMotion.Domain/"]
COPY ["AuthMotion.Infrastructure/AuthMotion.Infrastructure.csproj", "AuthMotion.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "AuthMotion.API/AuthMotion.API.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/AuthMotion.API"
RUN dotnet build "AuthMotion.API.csproj" -c Release -o /app/build

# STAGE 3: Publish
FROM build AS publish
RUN dotnet publish "AuthMotion.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 4: Final Production Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthMotion.API.dll"]