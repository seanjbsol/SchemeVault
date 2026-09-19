# SchemeVault

UK multi-scheme contractor compliance vault and renewal OS. Map evidence once, then drive CHAS, Constructionline, SafeContractor, Avetta and SMAS Worksafe renewals with expiry traffic lights and gap checklists.

This repository is an MVP monorepo:

| Path | Stack |
| --- | --- |
| `apps/api` | ASP.NET Core 10 Web API, EF Core, ASP.NET Identity + JWT |
| `apps/mobile` | React Native (Expo managed workflow, Expo Router) |
| `tests/SchemeVault.Api.Tests` | Tenant-isolation, billing-gate, and vertical-slice tests |

## Architecture

- **Multi-tenant.** A user belongs to a Tenant (organisation). Signup creates the tenant, the first user, and an Owner membership.
- **JWT claims** include `tenant_id` and `role` (`Owner`, `Admin`, `Member`) plus the usual subject/email.
- **Every operational row has `TenantId`.** Callers cannot stamp another tenant onto a payload — the API sets `TenantId` from the token.
- **Tenant isolation** is enforced twice:
  1. EF Core **global query filters** on memberships, evidence, accreditations and gap items (`TenantId == current tenant`).
  2. **Explicit checks** in services (`TenantGuard.EnsureOwns`) so a missed filter still returns 404, never another organisation's data.
- **Catalogue vs tenant data.** `Scheme` and `GapChecklistTemplate` are a shared UK catalogue (no `TenantId`). Accreditations, vault items and per-tenant gap rows are tenant-owned.
- **Billing** is delegated to the central **QckApp Subscription API**. SchemeVault never talks to Stripe. Authenticated tenant resource calls require an `active` or `trialing` entitlement; otherwise the API returns **402** with a pointer to checkout.

