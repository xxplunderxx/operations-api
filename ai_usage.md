Act as a senior .NET backend engineer. In this repository, build a runnable, containerized C# monolith that implements the API contract in `api-specs/operations-api.yaml`.

First, read these files completely and treat the OpenAPI specification as the response-schema authority:

- `api-specs/operations-api.yaml`
- `energy-dashboard/docs/api-contract.md`
- `energy-dashboard/take_home_exercise_updated.md`
- `energy-dashboard/docs/initial-data-profile.md`
- `energy-dashboard/Data/farms.csv`
- `energy-dashboard/Data/turbines.csv`
- `energy-dashboard/Data/telemetry.csv`

Goal: implement a self-contained ASP.NET Core Web API that runs locally in Docker and serves all endpoints specified by the contract. Keep it a modular monolith: clear domain/application/infrastructure boundaries are welcome, but do not introduce distributed services, external databases, authentication, cloud dependencies, or unnecessary infrastructure.

Requirements:

1. API behavior
   - Implement:
     - `GET /api/operations/dashboard`
     - `GET /api/operations/farms/{farmId}`
     - `GET /api/operations/turbines/{turbineId}`
     - `GET /api/operations/alerts`
   - Also provide `/api/...` aliases if needed for compatibility with `energy-dashboard/docs/api-contract.md`, but document which route is canonical.
   - Match JSON field names, shapes, number types, required fields, and 404 error shape in the OpenAPI contract exactly.
   - Return a structured `{ code, message }` response for missing farm/turbine IDs.
   - Use UTC ISO-8601 timestamps.

2. Data ingestion and correctness
   - Read the supplied CSV files at application startup (or lazily with safe caching), using paths that work both locally and in the Docker image.
   - Do not change or fabricate CSV data.
   - Build farm, turbine, telemetry, and response models from the supplied data.
   - Summaries must use only valid telemetry rows from the CSV; do not invent readings or summaries for farms without telemetry.
   - Preserve raw measurement timestamps and `received_at` for alert/data-quality evaluation.
   - Make parsing robust: report/skip malformed rows safely and avoid crashing the whole API.

3. Calculations
   - Dashboard: fleet average power, fleet average wind, fleet critical-alert count, and per-reporting-farm summaries.
   - Farm: requested farm metadata and all mapped turbine IDs, including farms with no telemetry.
   - Turbine: identity/farm context, averages, critical-alert count, chronological power-output series (`kW`) and wind-speed series (`m/s`).
   - Alerts: return explainable alert presentation records derived from telemetry. At minimum include:
     - critical: zero power when wind speed is >= 10 m/s;
     - critical: gearbox temperature > 100°C;
     - data-quality/freshness alerts for missing 5-minute reporting intervals and telemetry received more than 10 minutes after measurement.
   - Do not label valid-but-unusual data as invalid. Sort telemetry series and alerts chronologically, and define/document a deterministic ordering if timestamps tie.
   - Centralize alert thresholds/rules so they are easy to change.

4. Engineering quality
   - Use .NET 8 LTS (or the installed supported LTS SDK).
   - Prefer controllers or minimal APIs consistently; keep business logic out of endpoint handlers.
   - Enable OpenAPI/Swagger for local inspection.
   - Add health endpoint such as `GET /health`.
   - Configure CORS only as needed for local frontend development; do not use unrestricted behavior unless justified.
   - Add meaningful logging and clear startup failures when data files cannot be located.
   - Do not modify the frontend unless it is required to point it at the real API; if modified, replace only mock-data integration and preserve presentation behavior.

5. Tests
   - Add automated tests for CSV loading, dashboard aggregates, missing-resource 404s, turbine series structure/order, and all alert categories.
   - Use the seed data’s documented expected behavior as validation:
     - 6 operational critical readings;
     - 30 missing-interval gaps;
     - 312 late-arrival records (>10 minutes);
     - two turbines with telemetry, while other farms remain without manufactured summaries.
   - Run the test suite and fix failures before finishing.

6. Containerization and documentation
   - Add a multi-stage `Dockerfile` that builds and runs the API without requiring the host .NET SDK at runtime.
   - Add `docker-compose.yml` if it makes local usage simpler, but do not require external services.
   - Ensure CSV files are included in the image and reliably discoverable.
   - Expose a documented local port, preferably `8080`.
   - Update the API README with:
     - architecture and assumptions;
     - exact build/run commands;
     - Docker commands;
     - endpoint examples using curl;
     - how alert rules and data quality are evaluated;
     - tradeoffs and next steps for production scale.

Before editing, briefly state your implementation plan. After editing, provide:
- files created/changed;
- test commands and results;
- Docker build/run commands;
- sample curl commands for every required route;
- any contract ambiguity you resolved, especially the `/api/operations` vs `/api` path discrepancy.

Do not claim a route, test, or Docker command works unless you actually run and verify it.