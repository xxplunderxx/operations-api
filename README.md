# Operations API

A self-contained ASP.NET Core API for the supplied wind-farm CSV data. The solution uses three projects: `Operations.Api` hosts HTTP endpoints and the CSV repository, `Operations.Domain` contains framework-independent business behavior, and `Operations.Tests` provides unit and integration coverage. No database, authentication, or cloud services are required.

The canonical route base is `/api/operations`, as defined by the OpenAPI server URL. `GET /health` reports process health; browse Swagger UI at `/swagger` or the source-authoritative OpenAPI document at `/openapi/v1.yaml`.

## Run locally

The installed LTS is .NET 10 (the project targets `net10.0`). CSV assets are copied into the build output.

```sh
cd operations-api
dotnet run --project src/Operations.Api --urls http://localhost:8080
```

## Api Specs
see the `api-specs` repo for details on how to use the endpoints.

## Test

```sh
cd operations-api
dotnet test Operations.sln
```

## energy dashboard
see `energy-dashboard` repo for details on how to run UI. 

NOTE: Operations-API must be running for the energy dashboard to work properly.

## Docker

From the repository root:

```sh
docker build -f operations-api/Dockerfile -t operations-api .
docker run --rm -p 8080:8080 operations-api
```

Or from `operations-api`, run `docker compose up --build`.
