# STAGE 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# STAGE 2: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files using the new 'src' paths
COPY ["src/AuthMotion.API/AuthMotion.API.csproj", "src/AuthMotion.API/"]
COPY ["src/AuthMotion.Application/AuthMotion.Application.csproj", "src/AuthMotion.Application/"]
COPY ["src/AuthMotion.Domain/AuthMotion.Domain.csproj", "src/AuthMotion.Domain/"]
COPY ["src/AuthMotion.Infrastructure/AuthMotion.Infrastructure.csproj", "src/AuthMotion.Infrastructure/"]

# Restore using the API project path
RUN dotnet restore "src/AuthMotion.API/AuthMotion.API.csproj"

# Copy all source code
COPY . .

# Build the API
WORKDIR "/src/src/AuthMotion.API"
RUN dotnet build "AuthMotion.API.csproj" -c Release -o /app/build

# STAGE 3: Publish
FROM build AS publish
RUN dotnet publish "AuthMotion.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthMotion.API.dll"]