# Function to generate random key
function Generate-SecureKey {
    param (
        [int]$bytes
    )
    $keyBytes = New-Object byte[] $bytes
    $rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::Create()
    $rng.GetBytes($keyBytes)
    return [Convert]::ToBase64String($keyBytes)
}

# Function to generate password with specific characters
function Generate-Password {
    param (
        [int]$length = 16
    )
    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*"
    $bytes = New-Object byte[] $length
    $rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::Create()
    $rng.GetBytes($bytes)
    
    $password = ""
    for ($i = 0; $i -lt $length; $i++) {
        $password += $chars[$bytes[$i] % $chars.Length]
    }
    return $password
}

# Generate keys and passwords
$jwtSecret = Generate-SecureKey -bytes 64
$apiKey = Generate-SecureKey -bytes 32
$grafanaPassword = Generate-Password
$postgresPassword = Generate-Password
$redisPassword = Generate-Password
$pgadminPassword = Generate-Password

Write-Host "Generated Secrets:`n"

Write-Host "=== Security Keys ==="
Write-Host "JWT Secret Key (for Security.JwtSecret):"
Write-Host $jwtSecret
Write-Host "`nAPI Key (for Security.ApiKey):"
Write-Host $apiKey

Write-Host "`n=== Database Credentials ==="
Write-Host "PostgreSQL Username: tradinguser"
Write-Host "PostgreSQL Password (for Database.Password):"
Write-Host $postgresPassword

Write-Host "`n=== Redis Credentials ==="
Write-Host "Redis Password (for Redis.Password):"
Write-Host $redisPassword

Write-Host "`n=== Monitoring Credentials ==="
Write-Host "Grafana Admin Password (for Monitoring.GrafanaAdminPassword):"
Write-Host $grafanaPassword

Write-Host "`n=== PgAdmin Credentials ==="
Write-Host "PgAdmin Email: admin@tradingsystem.com"
Write-Host "PgAdmin Password (for PgAdmin.Password):"
Write-Host $pgadminPassword

Write-Host "`nCopy these values into your config/secrets.json file in their respective fields:"
Write-Host "1. Copy the JWT Secret Key into the 'Security.JwtSecret' field"
Write-Host "2. Copy the API Key into the 'Security.ApiKey' field"
Write-Host "3. Copy the PostgreSQL password into the 'Database.Password' field"
Write-Host "4. Copy the Redis password into the 'Redis.Password' field"
Write-Host "5. Copy the Grafana password into the 'Monitoring.GrafanaAdminPassword' field"
Write-Host "6. Copy the PgAdmin password into the 'PgAdmin.Password' field"
Write-Host "`nMake sure to keep these credentials secure and never share them or commit them to version control."
