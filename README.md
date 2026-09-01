# Flower Shoppe

A local-pickup storefront for a plumeria plant business.

- **`frontend/`** — React + TypeScript (Vite) site, organized by feature, where customers browse inventory, add items to a cart, and submit a pickup request for everything in it.
- **`backend/`** — ASP.NET Core 10 (C#) Minimal API behind admin login, with CRUD for inventory (including photos) and management of incoming pickup requests. No payment processing — pickup is arranged offline after a request comes in.

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
  src/components/             shared UI primitives (Modal, FormError, ConfirmDialog, ...)
  src/lib/                    api client and shared formatters
  src/styles/                 tokens.css -> base.css -> components.css (imported in that order)
  src/features/               each feature owns its components, API hooks, and types
    auth/, cart/, inventory/, reservations/, theme/
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

A single admin account is created from configuration **the first time the app runs against an
empty database**:

| Setting | Default |
|---|---|
| `App:Admin:Username` | `admin` |
| `App:Admin:Password` | `admin` |

These are bootstrap values, not a permanent override. Once the account exists the seeder leaves
it alone, so credentials changed in-app (Admin → Account) survive restarts. To get back to the
configured values, delete the SQLite database file and let it re-seed.

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
| `App:Email:Region` | AWS region used for the SES client | `us-west-2` |

### Email notifications

Copy `backend/.env.example` to `backend/.env` and fill in the values below to get an email at
`EMAIL_FROM_ADDRESS` every time a customer submits a pickup request, sent via AWS SES. `backend/.env`
is git-ignored — it's loaded automatically at startup (via `DotNetEnv`) and isn't committed.

| Key | Purpose |
|---|---|
| `EMAIL_SENDER` | Reserved for selecting a provider later; only `"ses"` is implemented today |
| `EMAIL_FROM_ADDRESS` | Verified SES sender **and** the notification recipient (send-to-self) |
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` | Credentials for an IAM user with `ses:SendEmail` |

While the SES account is in sandbox mode, both `EMAIL_FROM_ADDRESS` and any recipient must be
verified identities in the SES console, or sends will fail (silently, from the customer's
perspective — a failed notification is logged but never blocks the pickup request itself).

## Running the frontend

```bash
cd frontend
npm install
npm run dev
```

Opens at `http://localhost:5173`. It talks to the backend via `VITE_API_BASE_URL`, set in `frontend/.env` (copy `frontend/.env.example` if you need to point it elsewhere, e.g. a deployed backend URL).

## Using it

- Public site (`/`): browse inventory, filter by type/color/size and max price, click an item to view photos and details and add it to your cart, then request pickup for everything in your cart from the cart panel.
- Admin (`/admin/login`): log in with the admin credentials above, then use the dashboard tabs:
  - **Inventory** (`/admin/inventory`) — add/edit/delete items, upload photos and pick which one is the thumbnail.
  - **Pickup Requests** (`/admin/reservations`) — view each request's items and notes, move it through its status, and complete it (either permanently clearing the reserved stock or returning it).
  - **Categories** (`/admin/categories`) — edit the Type/Color/Size options customers filter by. Each carries a one-letter code, and an item's ID tag is those three codes concatenated (e.g. Rooted Plant + Yellow/White + Medium → `RYM`).
  - **Account** (`/admin/account`) — change the admin username and password.
- Light/dark theme: the toggle in the bottom-left corner overrides the system preference and is remembered per browser.

An item's ID tag is derived from its Type/Color/Size, so those three are fixed once an item is
created — creating a second item with the same combination is rejected, with a prompt to raise
the existing item's quantity instead.

### How stock is counted

Each item stores one number, **total** — the units physically on hand. Confirming a pickup request
places a *hold* on some of them rather than decrementing that number:

| | Meaning | Where it shows |
|---|---|---|
| Total | Units on hand, including held ones | Admin inventory table; the field the admin edits |
| Reserved | Units held by confirmed, not-yet-completed requests | Admin inventory table |
| Available | Total − reserved | What customers see; server-computed |

Items with zero available are hidden from the storefront, and a customer can never add more than
the available count to their cart — enforced in the UI and re-checked on the server.

Holds are released when a request leaves `CONFIRMED`. Completing a request resolves its hold for
good: **Clear Stock** subtracts the units from the total (the customer took them), while
**Restore Stock** just releases the hold and puts them back on sale. The total is only ever
reduced by a completed clear or by the admin editing it directly — and it can't be set below
what's currently reserved.
