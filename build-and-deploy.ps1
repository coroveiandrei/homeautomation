# PowerShell script to build, tag, and deploy Docker image for SmartThings WebApp

$ErrorActionPreference = 'Stop'

# Variables
$imageName = "smartthings-webapp"
$imageTag = "latest"
$acrName = "magellanacralpha"
$fullImageName = "${acrName}.azurecr.io/${imageName}:${imageTag}"

Write-Host "Building Docker image..."
docker build -t $imageName .

Write-Host "Tagging image for ACR..."
docker tag $imageName $fullImageName

Write-Host "Logging in to Azure Container Registry..."
az acr login --name $acrName

Write-Host "Pushing image to ACR..."
docker push $fullImageName

Write-Host "Image $fullImageName pushed successfully."
