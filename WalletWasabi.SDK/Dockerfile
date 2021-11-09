FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /app
EXPOSE 37127

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY . ./
RUN dotnet restore

WORKDIR "/src/WalletWasabi.Backend"
RUN dotnet build "WalletWasabi.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WalletWasabi.Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WalletWasabi.Backend.dll"]
