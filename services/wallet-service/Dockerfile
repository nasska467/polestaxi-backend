FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/ shared/
COPY services/wallet-service/ services/wallet-service/
RUN dotnet publish services/wallet-service/PolesTaxi.WalletService.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8085 9085
ENTRYPOINT ["dotnet", "PolesTaxi.WalletService.dll"]
