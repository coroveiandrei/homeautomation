# Build the Docker image
Write-Host "Building SmartThings Home Automation Web App Docker image..." -ForegroundColor Green
docker build -t smartthings-webapp .

Write-Host "Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To run the web application:" -ForegroundColor Yellow
Write-Host "docker run --rm -p 8080:8080 smartthings-webapp" -ForegroundColor White
Write-Host ""
Write-Host "Then open your browser to: http://localhost:8080" -ForegroundColor Cyan
