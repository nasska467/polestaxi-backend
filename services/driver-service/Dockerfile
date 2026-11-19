FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/ shared/
COPY services/driver-service/ services/driver-service/
RUN dotnet publish services/driver-service/PolesTaxi.DriverService.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8083 9083
ENTRYPOINT ["dotnet", "PolesTaxi.DriverService.dll"]
