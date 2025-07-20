# Solarman Configuration

To connect to your Solarman account, you need to update the credentials in the `SolarmanService` class in `Program.cs`.

## Required Information:

1. **Email**: Your Solarman account email
2. **Password**: Your Solarman account password  
3. **App ID**: From Solarman developer portal
4. **App Secret**: From Solarman developer portal

## Steps to Configure:

1. **Get API Credentials**:
   - Visit the Solarman developer portal
   - Create a new application to get App ID and App Secret

2. **Update Program.cs**:
   ```csharp
   private const string SolarmanEmail = "your-actual-email@example.com";
   private const string SolarmanPassword = "your-actual-password";
   private const string SolarmanAppId = "your-actual-app-id";
   private const string SolarmanAppSecret = "your-actual-app-secret";
   ```

## Current Behavior:

- If authentication fails or credentials are not configured, the app will display **mock solar data**
- Mock data shows a realistic solar production curve from 6 AM to 6 PM
- The chart updates every 30 seconds along with your SmartThings devices

## Features Added:

- **Solar Production Chart**: Line chart showing today's power generation
- **Real-time Updates**: Chart refreshes automatically
- **Responsive Design**: Chart adapts to screen size
- **Error Handling**: Graceful fallback to mock data

## Testing:

You can test the application with mock data before configuring real Solarman credentials. The mock data will show a realistic solar production pattern for testing the UI.
