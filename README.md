# SchemeVault

UK multi-scheme contractor compliance vault and renewal OS. Map evidence once, then drive CHAS, Constructionline, SafeContractor, Avetta and SMAS Worksafe renewals with expiry traffic lights and gap checklists.

This repository is an MVP monorepo:

| Path | Stack |
| --- | --- |
| `apps/api` | ASP.NET Core 10 Web API, EF Core, ASP.NET Identity + JWT |
| `apps/mobile` | React Native (Expo managed workflow, Expo Router) |
| `tests/SchemeVault.Api.Tests` | Tenant-isolation, billing-gate, Pro entitlement, and vertical-slice tests |

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
| `SubscriptionApi:UseStub` | When `true`, no HTTP calls are made. Default in Development and Testing. |
| `SubscriptionApi:StubPlan` | Stub plan name. `Starter` (default in tests) or `Pro`. |
| `SubscriptionApi:ForcePro` | When `true`, an active stub (or live) entitlement is treated as **Pro** so questionnaires, accidents and equipment can be demoed. **On by default in Development.** Does not bypass an inactive/canceled subscription. |

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

**Starter vs Pro.** An active subscription is not enough for every route. Guided questionnaires, multi-scheme pack export, accident/lost-hours, and the equipment register require **Pro**, detected from Qck `plan` / `planCode` (e.g. `Pro`) or feature flags (`questionnaires`, `multi_scheme_export`, `accidents`, `equipment`). Starter tenants receive **402** with `requiredPlan: "Pro"`. `GET /api/billing/entitlements` includes `isPro` and `features` so the mobile app can show upgrade UX before the call.

**Stub mode** (`SubscriptionApi:UseStub=true`):

- Entitlements are `active` on `StubPlan` (Development defaults to **Pro** via `ForcePro` + `StubPlan=Pro`; tests default to **Starter**)
- Checkout URL: `https://billing.qckapp.test/checkout/schemevault`
- Portal URL: `https://billing.qckapp.test/portal/schemevault`
- Tenant upsert is a no-op log line

### Try questionnaires and stub Pro

Development already turns Pro on so the demo tenant can complete a pack without a live Qck plan.

```bash
cd apps/api
dotnet run
# appsettings.Development.json: UseStub=true, ForcePro=true, StubPlan=Pro
```

1. Sign in as `demo@schemevault.test` / `DemoPassw0rd!` (or register a new org).
2. Confirm **Settings** shows plan **Pro**.
3. Open the **H&S** tab → **Guided questionnaires** → pick CHAS (or Constructionline / SafeContractor / Avetta).
4. Answer the plain-English questions and tap **Generate pack draft**. You get markdown in-app and a PDF (`GET /api/questionnaires/responses/{id}/pdf`).
5. **Export multi-scheme PDF pack** combines the latest generated draft per scheme.
6. Attach **photos** on a vault item, an incident, or a piece of equipment (local disk under `App_Data/uploads/{tenantId}/`).
7. **Accidents** logs incidents and lost hours for this tenant only. **Equipment** flags overdue calibration/service dates.

To see the **Starter paywall** (402 + upgrade copy) locally:

```bash
# from apps/api, override Development
export SubscriptionApi__ForcePro=false
export SubscriptionApi__StubPlan=Starter
dotnet run
```

Starter can still use the vault, schemes and renewals. Questionnaires, pack export, accidents and the equipment register return **402** with `requiredPlan: Pro` and `checkout: /api/billing/checkout`.

Generated packs are **working drafts**, not official scheme submissions, certificates, or legal advice. Copy says so on every document.

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

Screens: sign in / register (creates a tenant and upserts it to Qck), home dashboard (traffic lights plus incident/kit counts), schemes list + detail, evidence vault + photos, renewals, **H&S** (questionnaires, accidents, equipment — Pro), settings (plan/status, Manage billing / Upgrade via `Linking.openURL`, organisation name, sign out).

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
| GET | `/api/billing/entitlements` | Current plan/status, `isPro`, `features` (not subscription-gated) |
| POST | `/api/billing/checkout` | Owner/Admin: proxy Qck checkout session; body `{ successUrl, cancelUrl }` |
| POST | `/api/billing/portal` | Owner/Admin: proxy Qck customer portal session |
| GET/POST | `/api/evidence/{id}/photos` | Photo evidence on a vault item (Starter) |
| GET/POST | `/api/photos/{id}` + `/file` | Photo metadata / bytes / DELETE |
| GET | `/api/questionnaires` | Pro: scheme questionnaires |
| GET/POST | `/api/questionnaires/schemes/{code}` + `/responses` | Pro: questions / save answers and generate pack |
| GET | `/api/questionnaires/responses/{id}/pdf` | Pro: generated PDF |
| POST | `/api/questionnaires/export` | Pro: multi-scheme pack PDF |
| GET/POST | `/api/accidents` | Pro: incident list / create |
| GET/POST | `/api/lost-hours` | Pro: lost-hours log |
| GET/POST | `/api/equipment` | Pro: register; `?overdue=true` |
| GET | `/health` | Liveness |

## Solution layout

```
SchemeVault.sln
apps/api/          SchemeVault.Api
apps/mobile/       Expo app
tests/SchemeVault.Api.Tests
```

## What is deliberately out of scope for this scaffold

Portal scheme integrations, OCR, invite-to-tenant, and production blob storage. The vault/photo upload path is a local-disk stub so the vertical slice is real without cloud credentials. Stripe is owned by the QckApp Subscription API, not this repo. Generated questionnaire packs are drafts, not filings.