Traffic lights: **green** (in date), **amber** (expires within 60 days), **red** (expired / suspended), **grey** (not started).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS; `global.json` pins `10.0.401` with `rollForward: latestFeature`)
- Node 20+ (22 is fine) and npm
- For the mobile app: [Expo Go](https://expo.dev/go) on a device, or an iOS Simulator / Android emulator

## Run the API locally

```bash
cd apps/api
dotnet restore
dotnet run
```

The API listens on **http://localhost:5080**.

| Check | |
| --- | --- |
| Health | `GET http://localhost:5080/health` (anonymous) |
| Swagger | http://localhost:5080/swagger |
| Demo login | `demo@schemevault.test` / `DemoPassw0rd!` (Northern Plant Hire Ltd) |

On first run EF Core applies migrations to a local SQLite file `apps/api/schemevault.dev.db` and seeds the scheme catalogue plus the demo tenant.

Register a second organisation from the mobile app or:

```bash
curl -s http://localhost:5080/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"organisationName":"Bravo Scaffolding Ltd","fullName":"Sam Reed","email":"sam@bravo.test","password":"TestPassw0rd!"}'
```

That JWT cannot list the demo tenant's evidence or renewals. `tests/SchemeVault.Api.Tests` asserts this.

```bash
dotnet test
```

### Configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:Default` | SQLite path by default |
| `Database:Provider` | `Sqlite` (default) or `SqlServer` |
| `Jwt:SigningKey` | **Required**, ≥ 32 characters. Development has a local-only placeholder. In production set `Jwt__SigningKey` — do not commit production secrets. |
| `Jwt:Issuer` / `Jwt:Audience` | Must match the mobile client (`SchemeVault` / `SchemeVault.Mobile`) |
| `Storage:RootPath` | Local file-upload stub (`App_Data/uploads/{tenantId}/`) |
| `Cors:Origins` | Production allowed origins. Development allows any origin so Expo can talk to the API. |
| `SubscriptionApi:BaseUrl` | Origin of the QckApp Subscription API (no trailing path). Example: `https://subscription.qckapp.example` |
| `SubscriptionApi:ApiKey` | Server API key sent as `X-Api-Key`. Set via `SubscriptionApi__ApiKey` in production — do not commit secrets. |
| `SubscriptionApi:ProductCode` | Always `SchemeVault` |
| `SubscriptionApi:UseStub` | When `true`, no HTTP calls are made. Every tenant is treated as **active** on **Starter**. Default in Development and Testing. |

`appsettings.json` leaves `Jwt:SigningKey` and `SubscriptionApi:ApiKey` empty on purpose. `appsettings.Development.json` holds a **local-dev-only** JWT key and enables subscription stub mode so `dotnet run` works without a live Qck API.

### QckApp Subscription API

SchemeVault is a Qck product. Billing, checkout, and the customer portal live in the central subscription service. This repo only holds a typed `HttpClient` (`SubscriptionClient`).

| SchemeVault | Qck |
| --- | --- |
| Register tenant | `POST {BaseUrl}/api/v1/tenants` with `name`, `ownerEmail`, `externalTenantId`, `productCode` |
| Gate resource APIs | `GET {BaseUrl}/api/v1/entitlements/SchemeVault/{tenantId}` (`X-Api-Key`) |
| `POST /api/billing/checkout` | `POST {BaseUrl}/api/v1/checkout/sessions` (success/cancel URLs forwarded) |
| `POST /api/billing/portal` | `POST {BaseUrl}/api/v1/portal/sessions` |
| `GET /api/billing/entitlements` | Current plan/status for Settings |

Statuses **`active`** and **`trialing`** are allowed through. Anything else (including `canceled`, `past_due`, `inactive`) returns **402** with `checkout: "/api/billing/checkout"`. Auth (`/api/auth/*`) and billing endpoints are not gated, so a lapsed tenant can still sign in and upgrade.

**Stub mode** (`SubscriptionApi:UseStub=true`):

- Entitlements are `active` / `Starter` (override with `StubStatus` / `StubPlan` in tests)
- Checkout URL: `https://billing.qckapp.test/checkout/schemevault`
- Portal URL: `https://billing.qckapp.test/portal/schemevault`
- Tenant upsert is a no-op log line

Point a real environment at Qck with:

```bash
export SubscriptionApi__UseStub=false
export SubscriptionApi__BaseUrl=https://subscription.qckapp.example
export SubscriptionApi__ApiKey=...   # from your secret store, never committed
export SubscriptionApi__ProductCode=SchemeVault
```

### Swap SQLite → SQL Server

1. Set `Database:Provider` to `SqlServer`.
2. Set `ConnectionStrings:Default` to a SQL Server connection string, for example:

   `Server=localhost,1433;Database=SchemeVault;User Id=sa;Password=...;TrustServerCertificate=True`

3. Run `dotnet ef database update --project apps/api`.

SQLite cannot `ORDER BY DateTimeOffset`, so the DbContext stores those columns as binary **only when the provider is SQLite**. SQL Server uses native `datetimeoffset`. No other code change is required for the swap.

## Run the mobile app

```bash
cd apps/mobile
cp .env.example .env   # EXPO_PUBLIC_API_URL=http://localhost:5080
npm install
npm start              # or npm run ios / android / web
```

Point Expo at the API with **`EXPO_PUBLIC_API_URL`**:

| Where the app runs | Typical URL |
| --- | --- |
| iOS Simulator / Expo web on the same machine | `http://localhost:5080` |
| Android emulator | `http://10.0.2.2:5080` |
| Physical device | `http://<your-lan-ip>:5080` (API already binds to localhost; use `--urls http://0.0.0.0:5080` if the device cannot connect) |

Screens: sign in / register (creates a tenant and upserts it to Qck), home dashboard (traffic lights), schemes list + detail, evidence vault + add, renewals, settings (plan/status, Manage billing / Upgrade via `Linking.openURL`, organisation name, sign out).

UK English copy throughout.

Preview without running locally: [docs/screenshots](docs/screenshots). The settings shot predates billing UI.

## API surface (MVP)

All resource routes require `Authorization: Bearer <jwt>` and are scoped to `tenant_id`.

| Method | Path | |
| --- | --- | --- |
| POST | `/api/auth/register` | Create tenant + Owner |
| POST | `/api/auth/login` | Issue JWT |
| GET | `/api/auth/me` | Current user |
| GET | `/api/dashboard` | Upcoming / expired / missing evidence |
| GET | `/api/schemes` | Catalogue + this tenant's accreditation |
| GET | `/api/schemes/{id}` | Detail + gap checklist |
| PATCH | `/api/schemes/{schemeId}/gaps/{gapId}` | Link evidence / update status |
| GET/POST | `/api/evidence` | Vault list / create metadata |
| PUT/DELETE | `/api/evidence/{id}` | Update / delete |
| POST/GET | `/api/evidence/{id}/file` | Local disk upload stub / download |
| GET/POST | `/api/renewals` | Accreditation records |
| PUT/DELETE | `/api/renewals/{id}` | Update / delete |
| GET/PATCH | `/api/tenants/current` | Organisation name (Owner/Admin to rename) |
| GET | `/api/billing/entitlements` | Current plan/status (not subscription-gated) |
| POST | `/api/billing/checkout` | Owner/Admin: proxy Qck checkout session; body `{ successUrl, cancelUrl }` |
| POST | `/api/billing/portal` | Owner/Admin: proxy Qck customer portal session |
| GET | `/health` | Liveness |

## Solution layout

```
SchemeVault.sln
apps/api/          SchemeVault.Api
apps/mobile/       Expo app
tests/SchemeVault.Api.Tests
```

## What is deliberately out of scope for this scaffold

Portal scheme integrations, OCR, invite-to-tenant, and production blob storage. The vault upload path is a local-disk stub so the vertical slice is real without cloud credentials. Stripe is owned by the QckApp Subscription API, not this repo.
