# M4222-WeighBridgeReader
## Information

This project is used to connect a local weighbridge to a PowerApps Dataverse enviroment.  This app requires:
* API access to the PowerApps API
* Client Token
* Access to the weighbridge to receive data

Currently this project is only able to communicate with a M4222 Weighbridge network adapter.  However, this can be updated to work with other weighbridges if needed.


## Setup

The project needs a appsettings.json file, this needs the following details.

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "EventLog": {
    "SourceName": "Weighbridge Weight Logger",
    "LogName": "Application",
    "LogLevel": {
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Security": {
    "AuthenticationURL": "",   --- This is the URL for the app to fetch the authentication token to access the API"
    "ClientId": "",  --- This is the Client ID
    "ClientSecret": "",  --- This is the Client secret
    "Scope": ""  --- This is the scope to allow access to the API
  },
  "PostTimeoutSeconds": 10,  ---  This is how long the program will try to connect to the API before it gives up if it does not hear a responce
  "PostURL": "https://domain.com",  --- This is the URL to contact the API
  "EnviromentTableName": "",  --- This is the enviroment name, this is for Powerapps
  "WeighbridgeTableName": "",  --- This is the table name, this is for Powerapps
  "WeighBridges": [
    {
      "WeighbridgeName": "Name",   --- This is a freindly name for the Weight Bridge
      "WeighbridgeGUID": "",  --- This is the GUID ID for the weighbridge saved in PowerApps
      "IP": "192.168.0.1",  --- This is the IP of the weighbridge used to receive weight information
      "Port": 80  --- This is the port used to receive the weighbridges weight information
    }
  ]
}
```

An Admin will need to setup an account for a connection to the PowerApps API, instructions can be followed here:
https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-create-new-tenant

The program will talk to PowerApps API, details on this can be found here:
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/use-fetchxml-web-api

The Program will make a connection to the API using a URL much like the following, the ```appsettings.json``` file will contain a URL to the API for the specific table it will be saving data to, this URL will look like the following:
https://{enviroment number}.crm6.dynamics.com/api/data/v9.2/{full table name, this will most likly end with an 'es'}

This program can be setup a service, instructions for how to do this (for windows) can be found here:
https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service?pivots=dotnet-6-0#create-the-windows-service
