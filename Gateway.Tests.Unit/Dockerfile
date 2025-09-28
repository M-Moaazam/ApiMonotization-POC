FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/UnitTests
RUN dotnet restore
RUN dotnet build --no-restore
CMD ["dotnet", "test", "--logger:trx"]
