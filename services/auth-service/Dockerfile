FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/ shared/
COPY services/auth-service/ services/auth-service/
RUN dotnet publish services/auth-service/PolesTaxi.AuthService.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8082 9082
ENTRYPOINT ["dotnet", "PolesTaxi.AuthService.dll"]
