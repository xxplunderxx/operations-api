FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY operations-api/OperationsApi/OperationsApi.csproj operations-api/OperationsApi/
RUN dotnet restore operations-api/OperationsApi/OperationsApi.csproj
COPY operations-api/OperationsApi/ operations-api/OperationsApi/
COPY energy-dashboard/Data/ energy-dashboard/Data/
COPY api-specs/operations-api.yaml api-specs/operations-api.yaml
RUN dotnet publish operations-api/OperationsApi/OperationsApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OperationsApi.dll"]
