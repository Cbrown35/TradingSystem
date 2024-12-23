# Setup Guide

This guide walks through the process of setting up the Trading System for development and production use.

## Prerequisites

- .NET 6.0 SDK or later
- Docker and Docker Compose
- Git
- jq (for Unix/Linux systems)
- PowerShell (for Windows systems)
- Visual Studio 2022 or VS Code with C# extensions

## Initial Setup

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd tradingsystem
   ```

2. **Configure Secrets**
   ```bash
   # Copy the secrets template
   cp config/secrets.template.json config/secrets.json
   
   # Edit secrets.json with your secure values
   # IMPORTANT: Never commit secrets.json to the repository
   ```

3. **Load Secrets**
   Unix/Linux:
   ```bash
   chmod +x scripts/load-secrets.sh
   source scripts/load-secrets.sh
   ```
   
   Windows PowerShell:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
   . .\scripts\load-secrets.ps1
   ```

4. **Start Services**
   ```bash
   docker-compose up -d
   ```

## Database Setup

TimescaleDB will be automatically initialized with:
- Optimized time-series tables
- Automatic data compression (after 7 days)
- 1-year retention policy
- Proper indexes for performance

## Service Access

After starting the services, you can access:

1. **TimescaleDB**
   - Host: localhost:5432
   - Database: from secrets.json
   - Username: from secrets.json
   - Password: from secrets.json

2. **Redis**
   - Host: localhost:6379
   - Password: from secrets.json

3. **pgAdmin**
   - URL: http://localhost:5050
   - Email: from secrets.json
   - Password: from secrets.json

## Security Notes

1. **Secrets Management**
   - Never commit secrets.json to the repository
   - Keep your secrets.json secure
   - Regularly rotate passwords in production
   - Use different passwords for development and production

2. **Environment Variables**
   The load-secrets scripts will set up:
   - Database connection strings
   - Redis configuration
   - API keys and JWT secrets
   - Service credentials

3. **Production Deployment**
   For production:
   - Use a secure secrets management service
   - Enable SSL/TLS for all services
   - Use strong, unique passwords
   - Implement proper network security

## Development Tools

Recommended VS Code extensions:
- C# Dev Kit
- Docker
- REST Client
- GitLens

## Troubleshooting

1. **Secrets Issues**
   - Verify secrets.json exists
   - Check environment variables are set
   - Ensure correct file permissions
   - Validate JSON format

2. **Database Connection**
   - Verify TimescaleDB is running
   - Check connection strings
   - Confirm port availability
   - Review database logs

3. **Docker Issues**
   - Check container logs
   - Verify port conflicts
   - Ensure sufficient resources
   - Review Docker daemon logs

## Next Steps

1. Review the [Architecture](Architecture) documentation
2. Explore available [Components](Components)
3. Set up your development environment
4. Run the sample strategies

## Maintenance

1. **Backup Secrets**
   - Keep secure copies of secrets.json
   - Document all credentials
   - Implement backup procedures

2. **Update Passwords**
   ```bash
   # 1. Update secrets.json
   # 2. Reload secrets
   source scripts/load-secrets.sh  # Unix/Linux
   . .\scripts\load-secrets.ps1    # Windows
   # 3. Restart services
   docker-compose down
   docker-compose up -d
   ```

3. **Monitor Logs**
   ```bash
   docker-compose logs -f
