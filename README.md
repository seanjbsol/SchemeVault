# SchemeVault

UK multi-scheme contractor compliance vault and renewal OS. Map evidence once, then drive CHAS, Constructionline, SafeContractor, Avetta and SMAS Worksafe renewals with expiry traffic lights and gap checklists.

This repository is an MVP monorepo:

| Path | Stack |
| --- | --- |
| `apps/api` | ASP.NET Core 8 Web API, EF Core, ASP.NET Identity + JWT |
| `apps/mobile` | React Native (Expo managed workflow, Expo Router) |
| `tests/SchemeVault.Api.Tests` | Tenant-isolation and vertical-slice tests |

## Architecture

- **Multi-tenant.** A user belongs to a Tenant (organisation). Signup creates the tenant, the first user, and an Owner membership.
- **JWT claims** include `tenant_id` and `role` (`Owner`, `Admin`, `Member`) plus the usual subject/email.
- **Every operational row has `TenantId`.** Callers cannot stamp another tenant onto a payload — the API sets `TenantId` from the token.
- **Tenant isolation** is enforced twice:
  1. EF Core **global query filters** on memberships, evidence, accreditations and gap items (`TenantId == current tenant`).
  2. **Explicit checks** in services (`TenantGuard.EnsureOwns`) so a missed filter still returns 404, never another organisation's data.
- **Catalogue vs tenant data.** `Scheme` and `GapChecklistTemplate` are a shared UK catalogue (no `TenantId`). Accreditations, vault items and per-tenant gap rows are tenant-owned.

Traffic lights: **green** (in date), **amber** (expires within 60 days), **red** (expired / suspended), **grey** (not started).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
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

`appsettings.json` leaves `Jwt:SigningKey` empty on purpose. `appsettings.Development.json` holds a **local-dev-only** key so `dotnet run` works. That key is not a production secret.

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

Screens: sign in / register (creates a tenant), home dashboard (traffic lights), schemes list + detail, evidence vault + add, renewals, settings (organisation name + sign out).

UK English copy throughout.

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
| GET | `/health` | Liveness |

## Solution layout

```
SchemeVault.sln
apps/api/          SchemeVault.Api
apps/mobile/       Expo app
tests/SchemeVault.Api.Tests
```

## What is deliberately out of scope for this scaffold

Portal integrations, OCR, billing, invite-to-tenant, and production blob storage. The vault upload path is a local-disk stub so the vertical slice is real without cloud credentials.
