# Use .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Restore dependencies
RUN dotnet restore "Gateway.Api/Gateway.Api.csproj"

# Build and publish
RUN dotnet publish "Gateway.Api/Gateway.Api.csproj" -c Release -o /app/publish

# Use .NET 9 runtime image for final container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
