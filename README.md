# Operations API

A self-contained ASP.NET Core API for the supplied wind-farm CSV data. The solution uses three projects: `Operations.Api` hosts HTTP endpoints and the CSV repository, `Operations.Domain` contains framework-independent business behavior, and `Operations.Tests` provides unit and integration coverage. No database, authentication, or cloud services are required.

The canonical route base is `/api/operations`, as defined by the OpenAPI server URL. `GET /health` reports process health; browse Swagger UI at `/swagger` or the source-authoritative OpenAPI document at `/openapi/v1.yaml`.

## Run locally

The installed LTS is .NET 10 (the project targets `net10.0`). CSV assets are copied into the build output.

```sh
cd operations-api
dotnet run --project src/Operations.Api --urls http://localhost:8080
```

```sh
curl http://localhost:8080/health
curl http://localhost:8080/api/operations/dashboard
curl http://localhost:8080/api/operations/farms/FARM01
curl 'http://localhost:8080/api/operations/turbines/TURB001?metric=powerOutput&page%5Bsize%5D=100'
curl http://localhost:8080/api/operations/alerts
```

## Test

```sh
cd operations-api
dotnet test Operations.sln
```

## Docker

From the repository root:

```sh
docker build -f operations-api/Dockerfile -t operations-api .
docker run --rm -p 8080:8080 operations-api
```

Or from `operations-api`, run `docker compose up --build`.

## Data and alerts

Only parsed telemetry rows referencing a known turbine and that turbine's assigned farm contribute to summaries; farms without telemetry are returned by the farm route but absent from dashboard summaries. Raw measurement and receipt timestamps are retained. Rows with parsing errors are logged and skipped rather than terminating startup. The CSV location defaults to `Data` beneath the application content root and can be changed with `CsvData__DataDirectory`.

Thresholds and cadence are centralized in `Operations.Domain/AlertRules.cs`: critical alerts are zero power at wind speeds at least 10 m/s and gearbox temperature above 100 °C. Warning data-quality alerts identify adjacent measurements more than five minutes apart, and arrival more than ten minutes after measurement. Alerts sort by measurement timestamp, then turbine ID, then category; series sort by timestamp then receipt time. These are presentation alerts, not a claim that other unusual-but-valid readings are invalid.

The turbine endpoint requires `metric=powerOutput`, `metric=windSpeed`, or `metric=gearBoxTemp`. It returns a JSON:API document whose `data` contains telemetry resources, with `links.next` carrying an opaque `page[after]` cursor for the next page. `page[size]` is optional, defaults to 100, and is capped at 500 by `Pagination:MaxPageSize`.

For production scale, replace the startup store with durable, indexed time-series storage, ingest asynchronously, add authenticated tenant-aware APIs and observability, and evaluate alert state incrementally rather than recalculating it per request.
