# ASP.NET.Sample

The ASP demo is a Razor pages web app that allows you to interact with Open API by using the OpenAPI.NET.

To use it you have to create a "appsettings-dev.json" configuration file in project root and write your Open API application credentials on it:

```json
{
  "ApiCredentials": {
    "ClientId": "your-application-client-id",
    "Secret": "your-application-client-secret"
  }
}
```
The app will load your Open API application credentials and it will use it.

You have to also add your web app host URL in your API application redirect URIs, otherwise the account authorization will not work and you will get an error.

You can find the application URL on project/properties/launchSettings.json file, you can change it from there if you want to.