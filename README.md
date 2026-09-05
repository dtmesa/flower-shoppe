# Flower Shoppe

A local-pickup storefront for a plumeria plant business.

- **`frontend/`** — React + TypeScript (Vite) site, organized by feature, where customers browse inventory, add items to a cart, and submit a pickup request for everything in it.
- **`backend/`** — ASP.NET Core 10 (C#) Minimal API behind admin login, with CRUD for inventory (including photos) and management of incoming pickup requests. Published as a Native AOT binary and run as an AWS Lambda function, storing data in DynamoDB and photos in S3. No payment processing — pickup is arranged offline after a request comes in.

## Prerequisites

- .NET 10 SDK (backend)
- Docker (local DynamoDB + S3 emulators; also how the deployable Lambda artifact is built)
- Node.js 18+ and npm (frontend)

## Project layout

```
backend/
  PlumeriaStore.sln
  Dockerfile                 builds the Native AOT Lambda artifact (backend/publish/function.zip)
  Dockerfile.local           plain build, used by docker-compose for local development
  template.yaml              AWS SAM stack: function, HTTP API, DynamoDB table, S3 bucket
  src/PlumeriaStore.Api/     the API itself, organized by feature (Auth/, Inventory/, Reservations/)
  tests/PlumeriaStore.Api.Tests/   xUnit tests (service-layer + real HTTP endpoint tests)
docker-compose.yml           dynamodb-local + minio + the backend
frontend/
  src/components/             shared UI primitives (Modal, FormError, ConfirmDialog, ...)
  src/lib/                    api client and shared formatters
  src/styles/                 tokens.css -> base.css -> components.css (imported in that order)
  src/features/               each feature owns its components, API hooks, and types
    auth/, cart/, inventory/, reservations/, theme/
```

## Running the backend

The API talks to DynamoDB and S3. Locally those are emulated by containers, so start them first:

```bash
docker compose up -d dynamodb minio
```

Then either run the API from the SDK:

```bash
cd backend
dotnet run --project src/PlumeriaStore.Api
```

or run everything in containers:

```bash
docker compose up -d --build
```

Either way the API starts on `http://localhost:8080`, pointed at DynamoDB Local (`:8000`) and MinIO
(`:9000`, console on `:9001`). It creates its table and bucket on first run and seeds the admin
account and default categories, so there is nothing to set up by hand. Both containers keep their
data in named volumes; `docker compose down -v` wipes them and starts over.

Run the test suite with:

```bash
cd backend
dotnet test
```

The tests need the two emulator containers running — they exercise the real data layer (conditional
writes, transactions, S3 round-trips) rather than a stand-in for it. Each test class gets its own
table and bucket and drops them afterwards.

### Admin login

A single admin account is created from configuration **the first time the app runs against an
empty table**:

| Setting | Default |
|---|---|
| `App:Admin:Username` | `admin` |
| `App:Admin:Password` | `admin` |

These are bootstrap values, not a permanent override. Once the account exists the seeder leaves
it alone, so credentials changed in-app (Admin → Account) survive restarts. To get back to the
configured values, delete the admin row (or the whole local table) and let it re-seed.

**Change the password before any real use.** Configuration follows the standard ASP.NET Core pattern: `appsettings.json` holds the defaults, and any value can be overridden with an environment variable using the `__` (double underscore) separator for nested keys, e.g.:

```bash
App__Admin__Username=myuser App__Admin__Password=my-strong-password dotnet run --project src/PlumeriaStore.Api
```

Other configurable settings (all optional, see `backend/src/PlumeriaStore.Api/appsettings.json`):

| Setting | Purpose | Default |
|---|---|---|
| `Urls` (or `ASPNETCORE_URLS` env var) | Listen address when running under Kestrel (ignored on Lambda) | `http://localhost:8080` |
| `App:Jwt:Secret` | Signing key for admin session tokens; must be at least 32 bytes | dev-only default — **set this in production** |
| `App:Jwt:ExpirationMinutes` | Admin token lifetime | `720` (12h) |
| `App:Cors:AllowedOrigins` | Comma-separated allowed frontend origins | `http://localhost:5173,https://flower-shoppe.iridebears.workers.dev` |
| `App:Aws:Region` | Region for the DynamoDB and S3 clients | `us-west-2` |
| `App:Dynamo:TableName` | DynamoDB table holding everything | `PlumeriaStore` |
| `App:Dynamo:ServiceUrl` | Point at DynamoDB Local; blank means the real service | blank |
| `App:Storage:BucketName` | S3 bucket holding uploaded photos | `plumeria-store-uploads` |
| `App:Storage:ServiceUrl` | Point at MinIO; blank means real S3 | blank |
| `App:Storage:MaxSizeBytes` | Max photo upload size | `4194304` (4MB) |
| `App:Email:Region` | AWS region used for the SES client | `us-west-2` |

`App:Dynamo` and `App:Storage` each also take an `AccessKey`/`SecretKey` pair. Those exist for the
emulators, which have their own credentials and must not pick up the real AWS keys that
`backend/.env` supplies to SES. Left blank — as they are when deployed — the SDK's normal
credential chain is used.

### How data is stored

One DynamoDB table holds everything. Each kind of record lives in its own partition, split by sort
key, with no secondary index:

| Partition key | Sort key | Holds |
|---|---|---|
| `ADMIN` | `ADMIN` | The single admin account |
| `CATEGORY` | `<KIND>#<Name>` | Type/Color/Size options — the key is what makes each pair unique |
| `ITEM` | item ID tag (e.g. `RYM`) | An inventory item, with its photos as a nested list |
| `REQUEST` | zero-padded request ID | A pickup request, with its line items as a nested list |
| `COUNTER` | counter name | Atomic ID source, replacing SQLite's identity columns |

That shape suits a shop of this size: listing anything is a single strongly-consistent Query, so a
write is always visible to the next read, and there is no eventually-consistent index to reason
about. It assumes the collections stay small (tens of items, hundreds of requests) — a much larger
catalog would want items spread across partitions with an index for listing.

Two things the relational schema did for free are now explicit:

- **Reserved stock.** It used to be summed from the reservation rows on demand. It is now a
  counter on the inventory item, moved in the same `TransactWriteItems` that flips a request's
  holds on or off, conditioned on the values it was read at.
- **Deleting an item.** `ON DELETE SET NULL` cleared the reference on any line item pointing at it.
  Deleting an item now rewrites the requests that mention it, clearing the reference and keeping
  the snapshot so history still reads correctly.

Photos go to S3 and are served back through `GET /uploads/{filename}`, the same path the API used
to serve them from disk — so the frontend is unchanged and the bucket stays private.

### Email notifications

Copy `backend/.env.example` to `backend/.env` and fill in the values below to get an email at
`EMAIL_FROM_ADDRESS` every time a customer submits a pickup request, sent via AWS SES. `backend/.env`
is git-ignored — it's loaded automatically at startup (via `DotNetEnv`) and isn't committed.
Deployed, the same values come from the CloudFormation stack instead.

| Key | Purpose |
|---|---|
| `EMAIL_SENDER` | Reserved for selecting a provider later; only `"ses"` is implemented today |
| `EMAIL_FROM_ADDRESS` | Verified SES sender **and** the notification recipient (send-to-self) |
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` | Credentials for an IAM user with `ses:SendEmail` |

While the SES account is in sandbox mode, both `EMAIL_FROM_ADDRESS` and any recipient must be
verified identities in the SES console, or sends will fail (silently, from the customer's
perspective — a failed notification is logged but never blocks the pickup request itself).

## Deploying the backend

The function is published Native AOT: a self-contained binary with no .NET runtime to load, which
takes a cold start from well over a second down to a fraction of one. That matters more than usual
here, since a storefront this quiet is mostly cold starts.

Native AOT compiles to the machine it runs on and can't cross-compile, so the artifact is built in
a container that matches Lambda's Amazon Linux 2023:

```bash
docker build -f backend/Dockerfile --target artifact --output type=local,dest=backend/publish backend
```

That writes `backend/publish/function.zip`. First deploy, with [AWS SAM](https://docs.aws.amazon.com/serverless-application-model/):

```bash
cd backend
sam deploy --guided --template template.yaml
```

It will ask for `JwtSecret` (32+ bytes), `AdminPassword`, `CorsAllowedOrigins`, and optionally
`EmailFromAddress`. The stack creates the Lambda function, an HTTP API in front of it, the DynamoDB
table, and the S3 bucket; the table and bucket are set to `Retain`, so tearing the stack down
leaves the shop's data alone. Its `ApiUrl` output is what goes in `frontend/.env` as
`VITE_API_BASE_URL`.

Later code-only deploys don't need the stack at all:

```bash
docker build -f backend/Dockerfile --target artifact --output type=local,dest=backend/publish backend
aws lambda update-function-code --function-name <name> --zip-file fileb://backend/publish/function.zip
```

To run on Graviton instead (cheaper per millisecond), pass `--build-arg RUNTIME_ID=linux-arm64` and
change `Architectures` in `template.yaml` to `arm64` — note that needs an arm64 builder or emulation.

A few consequences of the AOT publish worth knowing before changing the API:

- **Anything crossing the wire must be listed in `Common/Serialization/AppJsonSerializerContext.cs`.**
  A response type that isn't will fail to serialize at runtime, not at build time.
- **Request validation is hand-written** (`IValidatableRequest`) rather than DataAnnotations, which
  discovers its attributes by reflection.
- **No Swagger UI.** Swashbuckle isn't AOT-compatible.
- **Photo uploads are capped at 4MB**, down from 5MB: a request reaches the function base64-encoded,
  which inflates it by a third, and Lambda caps a request or response at 6MB. The same ceiling
  applies to serving a photo back through `GET /uploads/{filename}`.

## Running the frontend

```bash
cd frontend
npm install
npm run dev
```

Opens at `http://localhost:5173`. It talks to the backend via `VITE_API_BASE_URL`, set in `frontend/.env` (copy `frontend/.env.example` if you need to point it elsewhere, e.g. the deployed API URL).

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

Holds are released when a request leaves `CONFIRMED`, or when it is deleted outright. Completing a
request resolves its hold for good: **Clear Stock** subtracts the units from the total (the customer
took them), while **Restore Stock** just releases the hold and puts them back on sale. The total is
only ever reduced by a completed clear or by the admin editing it directly — and it can't be set
below what's currently reserved.
