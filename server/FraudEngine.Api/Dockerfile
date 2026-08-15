# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FraudEngine.Api/FraudEngine.Api.csproj", "FraudEngine.Api/"]
COPY ["FraudEngine.Core/FraudEngine.Core.csproj", "FraudEngine.Core/"]
RUN dotnet restore "FraudEngine.Api/FraudEngine.Api.csproj"
COPY . .
WORKDIR "/src/FraudEngine.Api"
RUN dotnet publish "FraudEngine.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS="http://+:80"
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FraudEngine.Api.dll"]
