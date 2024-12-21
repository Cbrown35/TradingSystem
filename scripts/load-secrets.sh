#!/bin/bash

# Check if secrets file exists
if [ ! -f "./config/secrets.json" ]; then
    echo "Error: secrets.json not found!"
    echo "Please copy config/secrets.template.json to config/secrets.json and fill in your secrets"
    exit 1
fi

# Export environment variables from secrets.json
export DB_NAME=$(jq -r '.Database.Name' ./config/secrets.json)
export DB_USER=$(jq -r '.Database.User' ./config/secrets.json)
export DB_PASSWORD=$(jq -r '.Database.Password' ./config/secrets.json)
export REDIS_PASSWORD=$(jq -r '.Redis.Password' ./config/secrets.json)
export PGADMIN_EMAIL=$(jq -r '.PgAdmin.Email' ./config/secrets.json)
export PGADMIN_PASSWORD=$(jq -r '.PgAdmin.Password' ./config/secrets.json)
export JWT_SECRET=$(jq -r '.Security.JwtSecret' ./config/secrets.json)
export API_KEY=$(jq -r '.Security.ApiKey' ./config/secrets.json)

# Create connection strings
export ConnectionStrings__DefaultConnection="Host=postgres;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
export Redis__ConnectionString="redis:6379,password=${REDIS_PASSWORD}"

echo "Secrets loaded successfully!"
