#!/bin/bash

# Generate a 64-byte random key for JWT and encode it as base64
JWT_SECRET=$(openssl rand -base64 64)

# Generate a 32-byte random key for API and encode it as base64
API_KEY=$(openssl rand -base64 32)

# Function to generate a secure password
generate_password() {
    openssl rand -base64 12 | tr -dc 'a-zA-Z0-9!@#$%^&*' | head -c 16
}

# Generate passwords for all services
GRAFANA_PASSWORD=$(generate_password)
POSTGRES_PASSWORD=$(generate_password)
REDIS_PASSWORD=$(generate_password)
PGADMIN_PASSWORD=$(generate_password)

echo "Generated Secrets:"
echo
echo "=== Security Keys ==="
echo "JWT Secret Key (for Security.JwtSecret):"
echo $JWT_SECRET
echo
echo "API Key (for Security.ApiKey):"
echo $API_KEY
echo
echo "=== Database Credentials ==="
echo "PostgreSQL Username: tradinguser"
echo "PostgreSQL Password (for Database.Password):"
echo $POSTGRES_PASSWORD
echo
echo "=== Redis Credentials ==="
echo "Redis Password (for Redis.Password):"
echo $REDIS_PASSWORD
echo
echo "=== Monitoring Credentials ==="
echo "Grafana Admin Password (for Monitoring.GrafanaAdminPassword):"
echo $GRAFANA_PASSWORD
echo
echo "=== PgAdmin Credentials ==="
echo "PgAdmin Email: admin@tradingsystem.com"
echo "PgAdmin Password (for PgAdmin.Password):"
echo $PGADMIN_PASSWORD
echo
echo "Copy these values into your config/secrets.json file in their respective fields:"
echo "1. Copy the JWT Secret Key into the 'Security.JwtSecret' field"
echo "2. Copy the API Key into the 'Security.ApiKey' field"
echo "3. Copy the PostgreSQL password into the 'Database.Password' field"
echo "4. Copy the Redis password into the 'Redis.Password' field"
echo "5. Copy the Grafana password into the 'Monitoring.GrafanaAdminPassword' field"
echo "6. Copy the PgAdmin password into the 'PgAdmin.Password' field"
echo
echo "Make sure to keep these credentials secure and never share them or commit them to version control."
