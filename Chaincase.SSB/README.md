# Chaincase.SSB
## Run Chaincase in the Browser for Development

Chaincase.SSB is a [Server Side Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-5.0#blazor-server) project that allows one to interface with a near-complete Chaincase app in the browser. This is very convenient when developing the UI since you have all the development tools of the browser available. Since the browser is a vulnerable environment and the SSB project forgoes Tor, **Chaincase.SSB is fit for development purposes only**.

## How to Run
After you [install Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) and .NET, you can run the project from Visual Studio.

Make sure you have the WalletWasabi submodule installed

```sh
git submodule update --init --recursive
```
From this Chaincase.SSB directory you can build and run the .NET project

```sh
dotnet restore
dotnet build
dotnet run
```

## Watch Chaincase.SSB in Action
[![play Chaincase SSB thumbnail](https://user-images.githubusercontent.com/8525467/125820858-8288815c-ee28-472f-8c9b-52f0f8b7574f.png)](https://youtu.be/IYjBt7eiWk0)
