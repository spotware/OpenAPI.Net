## API Application

To use Spotware Open API you have to first create an open API application, send an activation request to Spotware for your application, and once it got activated you can start using the API through your application.

To create an Open API application go to: [**openapi.ctrader.com**](https://openapi.ctrader.com/){:target="\_blank"}

## Getting Auth Code

After Spotware activated your API application, you can start using it, the first step is to get an authentication code from user, to do that via OpenAPI.NET you can use the code below to get authentication URL:

First add these usings:

```C#
	using OpenAPI.Net;
	using OpenAPI.Net.Auth;
	using OpenAPI.Net.Helpers;
```

Then:

```C#
	// The classes used in this code snippet are located at OpenAPI.Net.Auth
	// Your API application ID
    var appId = "";
	// Your API application secret
    var appSecret = ""; 
	// One of your API applications redirect URI that you want to redirect user
    var redirectUrl = "";
    _app = new App(appId, appSecret, redirectUrl); 
	// The scope of authentication token (Trading or Accounts), Trading is default
    var authUri = _app.GetAuthUri(scope: Scope.Trading);
	// authUri is the authentication URI, open it on browser
    System.Diagnostics.Process.Start("explorer.exe", $"\"{authUri}\"");
```

When user opens the authentication URI on browser he has to enter his cTrader ID credentials and then select the trading accounts he want to authorize your API application to use, after that he will be redirected to your provided Redirect URL and the authentication code will be appended to the redirect URL as a parameter:

```
http://api.algodeveloper.com/redirects/?code=20df253b58df60a4e09f10b45e2ec11dbc0ccc565326d5706a8ea
```

As you can see "http://api.algodeveloper.com/redirects/" is my redirect URI and "20df253b58df60a4e09f10b45e2ec11dbc0ccc565326d5706a8ea" is the authentication code.

To extract the authentication code from redirect URL you can use the AuthCode class.

## Generating Access Token

After you got the user authentication code, you can generate an access token, you have to do it instantly otherwise authentication code will expire after one minute.

To generate an access token via OpenAPI.NET use the following code:

```C#
	// The classes used in this code snippet are located at OpenAPI.Net.Auth
	// Use TokenFactory to get the token
    var token = await TokenFactory.GetToken(authCode, app);
```

Now you have a token object, it has these properties:

* AccessToken: This is the access token, you will use it on your API calls
* RefreshToken: This is the token that you will use to refresh your access token after it expired, refresh token never expires
* ExpiresIn: The expiry time of your access token
* TokenType: Type of access token
* ErrorCode: The error code, if access token is null then use this property to find the cause
* ErrorDescription: The text description of error code

To refresh access token send a ProtoOARefreshTokenReq to OpenClient (API client) with your refresh token.

Now you have an access token, you can use it to take the list of user authenticated trading accounts and then execute trading operations on those accounts or get accounts historical data, for that you have to use OpenClient.