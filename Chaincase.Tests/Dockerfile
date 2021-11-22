
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.415-focal
WORKDIR /src
COPY . .

RUN apt update && apt install nodejs npm libbrotli1 libmbedtls12 -y
RUN dotnet restore "Chaincase.Tests/Chaincase.Tests.csproj"
ENV IN_CI="true"
WORKDIR "/src/Chaincase.Tests"
RUN dotnet build "Chaincase.Tests.csproj"

RUN dotnet playwright install chromium
RUN dotnet playwright install-deps chromium
ENTRYPOINT ["./docker-entrypoint.sh"]


