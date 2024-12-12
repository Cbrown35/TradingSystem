# Use the official .NET SDK image as the build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln .
COPY src/Common/*.csproj ./src/Common/
COPY src/Core/*.csproj ./src/Core/
COPY src/Infrastructure/*.csproj ./src/Infrastructure/
COPY src/RealTrading/*.csproj ./src/RealTrading/
COPY src/StrategySearch/*.csproj ./src/StrategySearch/
COPY src/TradingSystem.Console/*.csproj ./src/TradingSystem.Console/

# Restore NuGet packages
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Run tests
RUN dotnet test -c Release --no-build

# Publish the application
RUN dotnet publish src/TradingSystem.Console/TradingSystem.Console.csproj -c Release -o /app/publish --no-build

# Create the runtime image
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
