FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
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