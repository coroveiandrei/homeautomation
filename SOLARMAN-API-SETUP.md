# How to Get Solarman App ID and App Secret

## Step 1: Register as a Solarman Developer

1. **Visit the Solarman Developer Portal**:
   - Go to: https://developer.solarmanpv.com/
   - Or: https://globalapi.solarmanpv.com/

2. **Create a Developer Account**:
   - Click on "Sign Up" or "Register"
   - Use the same email as your Solarman app account
   - Complete the registration process

## Step 2: Create a New Application

1. **Login to the Developer Console**:
   - Use your developer account credentials
   
2. **Create New App**:
   - Look for "Create App" or "New Application" button
   - Fill in the application details:
     - **App Name**: "SmartThings Home Automation" (or your preferred name)
     - **App Description**: "Home automation dashboard with solar monitoring"
     - **App Type**: Usually "Third Party Application"
     - **Redirect URI**: Can use `http://localhost` for development

3. **Submit Application**:
   - Submit for approval (this may take 1-3 business days)
   - You'll receive an email when approved

## Step 3: Get Your Credentials

Once approved, you'll see your application in the developer console:

1. **App ID**: Usually a numeric ID or alphanumeric string
2. **App Secret**: A long secret key string
3. **API Documentation**: Links to API docs and endpoints

## Alternative: Use Solarman Smart API

If the main developer portal is difficult to access, try:

1. **Solarman Smart Developer Portal**:
   - https://api.solarmanpv.com/
   - Some regions have different portals

2. **Contact Solarman Support**:
   - Email: support@solarmanpv.com
   - Explain you need API access for personal home automation
   - Request developer account setup

## Common Issues

### Portal Access
- **Different regions** may have different developer portals
- Try: `.com`, `.cn`, or your local country domain
- Check if your Solarman app shows a developer section

### Account Verification
- Use the **same email** as your Solarman mobile app account
- Some portals require **phone verification**
- Business verification may be required in some regions

### API Approval
- Applications may need **manual approval**
- Approval can take 1-7 business days
- You'll get email notification when ready

## Testing Without Real Credentials

While waiting for approval, your app will work with **mock solar data**:

```csharp
// Current fallback in your code
private const string SolarmanAppId = "your-app-id"; // Keep as-is for now
private const string SolarmanAppSecret = "your-app-secret"; // Keep as-is for now
```

The app automatically detects missing credentials and shows realistic mock data instead.

## Update Credentials When Ready

Once you have your credentials, update these lines in `Program.cs`:

```csharp
private const string SolarmanAppId = "12345"; // Your actual App ID
private const string SolarmanAppSecret = "abcd1234..."; // Your actual App Secret
```

## Verification Steps

After updating credentials:

1. **Run your app**: `dotnet run`
2. **Check browser console** (F12) for any API errors
3. **Monitor the chart** - real data should replace mock data
4. **Check logs** - authentication success/failure messages

Let me know if you need help with any specific step or encounter issues with the developer portal access!
