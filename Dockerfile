# Stage 1: Build and restore (writable)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy NuGet config and solution files first
COPY *.sln .
COPY global.json .
COPY Directory.Build.props .
COPY NuGet.config .

# Copy project files first for better layer caching
COPY src/Common/*.csproj src/Common/
COPY src/Core/*.csproj src/Core/
COPY src/Infrastructure/*.csproj src/Infrastructure/
COPY src/RealTrading/*.csproj src/RealTrading/
COPY src/StrategySearch/*.csproj src/StrategySearch/
COPY src/TradingSystem.Console/*.csproj src/TradingSystem.Console/

# Restore packages in writable stage
RUN dotnet restore --verbosity detailed

# Copy the rest of the source code
COPY src/. src/

# Build in writable stage
RUN dotnet build -c Release --no-restore
RUN dotnet publish src/TradingSystem.Console/TradingSystem.Console.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime (can be read-only)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Create volume mount points
VOLUME ["/app/data", "/app/logs", "/app/config"]

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
    CMD curl -f http://localhost/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "TradingSystem.Console.dll"]
