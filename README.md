# Smart Home Automation Dashboard

A comprehensive home automation dashboard that integrates with SmartThings, Solarman solar panels, and Bosch smart home devices.

## Features

- **SmartThings Integration**: Monitor and control Samsung SmartThings devices
- **Solarman Solar Monitoring**: Track solar panel production with interactive charts
- **Bosch Device Support**: Connect to Bosch Smart Home Controller and IoT Hub devices
- **Web Dashboard**: Modern responsive web interface
- **Docker Support**: Containerized deployment with multi-stage builds
- **Azure Container Registry**: Ready for cloud deployment

## Prerequisites

- .NET 9.0 SDK
- Docker (optional, for containerized deployment)
- SmartThings Personal Access Token
- Solarman API credentials (optional)
- Bosch Smart Home Controller or IoT Hub access (optional)

## Setup

### 1. Clone and Configure Environment

```bash
git clone <your-repo-url>
cd homeautomation
```

### 2. Environment Configuration

Copy the example environment file and configure your credentials:

```bash
cp .env.example .env
```

Edit `.env` with your actual credentials:

```env
# SmartThings API Configuration (Required)
SMARTTHINGS_PERSONAL_ACCESS_TOKEN=your-smartthings-token-here

# Solarman API Configuration (Optional)
SOLARMAN_EMAIL=your-email@example.com
SOLARMAN_PASSWORD=your-password
SOLARMAN_APP_ID=your-solarman-app-id
SOLARMAN_APP_SECRET=your-solarman-app-secret

# Bosch Smart Home Configuration (Optional)
BOSCH_CONTROLLER_IP=192.168.1.100
BOSCH_CLIENT_NAME=MyHomeApp
BOSCH_CERTIFICATE_PATH=/path/to/certificate
BOSCH_CERTIFICATE_PASSWORD=certificate-password

# Bosch IoT Hub Configuration (Optional)
BOSCH_IOT_HUB_URL=https://api.bosch-iot-suite.com/hub/1
BOSCH_API_KEY=your-bosch-iot-api-key
```

### 3. Running the Application

#### Local Development

```bash
dotnet run
```

The application will be available at `http://localhost:5000`

#### Docker Deployment

```bash
# Build the Docker image
docker build -t homeautomation .

# Run the container
docker run -p 8080:8080 --env-file .env homeautomation
```

#### Azure Container Registry Deployment

```bash
# Build and push to ACR (configure ACR_NAME and RESOURCE_GROUP in the scripts)
./build-docker.sh
./deploy-to-acr.sh
```

## API Endpoints

- `GET /` - Web dashboard
- `GET /api/devices` - Combined device list from all services
- `GET /api/bosch/devices` - Bosch devices only
- `GET /api/solar/today` - Today's solar production data
- `POST /api/devices/{deviceId}/command` - Send command to SmartThings device

## Getting API Credentials

### SmartThings Personal Access Token

1. Go to [SmartThings Developer Portal](https://developer.smartthings.com/)
2. Sign in with your Samsung account
3. Go to Personal Access Tokens
4. Create a new token with device permissions
5. Copy the token to your `.env` file

### Solarman API Credentials

1. Register at [Solarman Developer Portal](https://developer.solarmanpv.com/)
2. Create a new application
3. Get your App ID and App Secret
4. Use your Solarman account email and password

### Bosch Smart Home Controller

1. Ensure your Bosch Smart Home Controller is on your network
2. Generate client certificates using Bosch's pairing process
3. Configure the controller IP and certificate paths

### Bosch IoT Hub

1. Register at [Bosch IoT Suite](https://www.bosch-iot-suite.com/)
2. Create an IoT Hub service
3. Generate API credentials

## Development

### Project Structure

```
├── Program.cs              # Main application entry point
├── Services/
│   ├── SmartThingsService.cs   # SmartThings API integration
│   ├── SolarmanService.cs      # Solarman solar data
│   └── BoschService.cs         # Bosch device integration
├── Dockerfile              # Multi-stage Docker build
├── .env.example            # Environment variables template
└── README.md              # This file
```

### Adding New Device Types

1. Create a new service in the `Services/` directory
2. Register the service in `Program.cs`
3. Add API endpoints for the new service
4. Update the web interface to display new device types

## Security Notes

- Never commit `.env` files to version control
- Use environment variables for all sensitive configuration
- The `.gitignore` file excludes sensitive files by default
- Consider using Azure Key Vault or similar for production secrets

## Mock Data

If API credentials are not configured, the application will fall back to mock data:
- SmartThings: Returns empty device list
- Solarman: Generates realistic solar production curves
- Bosch: Shows sample devices with mock status

## Troubleshooting

### Common Issues

1. **Empty device list**: Check your API credentials in `.env`
2. **Solar data not loading**: Verify Solarman credentials and API access
3. **Bosch devices offline**: Ensure controller IP is correct and accessible
4. **Build errors**: Ensure .NET 9.0 SDK is installed

### Logging

The application logs to console. In Docker, view logs with:
```bash
docker logs <container-id>
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Update documentation
5. Submit a pull request

## License

This project is licensed under the MIT License.
