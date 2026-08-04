FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY operations-api/Directory.Build.props operations-api/Directory.Packages.props operations-api/Operations.sln operations-api/
COPY operations-api/src/Operations.Domain/Operations.Domain.csproj operations-api/src/Operations.Domain/
COPY operations-api/src/Operations.Api/Operations.Api.csproj operations-api/src/Operations.Api/
RUN dotnet restore operations-api/src/Operations.Api/Operations.Api.csproj
COPY operations-api/src/ operations-api/src/
COPY api-specs/operations-api.yaml api-specs/operations-api.yaml
RUN dotnet publish operations-api/src/Operations.Api/Operations.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Operations.Api.dll"]
