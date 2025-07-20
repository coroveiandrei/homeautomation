# Azure Container Registry Deployment Guide

## Prerequisites
- Azure CLI installed and configured
- Docker installed and running
- Access to an Azure Container Registry

## Step-by-Step Commands

### 1. Replace ACR Name
Replace `your-acr-name` in the commands below with your actual ACR name.

### 2. Login to Azure
```bash
az login
```

### 3. Login to ACR
```bash
az acr login --name your-acr-name
```

### 4. Build Docker Image
```bash
docker build -t smartthings-webapp .
```

### 5. Tag Image for ACR
```bash
# Tag with latest
docker tag smartthings-webapp your-acr-name.azurecr.io/smartthings-webapp:latest

# Tag with version
docker tag smartthings-webapp your-acr-name.azurecr.io/smartthings-webapp:v1.0
```

### 6. Push to ACR
```bash
# Push latest tag
docker push your-acr-name.azurecr.io/smartthings-webapp:latest

# Push version tag
docker push your-acr-name.azurecr.io/smartthings-webapp:v1.0
```

### 7. Verify Deployment
```bash
# List repositories in ACR
az acr repository list --name your-acr-name --output table

# Show tags for your repository
az acr repository show-tags --name your-acr-name --repository smartthings-webapp --output table
```

## Quick Deployment Scripts

### Windows (PowerShell)
```powershell
.\deploy-to-acr.ps1
```

### Linux/Mac (Bash)
```bash
chmod +x deploy-to-acr.sh
./deploy-to-acr.sh
```

## Example with Real ACR Name
If your ACR name is `mycompanyacr`, the commands would be:

```bash
# Login
az acr login --name mycompanyacr

# Tag
docker tag smartthings-webapp mycompanyacr.azurecr.io/smartthings-webapp:latest

# Push
docker push mycompanyacr.azurecr.io/smartthings-webapp:latest

# Verify
az acr repository show-tags --name mycompanyacr --repository smartthings-webapp --output table
```

## Container App Deployment
Once pushed to ACR, you can deploy to Azure Container Apps:

```bash
# Create Container App
az containerapp create \
  --name smartthings-webapp \
  --resource-group your-resource-group \
  --environment your-container-env \
  --image your-acr-name.azurecr.io/smartthings-webapp:latest \
  --target-port 8080 \
  --ingress external
```

## Clean Up Local Images (Optional)
```bash
docker rmi smartthings-webapp
docker rmi your-acr-name.azurecr.io/smartthings-webapp:latest
docker rmi your-acr-name.azurecr.io/smartthings-webapp:v1.0
```
