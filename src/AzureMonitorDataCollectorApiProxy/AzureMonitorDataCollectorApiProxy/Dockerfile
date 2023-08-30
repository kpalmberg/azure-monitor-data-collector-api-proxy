FROM mcr.microsoft.com/dotnet/aspnet:6.0.21-alpine3.18-amd64 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:6.0.413-alpine3.18-amd64 AS build
WORKDIR /src
COPY ["AzureMonitorDataCollectorApiProxy/AzureMonitorDataCollectorApiProxy.csproj", "AzureMonitorDataCollectorApiProxy/"]
RUN dotnet restore "AzureMonitorDataCollectorApiProxy/AzureMonitorDataCollectorApiProxy.csproj"
COPY . .
WORKDIR "/src/AzureMonitorDataCollectorApiProxy"
RUN dotnet build "AzureMonitorDataCollectorApiProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureMonitorDataCollectorApiProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureMonitorDataCollectorApiProxy.dll"]