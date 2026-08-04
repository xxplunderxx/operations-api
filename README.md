# Operations API

A self-contained modular ASP.NET Core monolith for the supplied wind-farm CSV data. It uses an immutable, startup-loaded in-memory store: `Infrastructure` loads and safely skips malformed rows, `Application` owns response shaping, aggregation, and alert rules, and `Domain` holds source entities. No database, authentication, or cloud services are required.

The canonical route base is `/api/operations`, as defined by the OpenAPI server URL. `/api` aliases are included for the earlier frontend contract. `GET /health` reports process health; browse Swagger UI at `/swagger` or the source-authoritative OpenAPI document at `/openapi/v1.yaml`.

## Run locally

The installed LTS is .NET 10 (the project targets `net10.0`). CSV assets are copied into the build output.

```sh
cd operations-api
dotnet run --project OperationsApi --urls http://localhost:8080
```

```sh
curl http://localhost:8080/health
curl http://localhost:8080/api/operations/dashboard
curl http://localhost:8080/api/operations/farms/FARM01
curl http://localhost:8080/api/operations/turbines/TURB001
curl http://localhost:8080/api/operations/alerts
```

## Test

```sh
cd operations-api
dotnet test OperationsApi.Tests/OperationsApi.Tests.csproj
```

## Docker

From the repository root:

```sh
docker build -f operations-api/Dockerfile -t operations-api .
docker run --rm -p 8080:8080 operations-api
```

Or from `operations-api`, run `docker compose up --build`.

## Data and alerts

Only parsed telemetry rows referencing a known farm and turbine contribute to summaries; farms without telemetry are returned by the farm route but absent from dashboard summaries. Raw measurement and receipt timestamps are retained. Rows with parsing errors are logged and skipped rather than terminating startup.

Thresholds and cadence are centralized in `Application/AlertRules.cs`: critical alerts are zero power at wind speeds at least 10 m/s and gearbox temperature above 100 °C. Warning data-quality alerts identify adjacent measurements more than five minutes apart, and arrival more than ten minutes after measurement. Alerts sort by measurement timestamp, then turbine ID, then category; series sort by timestamp then receipt time. These are presentation alerts, not a claim that other unusual-but-valid readings are invalid.

For production scale, replace the startup store with durable, indexed time-series storage, ingest asynchronously, add authenticated tenant-aware APIs and observability, and evaluate alert state incrementally rather than recalculating it per request.
