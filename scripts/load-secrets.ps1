# Check if secrets file exists
if (-not (Test-Path "./config/secrets.json")) {
    Write-Error "Error: secrets.json not found!"
    Write-Host "Please copy config/secrets.template.json to config/secrets.json and fill in your secrets"
    exit 1
}

# Read secrets.json
$secrets = Get-Content "./config/secrets.json" | ConvertFrom-Json

# Export environment variables from secrets.json
$env:DB_NAME = $secrets.Database.Name
$env:DB_USER = $secrets.Database.User
$env:DB_PASSWORD = $secrets.Database.Password
$env:REDIS_PASSWORD = $secrets.Redis.Password
$env:PGADMIN_EMAIL = $secrets.PgAdmin.Email
$env:PGADMIN_PASSWORD = $secrets.PgAdmin.Password
$env:JWT_SECRET = $secrets.Security.JwtSecret
$env:API_KEY = $secrets.Security.ApiKey

# Create connection strings
$env:ConnectionStrings__DefaultConnection = "Host=postgres;Database=$($env:DB_NAME);Username=$($env:DB_USER);Password=$($env:DB_PASSWORD)"
$env:Redis__ConnectionString = "redis:6379,password=$($env:REDIS_PASSWORD)"

Write-Host "Secrets loaded successfully!"
