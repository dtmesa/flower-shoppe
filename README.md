# Plumeria Store

A local-pickup storefront for a plumeria plant business.

- **`frontend/`** — React + TypeScript (Vite) site, organized by feature, where customers browse inventory and submit a "reserve for pickup" request.
- **`backend/`** — ASP.NET Core 8 (C#) Minimal API behind admin login, with CRUD for inventory (including photos) and management of incoming pickup requests. No payment processing — pickup is arranged offline after a request comes in.

## Prerequisites

- .NET 10 SDK (backend)
- Node.js 18+ and npm (frontend)

## Project layout

```
backend/
  PlumeriaStore.sln
  src/PlumeriaStore.Api/     the API itself, organized by feature (Auth/, Inventory/, Reservations/)
  tests/PlumeriaStore.Api.Tests/   xUnit tests (service-layer + real HTTP endpoint tests)
frontend/
  src/features/               each feature owns its components, API hooks, and types
    auth/, inventory/, reservations/
```

## Running the backend

```bash
cd backend
dotnet run --project src/PlumeriaStore.Api
```

The API starts on `http://localhost:8080`. Data is stored in a local SQLite database file at `backend/src/PlumeriaStore.Api/db/plumeriadb.db`, and uploaded photos are saved to `.../uploads/` — both are created automatically on first run (tables are created via EF Core migrations at startup), and both are git-ignored.

Run the test suite with:

```bash
cd backend
dotnet test
```

### Admin login

A single admin account is seeded (and re-synced) on every startup from configuration:

| Setting | Default |
|---|---|
| `App:Admin:Username` | `admin` |
| `App:Admin:Password` | `admin` |

**Change the password before any real use.** Configuration follows the standard ASP.NET Core pattern: `appsettings.json` holds the defaults, and any value can be overridden with an environment variable using the `__` (double underscore) separator for nested keys, e.g.:

```bash
App__Admin__Username=myuser App__Admin__Password=my-strong-password dotnet run --project src/PlumeriaStore.Api
```

Other configurable settings (all optional, see `backend/src/PlumeriaStore.Api/appsettings.json`):

| Setting | Purpose | Default |
|---|---|---|
| `Urls` (or `ASPNETCORE_URLS` env var) | Backend listen address | `http://localhost:8080` |
| `ConnectionStrings:Default` | SQLite connection string | `Data Source=./db/plumeriadb.db` |
| `App:Upload:Directory` | Where uploaded photos are stored | `./uploads` |
| `App:Upload:MaxSizeBytes` | Max photo upload size | `5242880` (5MB) |
| `App:Jwt:Secret` | Signing key for admin session tokens | dev-only default — **set this in production** |
| `App:Jwt:ExpirationMinutes` | Admin token lifetime | `720` (12h) |
| `App:Cors:AllowedOrigin` | Allowed frontend origin | `http://localhost:5173` |

## Running the frontend

```bash
cd frontend
npm install
npm run dev
```

Opens at `http://localhost:5173`. It talks to the backend via `VITE_API_BASE_URL`, set in `frontend/.env` (copy `frontend/.env.example` if you need to point it elsewhere, e.g. a deployed backend URL).

## Using it

- Public site (`/`): browse inventory, filter by type/color/search, click an item to view photos and details, and submit a pickup request.
- Admin (`/admin/login`): log in with the admin credentials above, then manage inventory (add/edit/delete items, upload/remove photos) under `/admin/inventory` and view/update pickup requests under `/admin/reservations`.
