# Stage 1: Build and restore
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy just the .csproj files first to allow Docker caching of restore
COPY *.sln .
COPY global.json .
COPY Directory.Build.props .
COPY NuGet.config .
COPY src/Common/*.csproj src/Common/
COPY src/Core/*.csproj src/Core/
COPY src/Infrastructure/*.csproj src/Infrastructure/
COPY src/RealTrading/*.csproj src/RealTrading/
COPY src/StrategySearch/*.csproj src/StrategySearch/
COPY src/TradingSystem.Console/*.csproj src/TradingSystem.Console/

# Restore packages
RUN dotnet restore

# Now copy the rest of the code
COPY src/. src/

# Build and publish
RUN dotnet build -c Release --no-restore
RUN dotnet publish src/TradingSystem.Console/TradingSystem.Console.csproj -c Release -o /app/publish --no-restore

# Install EF Core tools and apply migrations
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    netcat-openbsd \
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

# Wait for database and run migrations
COPY scripts/wait-for-it.sh /app/wait-for-it.sh
RUN chmod +x /app/wait-for-it.sh

# Set the entry point
ENTRYPOINT ["/app/wait-for-it.sh", "postgres:5432", "--", "dotnet", "TradingSystem.Console.dll"]
