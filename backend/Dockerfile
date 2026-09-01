FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/PlumeriaStore.Api/PlumeriaStore.Api.csproj src/PlumeriaStore.Api/
RUN dotnet restore src/PlumeriaStore.Api/PlumeriaStore.Api.csproj

COPY src/PlumeriaStore.Api/ src/PlumeriaStore.Api/
RUN dotnet publish src/PlumeriaStore.Api/PlumeriaStore.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# db/ and uploads/ are created at startup (see Program.cs) if missing - declared here so a
# volume can be mounted over them without needing the image to pre-seed anything into it.
VOLUME ["/app/db", "/app/uploads"]

EXPOSE 8080

# appsettings.json's "Urls" (loopback-only, correct for local `dotnet run`) otherwise wins over
# ASPNETCORE_URLS here - passing --urls instead, since command-line args are the one config
# source with higher precedence than appsettings.json, to bind all interfaces inside the container.
ENTRYPOINT ["dotnet", "PlumeriaStore.Api.dll", "--urls", "http://+:8080"]
