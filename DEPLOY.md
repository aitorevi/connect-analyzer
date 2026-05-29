# Deploy

End-to-end deployment of the live demo: **backend + mock on [Fly.io](https://fly.io)**, **frontend on
[Vercel](https://vercel.com)**. The frontend (`page.tsx`) fetches the backend server-side, so the
demo flow is `Vercel (Next.js Server Component) → Fly (backend) → Fly (mock)` and **no browser CORS
is involved**.

## Prerequisites

- A Fly.io account and `flyctl` installed and authenticated (`flyctl auth login`).
- A Vercel account.
- (Optional, only if you want real SAP data) A free API key from the
  [SAP Business Accelerator Hub](https://api.sap.com).

## 1. Deploy the mock (Fly)

The mock is just an nginx serving `data/sales.txt`. Deploy it first so the backend can reach it.

```bash
cd sap-mock
flyctl launch --no-deploy        # accept the existing fly.toml; pick a unique app name if "sap-analyzer-mock" is taken
flyctl deploy
```

Verify it responds:

```bash
curl -s https://<your-mock-app>.fly.dev/sales.txt | head -3
```

> **Note**: app names are globally unique on Fly. If you change the name, update
> `SapMock__BaseUrl` in `backend/fly.toml` accordingly (the `.internal` hostname follows the app name).

## 2. Deploy the backend (Fly)

```bash
cd backend
flyctl launch --no-deploy        # accept the existing fly.toml; pick a unique app name
flyctl deploy
```

Smoke-test:

```bash
curl -s https://<your-backend-app>.fly.dev/api/sales | head
curl -X POST https://<your-backend-app>.fly.dev/api/sales/refresh   # forces re-ingestion from the source
```

### Optional: switch to real SAP data

By default the backend uses the mock (`SalesSource=Mock`). To pull from the real SAP S/4HANA OData
sandbox instead:

```bash
flyctl secrets set Sap__ApiKey="<your-api-key>" -a <your-backend-app>
flyctl secrets set SalesSource="Sap"             -a <your-backend-app>
# Redeploy if a deploy is not triggered automatically:
flyctl deploy
```

### Optional: persistent volume for SQLite

By default the SQLite file lives inside the machine's ephemeral filesystem; the seed-on-startup repopulates
it on each restart. To survive machine recreations, attach a Fly volume:

```bash
flyctl volumes create sap_analyzer_data --size 1 --region mad -a <your-backend-app>
```

Then add to `backend/fly.toml`:

```toml
[[mounts]]
  source = "sap_analyzer_data"
  destination = "/data"

[env]
  Sqlite__Path = "/data/sales.db"
```

You will likely need to make `/data` writable by the non-root user the container runs as:

```bash
flyctl ssh console -a <your-backend-app> -u root
chown $APP_UID:$APP_UID /data
exit
flyctl deploy
```

## 3. Deploy the frontend (Vercel)

In the Vercel dashboard: **Add New… → Project → Import the GitHub repo `aitorevi/sap-analyzer`**.

- **Root Directory**: `frontend` (the Next.js app lives in this subfolder).
- **Framework Preset**: Next.js (auto-detected).
- **Environment variables**:
  - `BACKEND_URL` = `https://<your-backend-app>.fly.dev`

Click **Deploy**. Vercel builds with `next build` and serves the dashboard.

## 4. Optional: lock CORS to your Vercel origin

`page.tsx` fetches the backend **server-side**, so the browser never calls the backend directly — CORS
is not exercised. If you later add a client-side fetch, restrict the backend's CORS origin:

```bash
flyctl secrets set Cors__AllowedOrigins__0="https://<your-frontend>.vercel.app" -a <your-backend-app>
flyctl deploy
```

Never widen to `AllowAnyOrigin`.

## Costs

Fly's free allowance covers two small `shared-cpu-1x` 256 MB machines that auto-stop when idle (~30 s
cold start on first hit). Vercel's Hobby plan covers a Next.js app of this size.
