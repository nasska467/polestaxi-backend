FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/ shared/
COPY services/order-service/ services/order-service/
RUN dotnet publish services/order-service/PolesTaxi.OrderService.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8084 9084
ENTRYPOINT ["dotnet", "PolesTaxi.OrderService.dll"]
